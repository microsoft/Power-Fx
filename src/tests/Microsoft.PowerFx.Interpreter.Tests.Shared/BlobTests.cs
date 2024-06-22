// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class BlobTests
    {        
        [Fact]
        public async Task BlobTest_NoCopy()
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
            FormulaValue result = await engine.EvalAsync("Set(var2, var1); var2", CancellationToken.None, new ParserOptions() { AllowsSideEffects = true }, symbolTable, runtimeConfig);

            Assert.Same(blob, result);
        }

        [Fact]
        public async Task BlobTest_CannotConvert()
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

            // IR: If:o(False:b, Lazy(ResolvedObject('var1:SymbolTable_5866477')), Lazy(TextToBlob:o(If:s(False:b, Lazy("a":s), Lazy(BlobToText:s(ResolvedObject('var1:SymbolTable_5866477')))))))                        
            FormulaValue result = await engine.EvalAsync(@"If(false, var1, If(false, ""a"", var1))", CancellationToken.None, new ParserOptions() { AllowsSideEffects = true }, symbolTable, runtimeConfig);

            ErrorValue ev = Assert.IsType<ErrorValue>(result);
            Assert.Equal("Not implemented: Unary op TextToBlob", ev.Errors[0].Message);
        }

        [Fact]
        public void BlobTest_Json()
        {
            // This is "abc😊" in bytes
            string blob = @"AsBlob([Hex2Dec(""61""),Hex2Dec(""62""),Hex2Dec(""63""),Hex2Dec(""F0""),Hex2Dec(""9F""),Hex2Dec(""98""),Hex2Dec(""8A"")])";

            PowerFxConfig config = new PowerFxConfig();
            config.EnableJsonFunctions();
            config.AddFunction(new AsBlobFunctionImpl());
            RecalcEngine engine = new RecalcEngine(config);

            CheckResult checkResult = engine.Check(@$"JSON({blob})");
            Assert.False(checkResult.IsSuccess);
            Assert.Equal("The value passed to the JSON function contains media, and it is not supported by default. To allow JSON serialization of media values, make sure to use the IncludeBinaryData option in the 'format' parameter.", checkResult.Errors.First().Message);
           
            checkResult = engine.Check(@$"JSON({blob}, JSONFormat.IgnoreBinaryData)");
            Assert.True(checkResult.IsSuccess);

            FormulaValue fv = checkResult.GetEvaluator().Eval();
            StringValue sv = Assert.IsType<StringValue>(fv);

            // Power Apps returns ""data:text/plain;base64,YWJj8J+Yig=="" with an equivalent blob
            Assert.Equal(@"""YWJj8J+Yig==""", sv.Value);
        }

        // Derived class to override GetFileInfoAsync
        private class MyBlobValue : BlobValue
        {
            public MyBlobValue()
                : base(new StringBlob("abc"))
            {
            }

            public override async Task<PowerFxFileInfo> GetFileInfoAsync()
            {
                return new PowerFxFileInfo
                {
                    Size = 12,
                    MIMEType = "mime",
                    Name = "name.txt"
                };
            }
        }

        [Theory]
        [InlineData("FileInfo(file).Size", 12)]
        [InlineData("With({x:FileInfo(file)}, x.Size & \",\" & x.Name & \",\" &  x.MIMEType)", "12,name.txt,mime")]
        [InlineData("FileInfo(Blank()).Size", null)]
        [InlineData("FileInfo(If(1/0, file)).Size", "#error")]
        [InlineData("IsError(FileInfo(notFile))", true)]
        public void FileInfoTest(string expr, object expectedValue)
        {
            var config = new PowerFxConfig();
#pragma warning disable CS0618 // Type or member is obsolete
            config.SymbolTable.EnableFileFunctions();
#pragma warning restore CS0618 // Type or member is obsolete
            var engine = new RecalcEngine(config);

            // A blob that supports file 
            var blob = new MyBlobValue();
            engine.UpdateVariable("file", blob);

            // A blob that does not support file. 
            BlobValue blob2 = new BlobValue(new StringBlob("abc"));
            engine.UpdateVariable("notFile", blob2);

            var val = engine.Eval(expr);

            if (expectedValue?.ToString() == "#error")
            {
                Assert.IsType<ErrorValue>(val);
            }
            else
            {
                var objStr = val.ToObject()?.ToString();
                var expectedToStr = expectedValue?.ToString();
                Assert.Equal(expectedToStr, objStr);
            }            
        }

        // Must call EnableFileFunctions() to get file functions. 
        [Fact]
        public void FileInfoTestNotInDefault()
        {
            var config = new PowerFxConfig();
                        
            var engine = new RecalcEngine(config);

            var blob = new MyBlobValue();
            engine.UpdateVariable("blob", blob);

            var check = engine.Check("FileInfo(blob).Size");
            var errors = check.ApplyErrors();

            Assert.NotEmpty(errors);
        }

        internal class AsBlobFunctionImpl : BuiltinFunction, IAsyncTexlFunction5
        {
            public AsBlobFunctionImpl()
                : base("AsBlob", (loc) => "Converts a Table of numbers (byte array) to a Blob.", FunctionCategories.Text, DType.Blob, 0, 1, 2, DType.EmptyTable)
            {
            }

            public override bool IsSelfContained => true;

            public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
            {
                yield return new TexlStrings.StringGetter[] { (loc) => "table" };                
            }

            public Task<FormulaValue> InvokeAsync(IServiceProvider runtimeServiceProvider, FormulaType irContext, FormulaValue[] args, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                return Task.FromResult<FormulaValue>(
                    args[0] is BlankValue || args[0] is BlobValue
                    ? args[0]
                    : args[0] is not TableValue tv
                    ? CommonErrors.RuntimeTypeMismatch(args[0].IRContext)
                    : BlobValue.NewBlob(tv.Rows.Select((DValue<RecordValue> drv) => (byte)(decimal)drv.Value.GetField("Value").ToObject()).ToArray()));
            }
        }
    }
}
