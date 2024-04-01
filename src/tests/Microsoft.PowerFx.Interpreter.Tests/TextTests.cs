// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
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
    }
}
