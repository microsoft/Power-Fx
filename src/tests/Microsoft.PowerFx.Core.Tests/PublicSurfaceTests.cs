// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class PublicSurfaceTests
    {
        [Fact]
        public void Test()
        {
            var asm = typeof(Parser.TexlParser).Assembly;

            var allowed = new HashSet<string>()
            {
                // Core namespace. 
                "Microsoft.PowerFx.PowerFxConfig",
                "Microsoft.PowerFx.CheckResult",
                "Microsoft.PowerFx.ParseResult",
                "Microsoft.PowerFx.FunctionInfo",
                "Microsoft.PowerFx.NameCollisionException",
                "Microsoft.PowerFx.ErrorKind",
                "Microsoft.PowerFx.ExpressionError",
                "Microsoft.PowerFx.FormulaWithParameters",
                "Microsoft.PowerFx.Core.Public.IExpression",
                "Microsoft.PowerFx.Core.Public.IExpressionExtensions",
                "Microsoft.PowerFx.IPowerFxEngine",
                "Microsoft.PowerFx.ParserOptions",
                "Microsoft.PowerFx.Engine",
                "Microsoft.PowerFx.ErrorSeverity",

                // Feature flags are experimental - hosts shouldn't use it. 
                "Microsoft.PowerFx.Preview.FeatureFlags",

                // TBD ...
                "Microsoft.PowerFx.Core.RenameDriver",
                "Microsoft.PowerFx.Core.Public.TokenResultType",
                "Microsoft.PowerFx.Core.App.DefaultEnabledFeatures",
                "Microsoft.PowerFx.Core.App.IExternalEnabledFeatures",
                "Microsoft.PowerFx.Core.BuiltinFormulaTypeConversions",
                "Microsoft.PowerFx.Core.DisplayNameUtility",
                "Microsoft.PowerFx.Core.FormulaTypeSchema",
                "Microsoft.PowerFx.Core.FormulaTypeToSchemaConverter",
                "Microsoft.PowerFx.Core.Utils.DName",
                "Microsoft.PowerFx.Core.Utils.DPath",
                "Microsoft.PowerFx.Core.Utils.ICheckable",

                // Lexer and Parser 
                "Microsoft.PowerFx.Core.Lexer.BinaryOp",
                "Microsoft.PowerFx.Core.Lexer.TokKind",
                "Microsoft.PowerFx.Core.Lexer.Tokens.CommentToken",
                "Microsoft.PowerFx.Core.Lexer.Tokens.ErrorToken",
                "Microsoft.PowerFx.Core.Lexer.Tokens.IdentToken",
                "Microsoft.PowerFx.Core.Lexer.Tokens.NumLitToken",
                "Microsoft.PowerFx.Core.Lexer.Tokens.StrLitToken",
                "Microsoft.PowerFx.Core.Lexer.Tokens.Token",
                "Microsoft.PowerFx.Core.Lexer.UnaryOp",
                "Microsoft.PowerFx.Core.Lexer.VariadicOp",
                "Microsoft.PowerFx.Core.Localization.ErrorResourceKey",
                "Microsoft.PowerFx.Core.Localization.Span",

                "Microsoft.PowerFx.Core.Syntax.Identifier",
                "Microsoft.PowerFx.Core.Syntax.NodeKind",
                "Microsoft.PowerFx.Core.Syntax.Nodes.AsNode",
                "Microsoft.PowerFx.Core.Syntax.Nodes.BinaryOpNode",
                "Microsoft.PowerFx.Core.Syntax.Nodes.BlankNode",
                "Microsoft.PowerFx.Core.Syntax.Nodes.BoolLitNode",
                "Microsoft.PowerFx.Core.Syntax.Nodes.CallNode",
                "Microsoft.PowerFx.Core.Syntax.Nodes.DottedNameNode",
                "Microsoft.PowerFx.Core.Syntax.Nodes.ErrorNode",
                "Microsoft.PowerFx.Core.Syntax.Nodes.FirstNameNode",
                "Microsoft.PowerFx.Core.Syntax.Nodes.ListNode",
                "Microsoft.PowerFx.Core.Syntax.Nodes.NameNode",
                "Microsoft.PowerFx.Core.Syntax.Nodes.NumLitNode",
                "Microsoft.PowerFx.Core.Syntax.Nodes.ParentNode",
                "Microsoft.PowerFx.Core.Syntax.Nodes.RecordNode",
                "Microsoft.PowerFx.Core.Syntax.Nodes.SelfNode",
                "Microsoft.PowerFx.Core.Syntax.Nodes.StrInterpNode",
                "Microsoft.PowerFx.Core.Syntax.Nodes.StrLitNode",
                "Microsoft.PowerFx.Core.Syntax.Nodes.TableNode",
                "Microsoft.PowerFx.Core.Syntax.Nodes.TexlNode",
                "Microsoft.PowerFx.Core.Syntax.Nodes.UnaryOpNode",
                "Microsoft.PowerFx.Core.Syntax.Nodes.VariadicBase",
                "Microsoft.PowerFx.Core.Syntax.Nodes.VariadicOpNode",

                // $$$ move to syntax 
                "Microsoft.PowerFx.Core.Syntax.Visitors.IdentityTexlVisitor",
                "Microsoft.PowerFx.Core.Syntax.Visitors.TexlFunctionalVisitor`2",
                "Microsoft.PowerFx.Core.Syntax.Visitors.TexlVisitor",

                // $$$ Microsoft.PowerFx.Types
                "Microsoft.PowerFx.Core.Public.Types.AggregateType",
                "Microsoft.PowerFx.Core.Public.Types.BlankType",
                "Microsoft.PowerFx.Core.Public.Types.BooleanType",
                "Microsoft.PowerFx.Core.Public.Types.ColorType",
                "Microsoft.PowerFx.Core.Public.Types.DateTimeNoTimeZoneType",
                "Microsoft.PowerFx.Core.Public.Types.DateTimeType",
                "Microsoft.PowerFx.Core.Public.Types.DateType",
                "Microsoft.PowerFx.Core.Public.Types.ExternalType",
                "Microsoft.PowerFx.Core.Public.Types.ExternalTypeKind",
                "Microsoft.PowerFx.Core.Public.Types.FormulaType",
                "Microsoft.PowerFx.Core.Public.Types.GuidType",
                "Microsoft.PowerFx.Core.Public.Types.HyperlinkType",
                "Microsoft.PowerFx.Core.Public.Types.ITypeVistor",
                "Microsoft.PowerFx.Core.Public.Types.NamedFormulaType",
                "Microsoft.PowerFx.Core.Public.Types.NumberType",
                "Microsoft.PowerFx.Core.Public.Types.OptionSetValueType",
                "Microsoft.PowerFx.Core.Public.Types.RecordType",
                "Microsoft.PowerFx.Core.Public.Types.StringType",
                "Microsoft.PowerFx.Core.Public.Types.TableType",
                "Microsoft.PowerFx.Core.Public.Types.TimeType",
                "Microsoft.PowerFx.Core.Public.Types.BindingErrorType",
                "Microsoft.PowerFx.Core.Public.Types.UnknownType",
                "Microsoft.PowerFx.Core.Public.Types.UntypedObjectType",
                "Microsoft.PowerFx.Core.Public.Values.BlankValue",
                "Microsoft.PowerFx.Core.Public.Values.BooleanValue",
                "Microsoft.PowerFx.Core.Public.Values.ColorValue",
                "Microsoft.PowerFx.Core.Public.Values.DValue`1",
                "Microsoft.PowerFx.Core.Public.Values.DateTimeValue",
                "Microsoft.PowerFx.Core.Public.Values.DateValue",
                "Microsoft.PowerFx.Core.Public.Values.ErrorValue",
                "Microsoft.PowerFx.Core.Public.Values.FormulaValue",
                "Microsoft.PowerFx.Core.Public.Values.GuidValue",
                "Microsoft.PowerFx.Core.Public.Values.IUntypedObject",
                "Microsoft.PowerFx.Core.Public.Values.IValueVisitor",
                "Microsoft.PowerFx.Core.Public.Values.NamedValue",
                "Microsoft.PowerFx.Core.Public.Values.NumberValue",
                "Microsoft.PowerFx.Core.Public.Values.OptionSetValue",
                "Microsoft.PowerFx.Core.Public.Values.PrimitiveValue`1",
                "Microsoft.PowerFx.Core.Public.Values.RecordValue",
                "Microsoft.PowerFx.Core.Public.Values.StringValue",
                "Microsoft.PowerFx.Core.Public.Values.TableValue",
                "Microsoft.PowerFx.Core.Public.Values.TimeValue",
                "Microsoft.PowerFx.Core.Public.Values.UntypedObjectValue",
                "Microsoft.PowerFx.Core.Public.Values.ValidFormulaValue",

                "Microsoft.PowerFx.Intellisense.IPowerFxScope",
                "Microsoft.PowerFx.Intellisense.IPowerFxScopeDisplayName",
                "Microsoft.PowerFx.Intellisense.IIntellisenseResult",
                "Microsoft.PowerFx.Intellisense.IIntellisenseSuggestion",
                "Microsoft.PowerFx.Intellisense.SignatureHelp.ParameterInformation",
                "Microsoft.PowerFx.Intellisense.SignatureHelp.SignatureHelp",
                "Microsoft.PowerFx.Intellisense.SignatureHelp.SignatureInformation",
                "Microsoft.PowerFx.Intellisense.SuggestionIconKind",
                "Microsoft.PowerFx.Intellisense.SuggestionKind",
                "Microsoft.PowerFx.Intellisense.UIString",
            };

            var sb = new StringBuilder();
            var count = 0;
            foreach (var type in asm.GetTypes().Where(t => t.IsPublic))
            {
                var name = type.FullName;
                if (!allowed.Contains(name))
                {
                    sb.AppendLine(name);
                    count++;
                }

                allowed.Remove(name);
            }

            Assert.True(count == 0, $"Unexpected public types: {sb}");

            // Types we expect to be in the assembly are all there. 
            Assert.Empty(allowed);
        }

        // No public type with TransportType attribute
        [Fact]
        public void NoTransportInPublicTypes()
        {
            var exceptionList = new HashSet<string>()
            {
                "Microsoft.PowerFx.Core.Localization.Span"
            };

            var asm = typeof(Parser.TexlParser).Assembly;
            foreach (var type in asm.GetTypes().Where(t => t.IsPublic))
            {
                if (exceptionList.Contains(type.FullName))
                {
                    continue;
                }

                var attrs = type.GetCustomAttributesData();

                var hasTransport = attrs.Any(attr => attr.AttributeType.Namespace.StartsWith("Microsoft.AppMagic.Transport"));
                Assert.False(hasTransport, $"Types '{type.FullName}' with Transport attribute shouldn't be public.");
            }
        }

        // Assert DocumentErrorSeverity and ErrorSeverity are in sync. 
        [Fact]
        public void ErrorSeverity()
        {
            var values1 = Enum.GetValues(typeof(Errors.DocumentErrorSeverity));
            var values2 = Enum.GetValues(typeof(ErrorSeverity));

            var len = values1.Length;
            Assert.Equal(len, values2.Length);
            
            for (var i = 0; i < len; i++)
            {
                var x = values1.GetValue(i);
                var y = values1.GetValue(i);

                Assert.Equal((int)x, (int)y);
                Assert.Equal(x.ToString(), y.ToString());
            }
        }

        [Fact]
        public void TestTexlNodeTypes() => TestPublicClassHierarchy(typeof(TexlNode));

        [Fact]
        public void TestTokenTypes() => TestPublicClassHierarchy(typeof(Token), requireAbstractOrSealed: false);

        private static void TestPublicClassHierarchy(Type rootType, bool requireAbstractOrSealed = true)
        {
            var errors = new StringBuilder();

            var asm = rootType.Assembly;
            var types = asm.GetTypes().Where(t => IsPublicSubclassOrEqual(t, rootType)).ToList();
            Assert.True(types.Count > 0, "No types found");

            foreach (var type in types)
            {
                var fullName = type.FullName;

                // Should be abstract or sealed
                if (requireAbstractOrSealed && !(type.IsSealed || type.IsAbstract))
                {
                    errors.AppendLine($"{fullName} is neither abstract nor sealed");
                }

                // Should have immutable attribute
                if (type.GetCustomAttribute<ThreadSafeImmutableAttribute>() is null)
                {
                    errors.AppendLine($"{fullName} does not have [ThreadSafeImmutable]");
                }

                // All ctors should be internal
                foreach (var ctor in type.GetConstructors())
                {
                    if (ctor.IsPublic)
                    {
                        errors.AppendLine($"{fullName}.{ctor.Name} constructor is public");
                    }
                }

                // Should not have public fields
                foreach (var field in type.GetFields())
                {
                    if (field.IsPublic)
                    {
                        errors.AppendLine($"{fullName}.{field.Name} field is public");
                    }
                }
            }

            Assert.True(errors.Length == 0, $"TexlNode errors: {errors}");
        }

        [Fact]
        public static void TestImmutability()
        {
            var asm = typeof(Syntax.Nodes.TexlNode).Assembly;
            ImmutabilityTests.CheckImmutability(asm);
        }

        /// <summary>
        ///     Checks whether <see cref="t1" /> is public, and equal to or subclass of to <see cref="t2" />.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        private static bool IsPublicSubclassOrEqual(Type t1, Type t2) => t1.IsPublic && (t1.Equals(t2) || t1.IsSubclassOf(t2));
    }
}
