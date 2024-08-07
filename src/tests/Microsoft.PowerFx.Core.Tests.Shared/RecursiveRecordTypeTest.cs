// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Syntax;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class RecursiveTypeTest : PowerFxTest
    {
        [Theory]
        [InlineData("Collect(TableLoop, First(TableLoop), First(TableLoop))")]
        public void NoStackoverflowTest(string expression)
        {
            var symbols = ReadOnlySymbolTable.NewFromRecord(new LazyRecursiveRecordType());
            var engine = new Engine();
            engine.Config.SymbolTable.AddFunction(new CollectFunction());

            var check = engine.Check(expression, symbolTable: symbols, options: new ParserOptions() { AllowsSideEffects = true });

            // No stackoverflow has been thrown.
            Assert.False(check.IsSuccess);
        }
    }
}
