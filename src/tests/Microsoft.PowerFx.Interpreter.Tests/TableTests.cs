// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class TableTests
    {
        [Fact]
        public async Task TestTableWithEmptyRecords()
        {
            var pfxConfig = new PowerFxConfig();
            var recalcEngine = new RecalcEngine(pfxConfig);

            var table = await recalcEngine.EvalAsync("Table({}, {}, {})", CancellationToken.None).ConfigureAwait(false);

            Assert.NotNull(table);

            var t = table.ToObject();

            Assert.NotNull(t);
            Assert.Equal(3, ((object[])t).Length);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Powerfx_Partial_Collect(bool serialize)
        {
            FormulaValue result = Run_Collect_Worflow(serialize);
            Assert.Equal("Blank()", result.ToString());
        }

        private FormulaValue Run_Collect_Worflow(bool serialize)
        {
            // Define initial FormulaValue
            RecordType schemaType = RecordType.Empty();
            schemaType = schemaType.Add("Col1", FormulaType.String);
            schemaType = schemaType.Add("Col2", FormulaType.String);
            var list = new List<RecordValue>();
            var fields = new List<NamedValue>
            {
                new NamedValue("Col1", FormulaValue.New("Col1")),
                new NamedValue("Col2", FormulaValue.New("Col2"))
            };
            list.Add(FormulaValue.NewRecordFromFields(schemaType, fields));
            FormulaValue formulaValue = FormulaValue.NewTable(schemaType, list);

            var recalEngine = new RecalcEngine();
            var result = (StringValue)RunExpr("Last(collection).Col2", recalEngine, false, formulaValue, "collection");
            Assert.Equal("Col2", result.Value);

            // Getting the FormulaValue that is used to update variable for the next expression
            // formulaValue is of type RecordsOnlyTableValuee
            formulaValue = recalEngine.GetValue("collection");

            recalEngine = new RecalcEngine();
            if (serialize)
            {
                // Serializing it as SetPowerCardMemory does and stores it in memoryContext
                string serializedFormulaValue = formulaValue.ToExpression();

                // Deserializing it
                // After serialization and deserialization, the formulaValue has type of InMemoryTableValue
                formulaValue = RunExpr(serializedFormulaValue, recalEngine, true);
            }

            RunExpr("Collect(collection, {Col1:\"newCol1\"})", recalEngine, false, formulaValue, "collection");
            return RunExpr("Last(collection).Col2", recalEngine, true);
        }

        private FormulaValue RunExpr(string expressionText, RecalcEngine engine, bool setVariableValueInEngine, FormulaValue formulaValue = null, string varName = "")
        {
            // Parser options for RecalEngine
            var parserOptions = new ParserOptions()
            {
                AllowsSideEffects = true,
                NumberIsFloat = true
            };

            // Define symbol table
            var symbolTable = new SymbolTable();
            symbolTable.EnableMutationFunctions();

            if (!setVariableValueInEngine)
            {
                engine.UpdateVariable(varName, formulaValue);
            }

            var check = engine.Check(expressionText, parserOptions, symbolTable: symbolTable);
            Assert.True(check.IsSuccess);
            var run = check.GetEvaluator();

            return run.Eval();
        }
    }
}
