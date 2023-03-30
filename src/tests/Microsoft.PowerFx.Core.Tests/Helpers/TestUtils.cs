// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
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
                        Assert.True(false, $"Property \"{property}\" missing from actual object.");
                    }

                    foreach (var property in actualProperties.Except(expectedProperties))
                    {
                        Assert.True(false, $"Actual object defines additional property \"{property}\".");
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
                    Assert.True(false, $"Unexpected JsonValueKind: JsonValueKind.{valueKind}.");
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
                var wasCoerced = false;
                if (CheckNumericTableOverload)
                {
                    if (CheckColumnType(argTypes[0], args[0], DType.Number, errors, TexlStrings.ErrInvalidSchemaNeedNumCol_Col, ref wasCoerced))
                    {
                        if (wasCoerced)
                        {
                            CollectionUtils.Add(ref coercedArgs, args[0], DType.EmptyTable.Add(new DName("Value"), DType.Number), allowDupes: true);
                        }
                    }
                    else
                    {
                        isValid = false;
                        if (coercedArgs != null)
                        {
                            coercedArgs.Clear();
                        }
                    }

                    return isValid;
                }

                if (CheckStringTableOverload)
                {
                    if (!CheckStringColumnType(argTypes[0], args[0], errors, ref coercedArgs))
                    {
                        isValid = false;
                        if (coercedArgs != null)
                        {
                            coercedArgs.Clear();
                        }
                    }

                    return isValid;
                }

                if (!DType.Number.Accepts(argTypes[0]))
                {
                    isValid = false;
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrNumberExpected);
                    if (coercedArgs != null)
                    {
                        coercedArgs.Clear();
                    }
                }

                return isValid;
            }

            private bool CheckColumnType(DType type, TexlNode arg, DType expectedType, IErrorContainer errors, ErrorResourceKey errKey, ref bool wasCoerced)
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
                else if (!(expectedType.Accepts(columns.Single().Type) || columns.Single().Type.CoercesTo(expectedType)))
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, arg, errKey, columns.Single().Name.Value);
                    return false;
                }

                if (!expectedType.Accepts(columns.Single().Type))
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
            public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
            {
                yield break;
            }

            public MockSilentDelegableFilterFunction(string name)
                : base(name, (l) => "MockFunction", FunctionCategories.Table, DType.EmptyTable, -2, 2, int.MaxValue, DType.EmptyTable)
            {
                ScopeInfo = new FunctionScopeInfo(this, acceptsLiteralPredicates: false);
            }

            public override bool IsServerDelegatable(CallNode callNode, TexlBinding binding)
            {
                IExternalDataSource dataSource = null;

                if ((binding.Document != null && !binding.Document.Properties.EnabledFeatures.IsEnhancedDelegationEnabled) || !TryGetValidDataSourceForDelegation(callNode, binding, FunctionDelegationCapability, out dataSource))
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
}
