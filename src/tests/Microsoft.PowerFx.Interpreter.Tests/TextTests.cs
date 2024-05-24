// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class InterpreterTextTests
    {
        // internally, we shouldn't throw an exception if the C# formatter used by the interpreter sees something it doesn't understand
        // instead, return an IllegalArgument runtime error
        [Fact]
        public void TextCatchIllegalFormatException()
        {
            var call = new CallNode(
                IRContext.NotInSource(FormulaType.String),
                BuiltinFunctionsCore.Text,
                new List<IntermediateNode>()
                {
                    new NumberLiteralNode(IRContext.NotInSource(FormulaType.Number), 12),
                    new TextLiteralNode(IRContext.NotInSource(FormulaType.String), "i")
                });

            var symbols = new SymbolTable();
            var values = symbols.CreateValues();

            var pe = new ParsedExpression(call, null, new StackDepthCounter(10))
            {
                _allSymbols = symbols,
                _parameterSymbolTable = symbols,
                _globals = values,
                _additionalFunctions = new Dictionary<TexlFunction, IAsyncTexlFunction>(),
            };

            // This test 
            var result = pe.Eval();

            Assert.True(result is ErrorValue ev && ev.Errors.Count == 1 && ev.Errors[0].Kind == ErrorKind.InvalidArgument);
        }

        [Fact]
        public void ZeroWidthSpaceCharactersPFxV1Engine()
        {
            var engine = new RecalcEngine(new PowerFxConfig(Features.None));
            var enginePFx1 = new RecalcEngine();

            var content = "AA" + char.ConvertFromUtf32(8203) + "BB";
            var expression = $"\"{content}\"";

            // If PFxV1 is false, the zero width space character will be removed from the result string
            var check = engine.Check(expression);
            var result = (StringValue)check.GetEvaluator().Eval();
            Assert.NotEqual(content, result.Value);
            Assert.Equal(4, result.Value.Length);

            // If PFxV1 is not true, the zero width space character will be kept in the result string
            check = enginePFx1.Check(expression);
            result = (StringValue)check.GetEvaluator().Eval();
            Assert.Equal(content, result.Value);
            Assert.Equal(5, result.Value.Length);
        }
    }
}
