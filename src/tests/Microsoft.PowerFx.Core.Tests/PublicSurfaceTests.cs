// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class PublicSurfaceTests
    {
        private static readonly Type TexlNodeType = typeof(Syntax.Nodes.TexlNode);

        [Fact]
        public void Test()
        {
            var asm = typeof(Parser.TexlParser).Assembly;

            var allowed = new HashSet<string>()
            {
                "Microsoft.PowerFx.FeatureFlags",
                "Microsoft.PowerFx.Core.Texl.Intellisense.IIntellisenseResult",
                "Microsoft.PowerFx.Core.Texl.Intellisense.IIntellisenseSuggestion",
                "Microsoft.PowerFx.Core.Texl.Intellisense.SuggestionIconKind",
                "Microsoft.PowerFx.Core.Texl.Intellisense.SuggestionKind",
                "Microsoft.PowerFx.Core.Texl.Intellisense.UIString",
                "Microsoft.PowerFx.Core.Texl.Intellisense.SignatureHelp.ParameterInformation",
                "Microsoft.PowerFx.Core.Texl.Intellisense.SignatureHelp.SignatureHelp",
                "Microsoft.PowerFx.Core.Texl.Intellisense.SignatureHelp.SignatureInformation",
                "Microsoft.PowerFx.Core.BuiltinFormulaTypeConversions",
                "Microsoft.PowerFx.Core.PowerFxConfig",
                "Microsoft.PowerFx.Core.Public.CheckResult",
                "Microsoft.PowerFx.Core.Public.ErrorKind",
                "Microsoft.PowerFx.Core.Public.ExpressionError",
                "Microsoft.PowerFx.Core.Public.FormulaWithParameters",
                "Microsoft.PowerFx.Core.Public.IExpression",
                "Microsoft.PowerFx.Core.Public.IPowerFxEngine",
                "Microsoft.PowerFx.Core.Public.IPowerFxScope",
                "Microsoft.PowerFx.Core.Public.IPowerFxScopeDisplayName",
                "Microsoft.PowerFx.Core.Public.TokenResultType",
                "Microsoft.PowerFx.Core.Public.Values.BlankValue",
                "Microsoft.PowerFx.Core.Public.Values.BooleanValue",
                "Microsoft.PowerFx.Core.Public.Values.ColorValue",
                "Microsoft.PowerFx.Core.Public.Values.UntypedObjectValue",
                "Microsoft.PowerFx.Core.Public.Values.IUntypedObject",
                "Microsoft.PowerFx.Core.Public.Values.DateTimeValue",
                "Microsoft.PowerFx.Core.Public.Values.DateValue",
                "Microsoft.PowerFx.Core.Public.Values.DValue`1",
                "Microsoft.PowerFx.Core.Public.Values.ErrorValue",
                "Microsoft.PowerFx.Core.Public.Values.FormulaValue",
                "Microsoft.PowerFx.Core.Public.Values.GuidValue",
                "Microsoft.PowerFx.Core.Public.Values.IValueVisitor",
                "Microsoft.PowerFx.Core.Public.Values.NamedValue",
                "Microsoft.PowerFx.Core.Public.Values.NumberValue",
                "Microsoft.PowerFx.Core.Public.Values.OptionSetValue",
                "Microsoft.PowerFx.Core.Public.Values.PrimitiveValue`1",
                "Microsoft.PowerFx.Core.Public.Values.RecordValue",
                "Microsoft.PowerFx.Core.Public.Values.StringValue",
                "Microsoft.PowerFx.Core.Public.Values.TableValue",
                "Microsoft.PowerFx.Core.Public.Values.TimeValue",
                "Microsoft.PowerFx.Core.Public.Values.ValidFormulaValue",
                "Microsoft.PowerFx.Core.Public.Types.AggregateType",
                "Microsoft.PowerFx.Core.Public.Types.BlankType",
                "Microsoft.PowerFx.Core.Public.Types.BooleanType",
                "Microsoft.PowerFx.Core.Public.Types.ColorType",
                "Microsoft.PowerFx.Core.Public.Types.UntypedObjectType",
                "Microsoft.PowerFx.Core.Public.Types.DateTimeNoTimeZoneType",
                "Microsoft.PowerFx.Core.Public.Types.DateTimeType",
                "Microsoft.PowerFx.Core.Public.Types.DateType",
                "Microsoft.PowerFx.Core.Public.Types.FormulaType",
                "Microsoft.PowerFx.Core.Public.Types.GuidType",
                "Microsoft.PowerFx.Core.Public.Types.ITypeVistor",
                "Microsoft.PowerFx.Core.Public.Types.NamedFormulaType",
                "Microsoft.PowerFx.Core.Public.Types.NumberType",
                "Microsoft.PowerFx.Core.Public.Types.OptionSetValueType",
                "Microsoft.PowerFx.Core.Public.Types.RecordType",
                "Microsoft.PowerFx.Core.Public.Types.StringType",
                "Microsoft.PowerFx.Core.Public.Types.TableType",
                "Microsoft.PowerFx.Core.Public.Types.TimeType",
                "Microsoft.PowerFx.Core.Public.Types.HyperlinkType",
                "Microsoft.PowerFx.Core.Public.Types.ExternalTypeKind",
                "Microsoft.PowerFx.Core.Public.Types.ExternalType",
                "Microsoft.PowerFx.Core.Localization.ErrorResourceKey",
                "Microsoft.PowerFx.Core.Localization.Span",
                "Microsoft.PowerFx.Core.Functions.Publish.Capabilities",
                "Microsoft.PowerFx.Core.Functions.DLP.RequiredDataSourcePermissions",
                "Microsoft.PowerFx.Core.Errors.DocumentErrorSeverity",
                "Microsoft.PowerFx.Core.App.IExternalEnabledFeatures",
                "Microsoft.PowerFx.Core.App.DefaultEnabledFeatures",
                "Microsoft.PowerFx.Core.Utils.DName",
                "Microsoft.PowerFx.Core.Utils.DPath",
                "Microsoft.PowerFx.Core.Utils.ICheckable",
                "Microsoft.PowerFx.Core.NameCollisionException",
                "Microsoft.PowerFx.Core.FormulaTypeSchema",
                "Microsoft.PowerFx.Core.FormulaTypeToSchemaConverter",
                "Microsoft.PowerFx.Core.Syntax.Identifier",
                "Microsoft.PowerFx.Core.Syntax.NodeKind",
                "Microsoft.PowerFx.Core.Syntax.Visitors.TexlFunctionalVisitor`2",
                "Microsoft.PowerFx.Core.Syntax.Visitors.TexlVisitor",
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
                "Microsoft.PowerFx.Core.Syntax.Nodes.ReplaceableNode",
                "Microsoft.PowerFx.Core.Syntax.Nodes.SelfNode",
                "Microsoft.PowerFx.Core.Syntax.Nodes.StrInterpNode",
                "Microsoft.PowerFx.Core.Syntax.Nodes.StrLitNode",
                "Microsoft.PowerFx.Core.Syntax.Nodes.TableNode",
                "Microsoft.PowerFx.Core.Syntax.Nodes.TexlNode",
                "Microsoft.PowerFx.Core.Syntax.Nodes.UnaryOpNode",
                "Microsoft.PowerFx.Core.Syntax.Nodes.VariadicBase",
                "Microsoft.PowerFx.Core.Syntax.Nodes.VariadicOpNode",
                "Microsoft.PowerFx.Core.Lexer.BinaryOp",
                "Microsoft.PowerFx.Core.Lexer.UnaryOp",
                "Microsoft.PowerFx.Core.Lexer.VariadicOp"
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

        [Fact]
        public void TestTexlNodeTypes()
        {
            var sb = new StringBuilder();

            var asm = typeof(Syntax.Nodes.TexlNode).Assembly;
            var types = asm.GetTypes().Where(IsTexlNodePublicType).ToList();
            Assert.True(types.Count > 0, "No types found");

            foreach (var type in types)
            {
                var fullName = type.FullName;

                // Should be abstract or sealed
                if (!(type.IsSealed || type.IsAbstract))
                {
                    sb.AppendLine($"{fullName} is neither abstract nor sealed");
                }

                // All ctors should be internal
                foreach (var ctor in type.GetConstructors())
                {
                    if (ctor.IsPublic)
                    {
                        sb.AppendLine($"{fullName}.{ctor.Name} constructor is public");
                    }
                }

                foreach (var field in type.GetFields())
                {
                    if (field.IsPublic)
                    {
                        sb.AppendLine($"{fullName}.{field.Name} field is public");
                    }
                }
            }

            Assert.True(sb.Length == 0, $"TexlNode errors: {sb}");
        }

        private static bool IsTexlNodePublicType(Type t) => t.IsPublic && IsSubclassOrEqual(t, TexlNodeType);

        /// <summary>
        ///     Checks whether <see cref="t1" /> is equal to or subclass of to <see cref="t2" />.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        private static bool IsSubclassOrEqual(Type t1, Type t2) => t1.Equals(t2) || t1.IsSubclassOf(t2);
    }
}
