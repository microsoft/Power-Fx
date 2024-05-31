// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests.Helpers
{
    internal class TestUtils
    {        
        // Parse a type in string form to a DType
        public static DType DT(string type)
        {
            Assert.True(DType.TryParse(type, out DType dtype));
            Assert.True(dtype.IsValid);
            return dtype;
        }

        // Parse a type in string form to a DType, with DisplayName
        public static DType DT2(string type)
        {
            Assert.True(TestExtensions.TryParse2(type, out DType dtype));
            Assert.True(dtype.IsValid);
            return dtype;
        }

        public static void AssertJsonEqual(string expected, string actual)
        {
            using var expectedDom = JsonDocument.Parse(expected);
            using var actualDom = JsonDocument.Parse(actual);
            AssertJsonEqual(expectedDom.RootElement, actualDom.RootElement);
        }

        public static void AssertJsonEqual(JsonElement expected, JsonElement actual)
        {
            AssertJsonEqualCore(expected, actual, new ());
        }

        private static void AssertJsonEqualCore(JsonElement expected, JsonElement actual, Stack<object> path)
        {
            JsonValueKind valueKind = expected.ValueKind;
            Assert.True(valueKind == actual.ValueKind);

            switch (valueKind)
            {
                case JsonValueKind.Object:
                    var expectedProperties = new List<string>();
                    foreach (JsonProperty property in expected.EnumerateObject())
                    {
                        expectedProperties.Add(property.Name);
                    }

                    var actualProperties = new List<string>();
                    foreach (JsonProperty property in actual.EnumerateObject())
                    {
                        actualProperties.Add(property.Name);
                    }

                    foreach (var property in expectedProperties.Except(actualProperties))
                    {
                        Assert.Fail($"Property \"{property}\" missing from actual object.");
                    }

                    foreach (var property in actualProperties.Except(expectedProperties))
                    {
                        Assert.Fail($"Actual object defines additional property \"{property}\".");
                    }

                    foreach (var name in expectedProperties)
                    {
                        path.Push(name);
                        AssertJsonEqualCore(expected.GetProperty(name), actual.GetProperty(name), path);
                        path.Pop();
                    }

                    break;
                case JsonValueKind.Array:
                    JsonElement.ArrayEnumerator expectedEnumerator = expected.EnumerateArray();
                    JsonElement.ArrayEnumerator actualEnumerator = actual.EnumerateArray();

                    var i = 0;
                    while (expectedEnumerator.MoveNext())
                    {
                        Assert.True(actualEnumerator.MoveNext(), "Actual array contains fewer elements.");
                        path.Push(i++);
                        AssertJsonEqualCore(expectedEnumerator.Current, actualEnumerator.Current, path);
                        path.Pop();
                    }

                    Assert.False(actualEnumerator.MoveNext(), "Actual array contains additional elements.");
                    break;
                case JsonValueKind.String:
                    Assert.Equal(expected.GetString(), actual.GetString());
                    break;
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                    Assert.Equal(expected.GetRawText(), actual.GetRawText());
                    break;
                default:
                    Assert.Fail($"Unexpected JsonValueKind: JsonValueKind.{valueKind}.");
                    break;
            }
        }

        public sealed class MockFunctionThatSupportsCoercedParamsWithOverride : BuiltinFunction
        {
            private readonly string _runtimeFunctionNameSuffix;
            private readonly bool _supportsParamCoercion;

            // Be explicit about wanting to support param coercion.
            public override bool SupportsParamCoercion => _supportsParamCoercion;

            public override bool IsSelfContained => true;

            public bool CheckNumericTableOverload { get; set; }

            public bool CheckStringTableOverload { get; set; }

            public MockFunctionThatSupportsCoercedParamsWithOverride(string name, string runtimeFunctionNameSuffix, bool supportsParamCoercion, DType returnType, System.Numerics.BigInteger maskLambdas, int arityMin, int arityMax, params DType[] argTypes)
                : base(name, (l) => name, FunctionCategories.Text, returnType, maskLambdas, arityMin, arityMax, argTypes)
            {
                _runtimeFunctionNameSuffix = runtimeFunctionNameSuffix;
                _supportsParamCoercion = supportsParamCoercion;
            }

            public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
            {
                yield break;
            }

            public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> coercedArgs)
            {
                var isValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out coercedArgs);

                // explicitly blocking coercion
                if (CheckNumericTableOverload)
                {
                    if (!CheckNumericColumnType(context, args[0], argTypes[0], errors, ref coercedArgs))
                    {
                        isValid = false;
                        coercedArgs?.Clear();
                    }

                    return isValid;
                }

                if (CheckStringTableOverload)
                {
                    if (!CheckStringColumnType(context, args[0], argTypes[0], errors, ref coercedArgs))
                    {
                        isValid = false;
                        coercedArgs?.Clear();
                    }

                    return isValid;
                }

                if (!DType.Number.Accepts(argTypes[0], exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules))
                {
                    isValid = false;
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrNumberExpected);                    
                    coercedArgs?.Clear();                    
                }

                return isValid;
            }

            private bool CheckColumnType(CheckTypesContext context, DType type, TexlNode arg, DType expectedType, IErrorContainer errors, ErrorResourceKey errKey, ref bool wasCoerced)
            {
                Contracts.Assert(type.IsValid);
                Contracts.AssertValue(arg);
                Contracts.Assert(expectedType.IsValid);
                Contracts.AssertValue(errors);

                IEnumerable<TypedName> columns;
                if (!type.IsTable || (columns = type.GetNames(DPath.Root)).Count() != 1)
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, arg, TexlStrings.ErrInvalidSchemaNeedCol);
                    return false;
                }
                else if (!(expectedType.Accepts(columns.Single().Type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules) || columns.Single().Type.CoercesTo(expectedType, aggregateCoercion: true, isTopLevelCoercion: false, features: context.Features)))
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, arg, TexlStrings.ErrInvalidSchemaNeedTypeCol_Col, expectedType.GetKindString(), columns.Single().Name.Value);
                    return false;
                }

                if (!expectedType.Accepts(columns.Single().Type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules))
                {
                    wasCoerced = true;
                }

                return true;
            }
        }

        public class MockFunction : BuiltinFunction
        {
            private readonly FunctionFlags _flags;
            private readonly string _runtimeFunctionNameSuffix;

            public override bool IsAsync => _flags.HasFlag(FunctionFlags.IsAsync);

            public override bool IsBehaviorOnly => _flags.HasFlag(FunctionFlags.IsBehaviorOnly);
            
            public override bool IsStateless => _flags.HasFlag(FunctionFlags.IsStateless);
            
            public override bool IsSelfContained => _flags.HasFlag(FunctionFlags.IsSelfContained);
            
            public override bool ManipulatesCollections => _flags.HasFlag(FunctionFlags.ManipulatesCollections);
            
            public override bool AffectsCollectionSchemas => _flags.HasFlag(FunctionFlags.AffectsCollectionSchemas);
            
            public override bool IsStrict => _flags.HasFlag(FunctionFlags.IsStrict);
            
            public override bool SupportsParamCoercion => _flags.HasFlag(FunctionFlags.SupportsParamCoercion);

            public MockFunction(string name, string runtimeFunctionNameSuffix, FunctionCategories category, FunctionFlags flags, DType returnType, System.Numerics.BigInteger maskLambdas, int arityMin, int arityMax, params DType[] argTypes)
                : base(name, (l) => "MockFunction", category, returnType, maskLambdas, arityMin, arityMax, argTypes)
            {
                _flags = flags;
                _runtimeFunctionNameSuffix = runtimeFunctionNameSuffix;
            }

            public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
            {
                yield break;
            }
        }

        public sealed class MockFunctionWithScope : FunctionWithTableInput
        {
            private readonly FunctionFlags _flags;
            private readonly string _runtimeFunctionNameSuffix;

            public override bool IsAsync => _flags.HasFlag(FunctionFlags.IsAsync);
            
            public override bool IsBehaviorOnly => _flags.HasFlag(FunctionFlags.IsBehaviorOnly);
            
            public override bool SupportsParamCoercion => _flags.HasFlag(FunctionFlags.SupportsParamCoercion);

            public override bool IsSelfContained => true;

            public MockFunctionWithScope(string name, string runtimeFunctionNameSuffix, FunctionCategories category, FunctionFlags flags, DType returnType, System.Numerics.BigInteger maskLambdas, int arityMin, int arityMax, params DType[] argTypes)
                : base(name, (l) => "MockFunction", category, returnType, maskLambdas, arityMin, arityMax, argTypes)
            {
                _flags = flags;
                _runtimeFunctionNameSuffix = runtimeFunctionNameSuffix;
                ScopeInfo = new FunctionScopeInfo(this, supportsAsyncLambdas: _flags.HasFlag(FunctionFlags.SupportsAsyncLambdas));
            }

            public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
            {
                yield break;
            }
        }

        public sealed class MockSilentDelegableFilterFunction : FilterFunctionBase
        {
            private readonly string _runtimeFunctionNameSuffix;

            public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
            {
                yield break;
            }

            public MockSilentDelegableFilterFunction(string name, string runtimeFunctionNameSuffix)
                : base(name, (l) => "MockFunction", FunctionCategories.Table, DType.EmptyTable, -2, 2, int.MaxValue, DType.EmptyTable)
            {
                _runtimeFunctionNameSuffix = runtimeFunctionNameSuffix;
                ScopeInfo = new FunctionScopeInfo(this, acceptsLiteralPredicates: false);
            }

            public override bool IsServerDelegatable(CallNode callNode, TexlBinding binding)
            {
                if (!TryGetValidDataSourceForDelegation(callNode, binding, FunctionDelegationCapability, out IExternalDataSource dataSource))
                {
                    if (dataSource != null && !dataSource.IsDelegatable)
                    {
                        return false;
                    }
                }

                var args = callNode.Args.Children.VerifyValue();

                if (dataSource != null && dataSource.DelegationMetadata != null)
                {
                    var metadata = dataSource.DelegationMetadata.FilterDelegationMetadata;
                    if (!IsValidDelegatableFilterPredicateNode(args[1], binding, metadata, false))
                    {
                        return false;
                    }
                }

                return false;
            }
        }

        [Flags]
        public enum FunctionFlags : uint
        {
            None = 0x0,
            IsAsync = 0x1,
            IsBehaviorOnly = 0x2,
            IsStateless = 0x4,
            IsSelfContained = 0x8,
            ManipulatesCollections = 0x20,
            AffectsCollectionSchemas = 0x40,
            IsStrict = 0x80,
            SupportsParamCoercion = 0x100,
            SupportsAsyncLambdas = 0x200,
        }
    }

    internal static class TestExtensions
    {
        internal static bool TryParse2(string typeSpec, out DType type)
        {
            Contracts.AssertNonEmpty(typeSpec);

            return DTypeSpecParser2.TryParse(new DTypeSpecLexer2(typeSpec), out type);
        }

        internal static class DTypeSpecParser2
        {
            internal const string _typeEncodings = DTypeSpecParser._typeEncodings;
            internal static readonly DType[] _types = DTypeSpecParser._types;

            // Parses a type specification, returns true and sets 'type' on success.
            public static bool TryParse(DTypeSpecLexer2 lexer, out DType type)
            {
                Contracts.AssertValue(lexer);
                Contracts.Assert(_typeEncodings.Length == _types.Length);
                Contracts.Assert(_typeEncodings.ToCharArray().Zip(_types, (c, t) => DType.MapKindToStr(t.Kind) == c.ToString()).All(x => x));

                if (!lexer.TryNextToken(out var token) || token.Length != 1)
                {
                    type = DType.Invalid;
                    return false;
                }

                // Older documents may use an "a" type, which for legacy reasons is a duplicate of "o" type.
                if (token == DType.MapKindToStr(DKind.LegacyBlob))
                {
                    token = DType.MapKindToStr(DKind.Blob);
                }

                // Note that control types "v" or "E" are parsed to Error, since the type spec language is not a mechanism for serializing/deserializing controls.
                if (token == DType.MapKindToStr(DKind.Control) || token == DType.MapKindToStr(DKind.DataEntity))
                {
                    type = DType.Error;
                    return true;
                }

                var typeIdx = _typeEncodings.IndexOf(token, StringComparison.Ordinal);
                if (typeIdx < 0)
                {
                    type = DType.Invalid;
                    return false;
                }

                Contracts.AssertIndex(typeIdx, _types.Length);
                var result = _types[typeIdx];

                if (result == DType.ObjNull)
                {
                    // For null value
                    type = result;
                    return true;
                }

                if (!result.IsAggregate)
                {
                    if (result.IsEnum)
                    {
                        if (!TryParse(lexer, out var enumSupertype) ||
                            (!enumSupertype.IsPrimitive && !enumSupertype.IsUnknown) ||
                            !TryParseValueMap(lexer, out var valueMap))
                        {
                            type = DType.Invalid;
                            return false;
                        }

                        // For enums
                        type = new DType(enumSupertype.Kind, valueMap);
                        return true;
                    }

                    // For non-enums, non-aggregates
                    type = result;
                    return true;
                }

                Contracts.Assert(result.IsRecord || result.IsTable);

                if (!TryParseTypeMap(lexer, out var typeMap, out var displayNameProvider))
                {
                    type = DType.Invalid;
                    return false;
                }

                type = new DType(result.Kind, typeMap, null, displayNameProvider);
                return true;
            }

            // Parses a typed name map specification, returns true and sets 'map' on success.
            // A map specification has the form: [name:type, ...]
            private static bool TryParseTypeMap(DTypeSpecLexer2 lexer, out TypeTree map, out DisplayNameProvider displayNameProvider2)
            {
                Contracts.AssertValue(lexer);

                SingleSourceDisplayNameProvider displayNameProvider = new SingleSourceDisplayNameProvider();
                displayNameProvider2 = displayNameProvider;

                if (!lexer.TryNextToken(out var token) || token != "[")
                {
                    map = default;
                    return false;
                }

                map = new TypeTree();

                while (lexer.TryNextToken(out token) && token != "]")
                {
                    string name = token;
                    string displayName = null;

                    if (name.Contains('`'))
                    {
                        var parts = name.Split('`');
                        name = parts[0];
                        displayName = parts[1];
                    }

                    if (name.Length >= 2 && name.StartsWith("'", StringComparison.Ordinal) && name.EndsWith("'", StringComparison.Ordinal))
                    {
                        name = TexlLexer.UnescapeName(name);
                    }

                    if (!DName.IsValidDName(name) ||
                        !lexer.TryNextToken(out token) ||
                        token != ":" ||
                        map.Contains(name) ||
                        !TryParse(lexer, out var type))
                    {
                        map = default;
                        displayNameProvider2 = displayNameProvider;
                        return false;
                    }

                    map = map.SetItem(name, type);
                    displayNameProvider = displayNameProvider.AddField(new DName(name), new DName(string.IsNullOrEmpty(displayName) ? name : displayName));

                    if (!lexer.TryNextToken(out token) || (token != "," && token != "]"))
                    {
                        map = default;
                        displayNameProvider2 = displayNameProvider;
                        return false;
                    }
                    else if (token == "]")
                    {
                        displayNameProvider2 = displayNameProvider;
                        return true;
                    }
                }

                if (token != "]")
                {
                    map = default;
                    displayNameProvider2 = displayNameProvider;
                    return false;
                }

                displayNameProvider2 = displayNameProvider;
                return true;
            }

            // Parses a value map specification, returns true and sets 'map' on success.
            // A map specification has the form: [name:value, ...]
            private static bool TryParseValueMap(DTypeSpecLexer2 lexer, out ValueTree map)
            {
                Contracts.AssertValue(lexer);

                if (!lexer.TryNextToken(out var token) || token != "[")
                {
                    map = default;
                    return false;
                }

                map = new ValueTree();

                while (lexer.TryNextToken(out token) && token != "]")
                {
                    var name = token;
                    if (name.Length >= 2 && name.StartsWith("'", StringComparison.Ordinal) && name.EndsWith("'", StringComparison.Ordinal))
                    {
                        name = name.TrimStart('\'').TrimEnd('\'');
                    }

                    if (!lexer.TryNextToken(out token) || token != ":" ||
                        !TryParseEquatableObject(lexer, out var value))
                    {
                        map = default;
                        return false;
                    }

                    map = map.SetItem(name, value);

                    if (!lexer.TryNextToken(out token) || (token != "," && token != "]"))
                    {
                        map = default;
                        return false;
                    }
                    else if (token == "]")
                    {
                        return true;
                    }
                }

                if (token != "]")
                {
                    map = default;
                    return false;
                }

                return true;
            }

            // Only primitive values are supported:
            //  - strings, such as "hello", etc.
            //  - numbers, such as 123.66124, etc.
            //  - booleans: true and false.
            private static bool TryParseEquatableObject(DTypeSpecLexer2 lexer, out EquatableObject value)
            {
                Contracts.AssertValue(lexer);

                if (!lexer.TryNextToken(out var token) || token.Length == 0)
                {
                    value = default;
                    return false;
                }

                // String support
                if (token[0] == '"')
                {
                    var tokenLen = token.Length;
                    if (tokenLen < 2 || token[tokenLen - 1] != '"')
                    {
                        value = default;
                        return false;
                    }

                    value = new EquatableObject(token.Substring(1, tokenLen - 2));
                    return true;
                }

                // Number (hex) support
                if (token[0] == '#' && token.Length > 1)
                {
                    if (uint.TryParse(token.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var intValue))
                    {
                        value = new EquatableObject((double)intValue);
                        return true;
                    }

                    value = default;
                    return false;
                }

                // Number (double) support
                if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var numValue))
                {
                    value = new EquatableObject(numValue);
                    return true;
                }

                // Boolean support
                if (bool.TryParse(token, out var boolValue))
                {
                    value = new EquatableObject(boolValue);
                    return true;
                }

                value = default;
                return false;
            }
        }

        internal sealed class DTypeSpecLexer2
        {
            private int _cursor;
            private readonly string _typeSpec;

            public DTypeSpecLexer2(string typeSpec)
            {
                Contracts.AssertNonEmpty(typeSpec);
                _typeSpec = typeSpec;
                _cursor = 0;
            }

            public bool Eol => _cursor >= _typeSpec.Length;

            private char CurChar
            {
                get
                {
                    Contracts.Assert(!Eol);
                    return _typeSpec[_cursor];
                }
            }

            public bool TryNextToken(out string token)
            {
                while (!Eol && CharacterUtils.IsSpace(CurChar))
                {
                    ++_cursor;
                }

                if (Eol)
                {
                    token = null;                    
                    return false;
                }

                const string punctuators = "*!%:[],";
                if (punctuators.Contains(CurChar))
                {
                    token = CurChar.ToString();
                    _cursor++;
                }
                else
                {
                    var tok = new StringBuilder();

                    var quote = '0';
                    while (!Eol)
                    {
                        var c = CurChar;
                        if ((c == '"' && (quote == '"' || quote == '0')) ||
                            (c == '\'' && (quote == '\'' || quote == '0')) ||
                            (c == '`' && (quote == '`' || quote == '0')))
                        {
                            if (quote == '0')
                            {
                                quote = c;
                            }
                            else
                            {
                                tok.Append(c);
                                ++_cursor;

                                // If the quote character is not being escaped (examples of
                                // escaping: 'apos''trophe', or "quo""te"), then we end the token.
                                if (Eol || CurChar != c)
                                {
                                    quote = '0';
                                    break;
                                }

                                // else we let the fall-through logic append c once more.
                            }
                        }
                        else if ((quote == '0') && (CharacterUtils.IsSpace(c) || punctuators.Contains(c)))
                        {
                            break;
                        }

                        tok.Append(c);
                        ++_cursor;
                    }

                    if (quote != '0')
                    {
                        token = null;                        
                        return false;
                    }

                    token = tok.ToString();
                }

                while (!Eol && CharacterUtils.IsSpace(CurChar))
                {
                    ++_cursor;
                }
                
                return true;
            }
        }
    }
}
