// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class BlobTests
    {
        private readonly RecalcEngine _engine;
        private readonly SymbolTable _symbolTable;
        private readonly SymbolValues _symbolValues;
        private readonly RuntimeConfig _runtimeConfig;

        private class BlobCreationFunction : ReflectionFunction
        {
            public BlobValue Execute(StringValue id)
            {
                return FormulaValue.NewBlob(id.Value);
            }
        }

        public BlobTests() 
        {
            var config = new PowerFxConfig();
            config.EnableSetFunction();
            _engine = new RecalcEngine(config);
            _symbolTable = new SymbolTable();
            _symbolValues = new SymbolValues(_symbolTable);
            _runtimeConfig = new RuntimeConfig(_symbolValues);

            _symbolTable.AddFunction(new BlobCreationFunction());
        }

        [Fact]
        public async void CreateBlobTest()
        {
            _symbolTable.AddVariable("b", FormulaType.Blob, mutable: true);
            var opts = new ParserOptions() { AllowsSideEffects = true };
            var evaluator = _engine.Check("Set(b, BlobCreation(\"myid\"))", opts, _symbolTable).GetEvaluator();
            var result = await evaluator.EvalAsync(default, _runtimeConfig).ConfigureAwait(false);

            // Set() returns constant 'true;
            Assert.Equal(true, result.ToObject());

            evaluator = _engine.Check("b", opts, _symbolTable).GetEvaluator();
            result = await evaluator.EvalAsync(default, _runtimeConfig).ConfigureAwait(false);
            var blobValue = result as BlobValue;
            Assert.NotNull(blobValue);
            Assert.Equal("myid", blobValue.Value);
        }
    }
}
