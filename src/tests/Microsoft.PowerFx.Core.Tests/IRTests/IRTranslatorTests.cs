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
        [InlineData("CountIf(numtable, val > 0)", ">", typeof(BooleanType))]
        [InlineData("Sum(numtable, Sum(val,0))", "Sum(val,0)", typeof(NumberType))]
        public void TestLazyEvalNode(string expression, string expectedFragment, Type type)
        {
            var tableType = new TableType()
                .Add(new NamedFormulaType("val", FormulaType.Number));

            var parameterType = new RecordType()
                .Add(new NamedFormulaType("numtable", tableType));

            var engine = new Engine(new PowerFxConfig());
            var result = engine.Check(expression, parameterType);
            result.ThrowOnErrors();
            
            (var irNode, var ruleScopeSymbol) = IRTranslator.Translate(result._binding);
           
            var callNode = (CallNode)irNode;

            Assert.True(callNode.IsLambdaArg(1));

            callNode.TryGetArgument(1, out var lazyEvalNode);

            Assert.IsType<LazyEvalNode>(lazyEvalNode);

            // Span Check
            var fragment = lazyEvalNode.IRContext.SourceContext.GetFragment(expression);
            Assert.Equal(expectedFragment, fragment);

            // Type Check
            Assert.Equal(type, lazyEvalNode.IRContext.ResultType.GetType());
        }
    }
}
