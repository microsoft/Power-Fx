// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Types.Enums;
using Xunit;
using CallNode = Microsoft.PowerFx.Core.IR.Nodes.CallNode;

namespace Microsoft.PowerFx.Core.Tests
{
    public class IRTranslatorTests
    {
        private readonly EnumStore _enumStore = new EnumStore();

        [Theory]
        [InlineData("CountIf(numtable, val > 0)", ">")]
        [InlineData("Sum(numtable, Sum(val,0))", "Sum(val,0)")]
        public void TestLazyEvalNodeSourceSpan(string expression, string expectedFragment)
        {
            var parameterType = new RecordType();

            var tableType = new TableType();
            tableType = tableType.Add(new NamedFormulaType("val", FormulaType.Number));

            parameterType = parameterType.Add(new NamedFormulaType("numtable", tableType));

            var formula = new Formula(expression);
            formula.EnsureParsed(TexlParser.Flags.None);

            var binding = TexlBinding.Run(
                new Glue2DocumentBinderGlue(),
                formula.ParseTree,
                new SimpleResolver(_enumStore.EnumSymbols),
                ruleScope: parameterType._type,
                useThisRecordForRuleScope: false);

            Assert.False(formula.HasParseErrors);
            Assert.False(binding.ErrorContainer.HasErrors());

            (var irNode, var ruleScopeSymbol) = IRTranslator.Translate(binding);
           
            var callNode = (CallNode)irNode;

            Assert.True(callNode.IsLambdaArg(1));

            callNode.TryGetArgument(1, out var lazyEvalNode);

            Assert.True(lazyEvalNode.GetType() == typeof(LazyEvalNode));

            // SourceSpan Check
            var fragment = lazyEvalNode.IRContext.SourceContext.GetFragment(expression);
            Assert.Equal(expectedFragment, fragment);
        }
    }
}
