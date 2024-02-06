// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class BlobTests
    {        
        [Fact]
        public void BlobTest_NoCopy()
        {
            PowerFxConfig config = new PowerFxConfig();
            config.EnableSetFunction();            

            SymbolTable symbolTable = new SymbolTable();
            ISymbolSlot var1Slot = symbolTable.AddVariable("var1", FormulaType.Blob, false, "var1");
            ISymbolSlot var2Slot = symbolTable.AddVariable("var2", FormulaType.Blob, true, "var2");

            BlobValue blob = new BlobValue(new StringBlob("abc"));
            SymbolValues symbolValues = new SymbolValues(symbolTable);
            symbolValues.Set(var1Slot, blob);
            symbolValues.Set(var2Slot, FormulaValue.NewBlank(FormulaType.Blob));

            RuntimeConfig runtimeConfig = new RuntimeConfig(symbolValues);
            RecalcEngine engine = new RecalcEngine(config);
            FormulaValue result = engine.EvalAsync("Set(var2, var1); var2", CancellationToken.None, new ParserOptions() { AllowsSideEffects = true }, symbolTable, runtimeConfig).Result;

            Assert.Same(blob, result);
        }

        [Fact]
        public void BlobTest_CannotConvert()
        {
            PowerFxConfig config = new PowerFxConfig();
            config.EnableSetFunction();

            SymbolTable symbolTable = new SymbolTable();
            ISymbolSlot var1Slot = symbolTable.AddVariable("var1", FormulaType.Blob, false, "var1");            

            BlobValue blob = new BlobValue(new StringBlob("abc"));
            SymbolValues symbolValues = new SymbolValues(symbolTable);
            symbolValues.Set(var1Slot, blob);            

            RuntimeConfig runtimeConfig = new RuntimeConfig(symbolValues);
            RecalcEngine engine = new RecalcEngine(config);
            FormulaValue result = engine.EvalAsync(@"If(false, var1, If(false, ""a"", var1))", CancellationToken.None, new ParserOptions() { AllowsSideEffects = true }, symbolTable, runtimeConfig).Result;

            ErrorValue ev = Assert.IsType<ErrorValue>(result);
            Assert.Equal("Cannot convert Blob to Text", ev.Errors[0].Message);
        }
    }
}
