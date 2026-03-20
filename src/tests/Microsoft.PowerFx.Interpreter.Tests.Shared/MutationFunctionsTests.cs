// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Tests.AssociatedDataSourcesTests;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class MutationFunctionsTests : PowerFxTest
    {
        private readonly ParserOptions _opts = new ParserOptions { AllowsSideEffects = true };

        [Theory]

        // Collect functions
        [InlineData("Collect(t, Blank()).F2", "Collect(t, Blank()).Display2", true)]
        [InlineData("Collect(t, {Display1:1/0}).F0", "Collect(t, {Display1:1/0}).Display0", true)]
        [InlineData("Collect(t, {F0:0}).F3", "Collect(t, {Display0:0}).F3", true)]
        [InlineData("Collect(t, {Display2:false}).F3", "Collect(t, {Display2:false}).F3", true)]
        [InlineData("Collect(t, {F1:1}).F0;Collect(t, {F2:false}).F0", "Collect(t, {Display1:1}).Display0;Collect(t, {Display2:false}).Display0", true)]
        [InlineData("Collect(t, Blank()).Display2", "Collect(t, Blank()).F2", false)]
        [InlineData("Collect(t, {Display1:1/0}).Display0", "Collect(t, {F1:1/0}).F0", false)]
        [InlineData("Collect(t, {Display0:0}).F3", "Collect(t, {F0:0}).F3", false)]
        [InlineData("Collect(t, {Display2:false}).F3", "Collect(t, {F2:false}).F3", false)]
        [InlineData("Collect(t, {Display1:1}).F0;Collect(t, {Display2:false}).Display0", "Collect(t, {F1:1}).F0;Collect(t, {F2:false}).F0", false)]

        // Patch functions
        [InlineData("Patch(t, First(t), {F1:2})", "Patch(t, First(t), {Display1:2})", true)]
        [InlineData("Patch(t, First(t), Blank()).F2", "Patch(t, First(t), Blank()).Display2", true)]
        [InlineData("Patch(t, First(t), {F0:0}).F3", "Patch(t, First(t), {Display0:0}).F3", true)]
        [InlineData("Patch(t, First(t), {Display2:false}).F3", "Patch(t, First(t), {Display2:false}).F3", true)]
        [InlineData("Patch(t, Lookup(t, F1 = 1), {Display2:false}).F3", "Patch(t, Lookup(t, Display1 = 1), {Display2:false}).F3", true)]
        [InlineData("Patch(t, Lookup(t, F3 = 1), {Display2:false}).F3", "Patch(t, Lookup(t, F3 = 1), {Display2:false}).F3", true)]
        [InlineData("Patch(t, Lookup(t, F1 = 1), {F2:Lookup(t, F1 = 1, F2)})", "Patch(t, Lookup(t, Display1 = 1), {Display2:Lookup(t, Display1 = 1, Display2)})", true)]
        [InlineData("Patch(t, Lookup(t, Display1 = 1), {Display2:Lookup(t, Display1 = 1, Display2)})", "Patch(t, Lookup(t, Display1 = 1), {Display2:Lookup(t, Display1 = 1, Display2)})", true)]
        [InlineData("Patch(t, First(t), {F1:1}).F0;Patch(t, First(t), {F2:false}).F0", "Patch(t, First(t), {Display1:1}).Display0;Patch(t, First(t), {Display2:false}).Display0", true)]
        [InlineData("Patch(t, Last(t), {F0:Lookup(t, F1 = 1, F0)})", "Patch(t, Last(t), {Display0:Lookup(t, Display1 = 1, Display0)})", true)]
        [InlineData("Patch(t, First(t), {Display1:2})", "Patch(t, First(t), {F1:2})", false)]
        [InlineData("Patch(t, First(t), Blank()).Display2", "Patch(t, First(t), Blank()).F2", false)]
        [InlineData("Patch(t, First(t), {Display0:0}).Display3", "Patch(t, First(t), {F0:0}).Display3", false)]
        [InlineData("Patch(t, First(t), {Display2:false}).Display3", "Patch(t, First(t), {F2:false}).Display3", false)]
        [InlineData("Patch(t, First(t), {Display1:1}).F0;Patch(t, First(t), {Display2:false}).F0", "Patch(t, First(t), {F1:1}).F0;Patch(t, First(t), {F2:false}).F0", false)]
        [InlineData("Patch(t, Last(t), {Display0:Lookup(t, Display1 = 1, Display0)})", "Patch(t, Last(t), {F0:Lookup(t, F1 = 1, F0)})", false)]
        [InlineData("Patch(t, Lookup(t, Display1 = 1), {Display2:false}).Display3", "Patch(t, Lookup(t, F1 = 1), {F2:false}).Display3", false)]
        [InlineData("Patch(t, Lookup(t, Display3 = 1), {F2:false}).F3", "Patch(t, Lookup(t, Display3 = 1), {F2:false}).F3", false)]
        [InlineData("Patch(t, Lookup(t, Display1 = 1), {F2:Lookup(t, Display1 = 1, Display2)})", "Patch(t, Lookup(t, F1 = 1), {F2:Lookup(t, F1 = 1, F2)})", false)]
        [InlineData("Patch(t, Lookup(t, Display1 = 1), {Display2:Lookup(t, Display1 = 1, Display2)})", "Patch(t, Lookup(t, F1 = 1), {F2:Lookup(t, F1 = 1, F2)})", false)]

        // Remove functions
        [InlineData("Remove(t, {F1:1})", "Remove(t, {Display1:1})", true)]
        [InlineData("Remove(t, {F1:1}, \"All\")", "Remove(t, {Display1:1}, \"All\")", true)]
        [InlineData("Remove(t, Blank())", "Remove(t, Blank())", true)]
        [InlineData("Remove(t, {F1:1, F0:Blank()})", "Remove(t, {Display1:1, Display0:Blank()})", true)]
        [InlineData("Remove(t, {F1:1, F3:Blank()})", "Remove(t, {Display1:1, F3:Blank()})", true)]
        [InlineData("Remove(t, {F1:1})", "Remove(t, {F1:1})", false)]
        [InlineData("Remove(t, {F1:1}, \"All\")", "Remove(t, {F1:1}, \"All\")", false)]
        [InlineData("Remove(t, Blank())", "Remove(t, Blank())", false)]
        [InlineData("Remove(t, {Display1:1, Display0:Blank()})", "Remove(t, {F1:1, F0:Blank()})", false)]
        [InlineData("Remove(t, {Display1:1, Display3:Blank()})", "Remove(t, {F1:1, Display3:Blank()})", false)]
        public void MutationDisplayNameTest(string inputExpression, string outputExpression, bool toDisplay)
        {
            var engine = new Engine(new PowerFxConfig());

            var rType = RecordType.Empty()
                        .Add(new NamedFormulaType("F1", FormulaType.Decimal, "Display1"))
                        .Add(new NamedFormulaType("F0", FormulaType.String, "Display0"))
                        .Add(new NamedFormulaType("F2", FormulaType.Boolean, "Display2"));

            var r = FormulaValue.NewRecordFromFields(rType, new List<NamedValue>()
            {
                new NamedValue("F1", FormulaValue.New(1m)),
                new NamedValue("F0", FormulaValue.New("string1")),
                new NamedValue("F2", FormulaValue.New(true))
            });

            var t = FormulaValue.NewTable(rType, new List<RecordValue>() { r });

            var parameters = RecordType.Empty()
                .Add("rType", rType);

            engine.Config.SymbolTable.EnableMutationFunctions();
            engine.Config.SymbolTable.AddConstant("t", t);

            if (toDisplay)
            {
                var outDisplayExpression = engine.GetDisplayExpression(inputExpression, rType);
                Assert.Equal(outputExpression, outDisplayExpression);
            }
            else
            {
                var outInvariantExpression = engine.GetInvariantExpression(inputExpression, rType);
                Assert.Equal(outputExpression, outInvariantExpression);
            }
        }

        [Theory]

        // Collect
        // Primitives + primitives
        [InlineData("Collect({0},{0})")]

        // Blank + primitives
        [InlineData("Collect(Blank(),{0})")]
        [InlineData("Collect({0},Blank())")]

        // Error + primitives
        [InlineData("Collect(Error(\"Custom error\"),{0})")]
        [InlineData("Collect({0},Error(\"Custom error\"))")]

        // Table + primitives
        [InlineData("Collect(t,{0})")]
        [InlineData("Collect({0},t)")]

        // Record + primitives
        [InlineData("Collect({1},{0})")]
        [InlineData("Collect({0},{1})")]

        // Patch
        // Primitives + primitives
        [InlineData("Patch({0},{0},{0})")]
        [InlineData("Patch({0},{0},{0},{0})")]

        // Blank + primitives
        [InlineData("Patch(Blank(),{0}, Blank())")]
        [InlineData("Patch({0},Blank(), {0})")]
        [InlineData("Patch({0},Blank(),{0},Blank())")]

        // Error + primitives
        [InlineData("Patch(Error(\"Custom error\"),{0},{0})")]
        [InlineData("Patch({0},Error(\"Custom error\"),Error(\"Custom error\"))")]
        [InlineData("Patch({0},Error(\"Custom error\"),{0},Error(\"Custom error\"))")]

        // Table + primitives
        [InlineData("Patch(t,{0},{0})")]
        [InlineData("Patch({0},t,t)")]
        [InlineData("Patch(t,{0},{0},{0})")]

        // Record + primitives
        [InlineData("Patch({1},{0},{0})")]
        [InlineData("Patch({0},{1},{1})")]
        [InlineData("Patch({1},{0},{1},{0})")]

        // Remove
        // Primitives + primitives
        [InlineData("Remove({0},{0})")]
        [InlineData("Remove({0},{0},\"all\")")]
        [InlineData("Remove({0},{0},{0},{0},\"all\")")]

        // Blank + primitives
        [InlineData("Remove(Blank(),{0})")]
        [InlineData("Remove(Blank(),{0},\"all\")")]
        [InlineData("Remove({0},Blank())")]
        [InlineData("Remove({0},Blank(),\"all\")")]
        [InlineData("Remove({0},Blank(),{0},Blank(),\"all\")")]

        // Error + primitives
        [InlineData("Remove(Error(\"Custom error\"),{0})")]
        [InlineData("Remove(Error(\"Custom error\"),{0}, \"all\")")]
        [InlineData("Remove({0},Error(\"Custom error\"))")]
        [InlineData("Remove({0},Error(\"Custom error\"), \"all\")")]
        [InlineData("Remove(Error(\"Custom error\"),{0},Error(\"Custom error\"),{0},\"all\")")]

        // Table + primitives
        [InlineData("Remove(t,{0})")]
        [InlineData("Remove(t,{0},\"all\")")]
        [InlineData("Remove({0},t)")]
        [InlineData("Remove({0},t,\"all\")")]
        [InlineData("Remove(t,{0},t,{0},\"all\")")]

        // Record + primitives
        [InlineData("Remove({1},{0})")]
        [InlineData("Remove({1},{0},\"all\")")]
        [InlineData("Remove({0},{1})")]
        [InlineData("Remove({0},{1},\"all\")")]
        [InlineData("Remove({1},{0},{1},{0},\"all\")")]
        public void MutationCheckFailTests(string expression)
        {
            var engine = new Engine(new PowerFxConfig());

            var rType = RecordType.Empty()
                        .Add(new NamedFormulaType("F1", FormulaType.Number, "Display1"))
                        .Add(new NamedFormulaType("F0", FormulaType.String, "Display0"))
                        .Add(new NamedFormulaType("F2", FormulaType.Boolean, "Display2"));

            var r = FormulaValue.NewRecordFromFields(rType, new List<NamedValue>()
            {
                new NamedValue("F1", FormulaValue.New(1)),
                new NamedValue("F0", FormulaValue.New("string1")),
                new NamedValue("F2", FormulaValue.New(true))
            });

            var t = FormulaValue.NewTable(rType, new List<RecordValue>() { r });

            var parameters = RecordType.Empty()
                .Add("rType", rType);

            engine.Config.SymbolTable.EnableMutationFunctions();
            engine.Config.SymbolTable.AddConstant("t", t);

            var types = new List<FormulaType>()
            {
                new StringType(),
                new NumberType(),
                new BooleanType(),
                new DateTimeType(),
                new DateType(),
                new GuidType(),
                new TimeType(),
            };

            foreach (var type in types)
            {
                var defaultExpressionValue = type.DefaultExpressionValue();
                var recordDefaultExpressionValue = rType.DefaultExpressionValue();

                var formatedExpression = string.Format(expression, defaultExpressionValue, recordDefaultExpressionValue);

                Check(engine, formatedExpression);
            }
        }

        // Immutable arguments
        [Theory]
        [InlineData("Patch([1,2,3], {Value:1}, {Value:9})")]
        [InlineData("With({x:[1,2,3]},Patch(x, {Value:1}, {Value:9}))")]
        [InlineData("Patch(namedFormula, {Value:1}, {Value:9})")]

        [InlineData("Collect([1,2,3], {Value:9})")]
        [InlineData("With({x:[1,2,3]},Collect(x, {Value:9}))")]
        [InlineData("Collect(namedFormula, {Value:9})")]

        [InlineData("Remove([1, 2, 3], {Value:1})")]
        [InlineData("With({x:[1,2,3]}, Remove(x, {Value:1}))")]
        [InlineData("Remove(namedFormula, {Value:1})")]

        [InlineData("Clear([1, 2, 3])")]
        [InlineData("With({x:[1,2,3]}, Clear(x))")]
        [InlineData("Clear(namedFormula)")]
        public void MutationCheckFailImmutableNodesTests(string expression)
        {
            var engine = new Engine(new PowerFxConfig());
            engine.Config.SymbolTable.AddVariable("namedFormula", new TableType(TestUtils.DT("*[Value:n]")), mutable: false);
            engine.Config.SymbolTable.EnableMutationFunctions();
            var check = engine.Check(expression, options: _opts);
            Assert.False(check.IsSuccess);
        }

        // Taking an immutable tables, storing it into a variable, makes it mutable
        [Theory]
        [InlineData("Set(varTable, [\"a\", \"b\", \"c\"]); Patch(varTable, {Value:\"a\"}, {Value:\"z\"})")]
        [InlineData("Set(varTable, [\"a\", \"b\", \"c\"]); Collect(varTable, {Value:\"z\"})")]
        [InlineData("Set(varRecord, {x:[\"a\", \"b\", \"c\"]}); Patch(varRecord.x, {Value:\"a\"}, {Value:\"z\"})")]
        [InlineData("Set(varRecord, {x:[\"a\", \"b\", \"c\"]}); Collect(varRecord.x, {Value:\"z\"})")]
        public void MutationCheckForWellDefinedVariables(string expression)
        {
            var engine = new Engine(new PowerFxConfig());
            engine.Config.SymbolTable.AddVariable("varTable", new TableType(TestUtils.DT("*[Value:s]")), mutable: true);
            engine.Config.SymbolTable.AddVariable("varRecord", new KnownRecordType(TestUtils.DT("![x:*[Value:s]]")), mutable: true);
            engine.Config.SymbolTable.EnableMutationFunctions();
            var check = engine.Check(expression, options: _opts);
            Assert.True(check.IsSuccess);
        }

        protected void Check(Engine engine, string expression)
        {
            var functionName = expression.Split('(')[0];
            var errorMessage = $"The function '{functionName}' has some invalid arguments";

            var check = engine.Check(expression, options: _opts);

            Assert.False(check.IsSuccess);
            Assert.NotEmpty(check.Errors);
            Assert.True(check.Errors.Where(er => er.Message.Contains(errorMessage)).Any());
        }

        // Regression test case
        // https://github.com/microsoft/Power-Fx/issues/1335
        [Theory]
        [InlineData("Collect(checktable, {flavor: \"Strawberry\", quantity: 300 })")]
        public void MutationNumberAsFloatTests(string expr)
        {
            var engine = new RecalcEngine(new PowerFxConfig(Features.PowerFxV1));
            var fv = FormulaValueJSON.FromJson("100", numberIsFloat: true);

            var rType = RecordType.Empty()
                .Add(new NamedFormulaType("flavor", FormulaType.String))
                .Add(new NamedFormulaType("quantity", fv.Type));

            engine.Config.SymbolTable.EnableMutationFunctions();
            engine.UpdateVariable("checktable", FormulaValue.NewTable(rType));

            var check = engine.Check(expr, options: new ParserOptions() { NumberIsFloat = true, AllowsSideEffects = true });

            var message = string.Empty;
            if (check.Errors.Any())
            {
                message = check.Errors?.First().Message;
            }

            Assert.True(check.IsSuccess, message);
        }

        [Theory]
        [InlineData("Collect(t1, {subject: \"something\", poly: {} })", true, true)]
        [InlineData("Collect(t1, {subject: \"something\", poly: [] })", false, true)]

        [InlineData("Collect(t1, {subject: \"something\", poly: {} })", false, false)]
        [InlineData("Collect(t1, {subject: \"something\", poly: [] })", false, false)]
        public void PolymorphicFieldUnions(string expr, bool isSuccess, bool isPowerFxV1)
        {
            var features = isPowerFxV1 ? Features.PowerFxV1 : Features.None;
            var engine = new RecalcEngine(new PowerFxConfig(features));

            var rType = RecordType.Empty()
                .Add(new NamedFormulaType("subject", FormulaType.String))
                .Add(new NamedFormulaType("poly", FormulaType.Build(DType.Polymorphic)));

            engine.Config.SymbolTable.EnableMutationFunctions();
            engine.UpdateVariable("t1", FormulaValue.NewTable(rType));

            var check = engine.Check(expr, options: new ParserOptions() { NumberIsFloat = true, AllowsSideEffects = true });

            Assert.Equal(isSuccess, check.IsSuccess);
        }

        [Theory]
        [InlineData("Collect(t, x)")]
        [InlineData("Collect(t, x);First(t)")]
        [InlineData("Collect(t, x);Patch(t, First(t), x);First(t)")]
        public void DontCopyDerivedRecordValuesTest(string expression)
        {
            var engine = new RecalcEngine();
            var rType = RecordType.Empty()
                .Add(new NamedFormulaType("field", FormulaType.String));

            TableValue t = FormulaValue.NewTable(rType);
            RecordValue x = new FileObjectRecordValue("x", IRContext.NotInSource(rType), new List<NamedValue>());

            engine.Config.SymbolTable.EnableMutationFunctions();
            engine.UpdateVariable("x", x);
            engine.UpdateVariable("t", t);

            var check = engine.Check(expression, options: new ParserOptions() { AllowsSideEffects = true });

            Assert.True(check.IsSuccess);

            var result = check.GetEvaluator().Eval();

            Assert.IsType<FileObjectRecordValue>(result);
            Assert.False(ReferenceEquals(x, result));

            var fileObjectRecordValue = (FileObjectRecordValue)result;

            // Derived object did not lose it's properties
            Assert.Equal("x", fileObjectRecordValue.SomeProperty);
        }

        [Fact]
        public void SymbolTableEnableMutationFuntionsTest()
        {
            var expr = "Collect()";
            var engine = new RecalcEngine();

            var symbolTable = new SymbolTable();
            var symbolTableEnabled = new SymbolTable();

            symbolTableEnabled.EnableMutationFunctions();

            // Mutation functions not listed.
            var check = engine.Check(expr, symbolTable: symbolTable);
            Assert.DoesNotContain(check.Symbols.Functions.FunctionNames, f => f == "Collect");

            // Mutation functions is listed.
            var checkEnabled = engine.Check(expr, symbolTable: symbolTableEnabled);
            Assert.Contains(checkEnabled.Symbols.Functions.FunctionNames, f => f == "Collect");
        }

        [Theory]
        [InlineData("Patch(t, First(t), {Value:1})")]
        public async Task MutationPFxV1Disabled(string expression)
        {
            var engine = new RecalcEngine(new PowerFxConfig(Features.None));
            var t = FormulaValue.NewTable(RecordType.Empty().Add(new NamedFormulaType("Value", FormulaType.Decimal)));

            engine.Config.SymbolTable.EnableMutationFunctions();
            engine.UpdateVariable("t", t);

            var check = engine.Check(expression, options: new ParserOptions() { AllowsSideEffects = true });

            // Compilation will be successful, but the function will not be executed.
            // This is because PA depends on the CheckType to determine if the function is valid.
            Assert.True(check.IsSuccess);

            var evaluator = check.GetEvaluator();

            // no runtime exception
            _ = await evaluator.EvalAsync(CancellationToken.None);
        }

        [Theory]        
        [InlineData(
            "Patch(t1, {accountid:GUID(\"00000000-0000-0000-0000-000000000001\"),name:\"Mary Doe\"});Concat(t1, $\"{name} from {address1_city}\", \",\")",
            "Mary Doe from Chicago,Sam Doe from New York",
            2)]
        [InlineData(
            "Patch(t1, {accountid:GUID(\"00000000-0000-0000-0000-000000000001\"),address1_city:\"Seattle\"});Concat(t1, $\"{name} from {address1_city}\", \",\")",
            "John Doe from Seattle,Sam Doe from New York",
            2)]
        [InlineData(
            "Patch(t1, {accountid:GUID(\"00000000-0000-0000-0000-000000000003\"),name:\"Microsoft Corporation\",address1_city:\"Seattle\"});Concat(t1, $\"{name} from {address1_city}\", \",\")",
            "John Doe from Chicago,Sam Doe from New York,Microsoft Corporation from Seattle",
            3)]
        [InlineData(
            "Patch(t1, Table({accountid:GUID(\"00000000-0000-0000-0000-000000000001\"),name:\"Emily Doe\"},{accountid:GUID(\"00000000-0000-0000-0000-000000000002\"),name:\"Benjamin Doe\"}));Concat(t1, $\"{name} from {address1_city}\", \",\")",
            "Emily Doe from Chicago,Benjamin Doe from New York",
            2)]
        [InlineData(
            "Patch(t1, Table({accountid:GUID(\"00000000-0000-0000-0000-000000000001\"),address1_city:\"Miami\"},{accountid:GUID(\"00000000-0000-0000-0000-000000000002\"),address1_city:\"Orlando\"}));Concat(t1, $\"{name} from {address1_city}\", \",\")",
            "John Doe from Miami,Sam Doe from Orlando",
            2)]
        [InlineData(
            "Patch(t1, Table({accountid:GUID(\"00000000-0000-0000-0000-000000000003\"),name:\"Microsoft Corporation\",address1_city:\"Seattle\"},{accountid:GUID(\"00000000-0000-0000-0000-000000000004\"),name:\"Bill Gates\",address1_city:\"Seattle\"}));Concat(t1, $\"{name} from {address1_city}\", \",\")",
            "John Doe from Chicago,Sam Doe from New York,Microsoft Corporation from Seattle,Bill Gates from Seattle",
            4)]
        [InlineData(
            "Patch(t1, Table({accountid:GUID(\"00000000-0000-0000-0000-000000000001\")},{accountid:GUID(\"00000000-0000-0000-0000-000000000002\")}), Table({name:\"Emily Doe\"},{name:\"Mary Doe\"}));Concat(t1, $\"{name} from {address1_city}\", \",\")",
            "Emily Doe from Chicago,Mary Doe from New York",
            2)]
        [InlineData(
            "Patch(t1, Table({accountid:GUID(\"00000000-0000-0000-0000-000000000003\")},{accountid:GUID(\"00000000-0000-0000-0000-000000000004\")}), Table({name:\"Emily Doe\",address1_city:\"Seattle\"},{name:\"Mary Doe\",address1_city:\"Seattle\"}));Concat(t1, $\"{name} from {address1_city}\", \",\")",
            "John Doe from Chicago,Sam Doe from New York,Emily Doe from Seattle,Mary Doe from Seattle",
            4)]
        public void MutationEntityTests(string expression, string expected, int count)
        {
            var engine = PatchEngine;
            var check = engine.Check(expression, options: new ParserOptions() { AllowsSideEffects = true });

            Assert.True(check.IsSuccess);

            var result = check.GetEvaluator().Eval();
            Assert.IsNotType<ErrorValue>(result);
            Assert.Equal(expected, ((StringValue)result).Value);

            var varTableValue = engine.GetValue("t1") as EntityTableValue;
            Assert.Equal(count, varTableValue.Rows.Count());
        }

        [Theory]
        [InlineData("Patch(t1, First(t1), {accountid:GUID(\"00000000-0000-0000-0000-000000000001\")})")]
        [InlineData("Patch(t1, Table(First(t1)), Table({accountid:GUID(\"00000000-0000-0000-0000-000000000001\")}))")]
        public void MutationCheckSemanticsTests(string expression)
        {
            var engine = PatchEngine;

            var check = engine.Check(expression, options: new ParserOptions() { AllowsSideEffects = true });

            Assert.False(check.IsSuccess);
            Assert.Contains(check.Errors, e => e.MessageKey == "ErrRecordContainsInvalidFields_Arg");

            // Rows count hasn't changed.
            var varTableValue = engine.GetValue("t1") as EntityTableValue;
            Assert.Equal(2, varTableValue.Rows.Count());
        }

        /// <summary>
        /// Intellisense should suggest different symbols depending on AllowSideEffect.
        /// </summary>
        [Theory]        
        [InlineData("Patch(", false, "r1")]
        [InlineData("Patch(", true, "MyDataSource")]
        [InlineData("Patch(MyDataSource,", true, "r1")]
        [InlineData("Patch(MyDataSource,First(MyDataSource),", true, "r1")]
        public void MutationSuggestionTests(string expression, bool allowSideEffects, params string[] expectedSuggestions)
        {
            var config = new PowerFxConfig();
            var varTableValue = new TestDataSource("MyDataSource", TestUtils.DT("*[Id:n, Name:s, Age:n]"));

            config.SymbolTable.AddEntity(varTableValue);
            config.SymbolTable.AddVariable("r1", FormulaType.Build(varTableValue.Type.ToRecord()));
            config.SymbolTable.EnableMutationFunctions();

            var engine = new RecalcEngine(config);
            var check = engine.Check(expression, options: new ParserOptions() { AllowsSideEffects = allowSideEffects });
            var suggestions = engine.Suggest(check, expression.Length);

            Assert.Equal(expectedSuggestions.Length, suggestions.Suggestions.Count());
            Assert.Equal(string.Join("-", expectedSuggestions), string.Join("-", suggestions.Suggestions.Select(s => s.DisplayText.Text)));
        }

        [Theory]

        // Filter functions block side effects in predicate                                             delegable DS, enhanced    ,   non-del DS, enhanced    ,  variable DS, enhanced

        [InlineData("Filter(DS, Set(TblVar, [{Id:1,Name:\"a\"}]); 1=1)                               ", "FilterNoSE", "FilterNoSE", "FilterNoSE", "FilterNoSE", "FilterNoSE", "FilterNoSE")]
        [InlineData("Filter(DS, Set(First(TblVar).Id, 3); 1=1)                                       ", "FilterNoSE", "FilterNoSE", "FilterNoSE", "FilterNoSE", "FilterNoSE", "FilterNoSE")]
        [InlineData("LookUp(DS, Set(TblVar, [{Id:1,Name:\"a\"}]); 1=1)                               ", "FilterNoSE", "FilterNoSE", "FilterNoSE", "FilterNoSE", "FilterNoSE", "FilterNoSE")]
        [InlineData("LookUp(DS, Set(First(TblVar).Id, 3); 1=1)                                       ", "FilterNoSE", "FilterNoSE", "FilterNoSE", "FilterNoSE", "FilterNoSE", "FilterNoSE")]
        [InlineData("CountIf(DS, Set(TblVar, [{Id:1,Name:\"a\"}]); Id>2)                             ", "FilterNoSE", "FilterNoSE", "FilterNoSE", "FilterNoSE", "FilterNoSE", "FilterNoSE")]
        [InlineData("CountIf(DS, Set(First(TblVar).Id, 3); Id>2)                                     ", "FilterNoSE", "FilterNoSE", "FilterNoSE", "FilterNoSE", "FilterNoSE", "FilterNoSE")]
        [InlineData("Filter(DS, Remove(TblVar, {}); 1=1)                                             ", "FilterNoSE", "FilterNoSE", "FilterNoSE", "FilterNoSE", "FilterNoSE", "FilterNoSE")]
        [InlineData("LookUp(DS, Remove(TblVar, {}); 1=1)                                             ", "FilterNoSE", "FilterNoSE", "FilterNoSE", "FilterNoSE", "FilterNoSE", "FilterNoSE")]
        [InlineData("CountIf(DS, Remove(TblVar, {}); Id>2)                                           ", "FilterNoSE", "FilterNoSE", "FilterNoSE", "FilterNoSE", "FilterNoSE", "FilterNoSE")]

        // Search and With can have a formula for the second argument,
        // but it is not an iterated lambda and OK                                                      delegable DS, enhanced    ,   non-del DS, enhanced    ,  variable DS, enhanced

        [InlineData("Search(DS, Set(TblVar, [{Id:1,Name:\"a\"}]); \"b\", Name)                       ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("With(LookUp(DS,1=1), Set(TblVar, [{Id:1,Name:\"a\"}]))                          ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Search(DS, Remove(TblVar, {}); \"b\", Name)                                     ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Search(Filter(DS,1=1), Set(DS, [{Id:1,Name:\"a\"}]); \"b\", Name)               ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Search(DS, Set(DS, [{Id:1,Name:\"a\"}]); \"b\", Name)                           ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Search(Sort(DS,Id), Set(DS, [{Id:1,Name:\"a\"}]); \"b\", Name)                  ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Search(Sort(DS,Id), Remove(DS, {}); \"b\", Name)                                ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Search(Sort(Filter(DS,1=1),Id), Remove(DS, {}); \"b\", Name)                    ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Search(SortByColumns(DS,\"Id\"), Remove(DS, {}); \"b\", Name)                   ", "NonDelWarn", "NonDelWarn", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Search(FirstN(DS,10), Remove(DS, {}); \"b\", Name)                              ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Search(SortByColumns(DS,\"Id\"), Set(DS, [{Id:1,Name:\"a\"}]); \"b\", Name)     ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Search(Sort(Filter(DS,1=1),Id), Set(DS, [{Id:1,Name:\"a\"}]); \"b\", Name)      ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Search(FirstN(DS,10), Set(DS, [{Id:1,Name:\"a\"}]); \"b\", Name)                ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("With(LookUp(DS,1=1), Set(DS, [{Id:1,Name:\"a\"}]))                              ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Search(DS, Remove(DS, {}); \"b\", Name)                                         ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Search(Filter(DS,1=1), Remove(DS, {}); \"b\", Name)                             ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]

        // DropColumns, and friends use record scpoe for the column names, 
        // but don't have lambdas or even a formula, so can't have side effects                         delegable DS, enhanced    ,   non-del DS, enhanced    ,  variable DS, enhanced

        [InlineData("Search(DS, \"a\", Set(TblVar, [{Id:1,Name:\"a\"}]); Name )                      ", " IdentName", " IdentName", " IdentName", " IdentName", " IdentName", " IdentName")]
        [InlineData("Search(DS, \"a\", Set(DS, [{Id:1,Name:\"a\"}]); Name )                          ", " IdentName", " IdentName", " IdentName", " IdentName", " IdentName", " IdentName")]
        [InlineData("Search(DS, \"a\", Remove(DS, {}); Name )                                        ", " IdentName", " IdentName", " IdentName", " IdentName", " IdentName", " IdentName")]
        [InlineData("DropColumns(DS, Set(DS, [{Id:1,Name:\"a\"}]); Name )                            ", " IdentName", " IdentName", " IdentName", " IdentName", " IdentName", " IdentName")]
        [InlineData("DropColumns(DS, Remove(DS, {}); Name )                                          ", " IdentName", " IdentName", " IdentName", " IdentName", " IdentName", " IdentName")]
        [InlineData("ShowColumns(DS, Set(TblVar, [{Id:1,Name:\"a\"}]); Name )                        ", " IdentName", " IdentName", " IdentName", " IdentName", " IdentName", " IdentName")]
        [InlineData("RenameColumns(DS, Name, Set(TblVar, [{Id:1,Name:\"a\"}]); NewName )             ", " IdentName", " IdentName", " IdentName", " IdentName", " IdentName", " IdentName")]
        [InlineData("RenameColumns(DS, Set(TblVar, [{Id:1,Name:\"a\"}]); Name, NewName )             ", " IdentName", " IdentName", " IdentName", " IdentName", " IdentName", " IdentName")]
        [InlineData("RenameColumns(DS, Name, Remove(DS, {}); NewName )                               ", " IdentName", " IdentName", " IdentName", " IdentName", " IdentName", " IdentName")]
        [InlineData("RenameColumns(DS, Remove(DS, {}); Name, NewName )                               ", " IdentName", " IdentName", " IdentName", " IdentName", " IdentName", " IdentName")]

        // Untyped objects can be iterated only by ForAll                                                 dynamic DS, enhanced

        [InlineData("ForAll(DynVar, Set(DynVar, TblDynVar))                                          ", "        Ok", "        Ok")]
        [InlineData("ForAll(DynVar, Set(First(DynVar), TblDynVar))                                   ", " Immutable", " Immutable")]
        [InlineData("ForAll(DynVar, Clear(TblVar))                                                   ", " Unordered", " Unordered")]
        [InlineData("ForAll(DynVar, Collect(DynVar, {Id:1}))                                         ", "InvalidArg", "InvalidArg")]
        [InlineData("ForAll(DynVar, Clear(DynVar))                                                   ", "BadTypeExp", "BadTypeExp")]

        [InlineData("Filter(DynVar, Set(DynVar, TblDynVar); 1=1)                                     ", "   BadType", "   BadType")]
        [InlineData("LookUp(DynVar, Set(DynVar, TblDynVar); 1=1)                                     ", "   BadType", "   BadType")]
        [InlineData("CountIf(DynVar, Set(DynVar, TblDynVar); 1=1)                                    ", "   BadType", "   BadType")]
        [InlineData("AddColumns(DynVar, Num, Set(DynVar, TblDynVar); 2)                              ", "   BadType", "   BadType")]
        [InlineData("Concat(DynVar, Set(DynVar, TblDynVar); Text(Id))                                ", "   BadType", "   BadType")]
        [InlineData("Distinct(DynVar, Set(DynVar, TblDynVar); Id)                                    ", "   BadType", "   BadType")]
        [InlineData("Sum(DynVar, Set(DynVar, TblDynVar); Id)                                         ", " NoUntyped", " NoUntyped")]
        [InlineData("Average(DynVar, Set(DynVar, TblDynVar); Id)                                     ", " NoUntyped", " NoUntyped")]
        [InlineData("Min(DynVar, Set(DynVar, TblDynVar); Id)                                         ", " NoUntyped", " NoUntyped")]
        [InlineData("Max(DynVar, Set(DynVar, TblDynVar); Id)                                         ", " NoUntyped", " NoUntyped")]
        [InlineData("VarP(DynVar, Set(DynVar, TblDynVar); Id)                                        ", " NoUntyped", " NoUntyped")]
        [InlineData("StdevP(DynVar, Set(DynVar, TblDynVar); Id)                                      ", " NoUntyped", " NoUntyped")]
        [InlineData("Search(DynVar, Set(DynVar, TblDynVar); \"b\", Name)                             ", "   BadType", "   BadType")]
        [InlineData("With(First(DynVar), Set(DynVar, TblDynVar))                                     ", "   BadType", "   BadType")]

        // Deep mutations are not supported on the iterated record                                      delegable DS, enhanced    ,   non-del DS, enhanced    ,  variable DS, enhanced

        [InlineData("ForAll(DS, Set(Id, 3))                                                          ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "   CantMod")]
        [InlineData("ForAll(DS, Set(ThisRecord.Id, 3))                                               ", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable")]
        [InlineData("ForAll(DS As A, Set(A.Id, 3))                                                   ", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable")]
        [InlineData("AddColumns(DS, Num, Set(Id, 3); 2)                                              ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "   CantMod")]
        [InlineData("AddColumns(DS, Num, Set(ThisRecord.Id, 3); 2)                                   ", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable")]
        [InlineData("AddColumns(DS As A, Num, Set(A.Id, 3); 2)                                       ", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable")]
        [InlineData("Concat(DS, Set(Id, 3); Text(Id))                                                ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "   CantMod")]
        [InlineData("Concat(DS, Set(ThisRecord.Id, 3); Text(Id))                                     ", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable")]
        [InlineData("Concat(DS As A, Set(A.Id, 3); Text(Id))                                         ", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable")]
        [InlineData("Distinct(DS, Set(Id,3); Id)                                                     ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "   CantMod")]
        [InlineData("Distinct(DS, Set(ThisRecord.Id,3); Id)                                          ", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable")]
        [InlineData("Distinct(DS As A, Set(A.Id,3); Id)                                              ", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable")]
        [InlineData("Sum(DS, Set(Id, 3); Id)                                                         ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "   CantMod")]
        [InlineData("Sum(DS, Set(ThisRecord.Id, 3); Id)                                              ", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable")]
        [InlineData("Sum(DS As A, Set(A.Id, 3); Id)                                                  ", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable")]
        [InlineData("Average(DS, Set(Id, 3); Id)                                                     ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "   CantMod")]
        [InlineData("Average(DS, Set(ThisRecord.Id, 3); Id)                                          ", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable")]
        [InlineData("Average(DS As A, Set(A.Id, 3); Id)                                              ", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable")]
        [InlineData("Min(DS, Set(Id, 3); Id)                                                         ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "   CantMod")]
        [InlineData("Min(DS, Set(ThisRecord.Id, 3); Id)                                              ", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable")]
        [InlineData("Min(DS As A, Set(A.Id, 3); Id)                                                  ", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable")]
        [InlineData("Max(DS, Set(Id, 3); Id)                                                         ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "   CantMod")]
        [InlineData("Max(DS, Set(ThisRecord.Id, 3); Id)                                              ", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable")]
        [InlineData("Max(DS As A, Set(A.Id, 3); Id)                                                  ", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable")]
        [InlineData("VarP(DS, Set(Id, 3); Id)                                                        ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "   CantMod")]
        [InlineData("VarP(DS, Set(ThisRecord.Id, 3); Id)                                             ", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable")]
        [InlineData("VarP(DS As A, Set(A.Id, 3); Id)                                                 ", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable")]
        [InlineData("StdevP(DS, Set(Id, 3); Id)                                                      ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "   CantMod")]
        [InlineData("StdevP(DS, Set(ThisRecord.Id, 3); Id)                                           ", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable")]
        [InlineData("StdevP(DS As A, Set(A.Id, 3); Id)                                               ", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable", " Immutable")]

        // Set non self modifying                                                                       delegable DS, enhanced    ,   non-del DS, enhanced    ,  variable DS, enhanced

        [InlineData("ForAll(DS, Set(TblVar, [{Id:1,Name:\"a\"}]))                                    ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("ForAll(DS, Set(First(TblVar).Id, 3))                                            ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("AddColumns(DS, Num, Set(TblVar, [{Id:1,Name:\"a\"}]); 2)                        ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("AddColumns(DS, Num, Set(First(TblVar).Id, 3); 2)                                ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Concat(DS, Set(TblVar, [{Id:1,Name:\"a\"}]); Text(Id))                          ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Distinct(DS, Set(TblVar, [{Id:1,Name:\"a\"}]); Id)                              ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Sum(DS, Set(TblVar, [{Id:1,Name:\"a\"}]); Id)                                   ", "NonDelWarn", "NonDelWarn", "NonDelWnOp", "NonDelWnOp", "        Ok", "        Ok")]
        [InlineData("Average(DS, Set(TblVar, [{Id:1,Name:\"a\"}]); Id)                               ", "NonDelWarn", "NonDelWarn", "NonDelWnOp", "NonDelWnOp", "        Ok", "        Ok")]
        [InlineData("Min(DS, Set(TblVar, [{Id:1,Name:\"a\"}]); Id)                                   ", "NonDelWarn", "NonDelWarn", "NonDelWnOp", "NonDelWnOp", "        Ok", "        Ok")]
        [InlineData("Max(DS, Set(TblVar, [{Id:1,Name:\"a\"}]); Id)                                   ", "NonDelWarn", "NonDelWarn", "NonDelWnOp", "NonDelWnOp", "        Ok", "        Ok")]
        [InlineData("VarP(DS, Set(TblVar, [{Id:1,Name:\"a\"}]); Id)                                  ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("StdevP(DS, Set(TblVar, [{Id:1,Name:\"a\"}]); Id)                                ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Concat(DS, Set(First(TblVar).Id, 3); Text(Id))                                  ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Distinct(DS, Set(First(TblVar).Id, 3); Id)                                      ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Sum(DS, Set(First(TblVar).Id, 3); Id)                                           ", "NonDelWarn", "NonDelWarn", "NonDelWnOp", "NonDelWnOp", "        Ok", "        Ok")]
        [InlineData("Average(DS, Set(First(TblVar).Id, 3); Id)                                       ", "NonDelWarn", "NonDelWarn", "NonDelWnOp", "NonDelWnOp", "        Ok", "        Ok")]
        [InlineData("Min(DS, Set(First(TblVar).Id, 3); Id)                                           ", "NonDelWarn", "NonDelWarn", "NonDelWnOp", "NonDelWnOp", "        Ok", "        Ok")]
        [InlineData("Max(DS, Set(First(TblVar).Id, 3); Id)                                           ", "NonDelWarn", "NonDelWarn", "NonDelWnOp", "NonDelWnOp", "        Ok", "        Ok")]
        [InlineData("VarP(DS, Set(First(TblVar).Id, 3); Id)                                          ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("StdevP(DS, Set(First(TblVar).Id, 3); Id)                                        ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]

        // Set self modifying, direct DS                                                                delegable DS, enhanced    ,   non-del DS, enhanced    ,  variable DS, enhanced

        [InlineData("ForAll(DS, Set(DS, [{Id:1,Name:\"a\"}]))                                        ", "   SelfMod", "   SelfMod", "   SelfMod", "   SelfMod", "        Ok", "   SelfMod")]
        [InlineData("ForAll(DS, Set(First(DS).Id, 3))                                                ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("ForAll(DS, If(ThisRecord.Id>1, Set(DS, [{Id:1,Name:\"a\"}])))                   ", "   SelfMod", "   SelfMod", "   SelfMod", "   SelfMod", "        Ok", "   SelfMod")]
        [InlineData("ForAll(DS, If(Id>1, Set(DS, [{Id:1,Name:\"a\"}])))                              ", "   SelfMod", "   SelfMod", "   SelfMod", "   SelfMod", "        Ok", "   SelfMod")]
        [InlineData("AddColumns(DS, Num, Set(DS, [{Id:1,Name:\"a\"}]); 2)                            ", "   SelfMod", "   SelfMod", "   SelfMod", "   SelfMod", "        Ok", "   SelfMod")]
        [InlineData("AddColumns(DS, Num, Set(First(DS).Id, 3); 2)                                    ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("AddColumns(DS, Num, If(ThisRecord.Id>1,Set(DS, [{Id:1,Name:\"a\"}]); 2))        ", "   SelfMod", "   SelfMod", "   SelfMod", "   SelfMod", "        Ok", "   SelfMod")]
        [InlineData("AddColumns(DS, Num, If(Id>1,Set(DS, [{Id:1,Name:\"a\"}]); 2))                   ", "   SelfMod", "   SelfMod", "   SelfMod", "   SelfMod", "        Ok", "   SelfMod")]
        [InlineData("Concat(DS, Set(DS, [{Id:1,Name:\"a\"}]); Text(Id))                              ", "   SelfMod", "   SelfMod", "   SelfMod", "   SelfMod", "        Ok", "   SelfMod")]
        [InlineData("Distinct(DS, Set(DS, [{Id:1,Name:\"a\"}]); Id)                                  ", "   SelfMod", "   SelfMod", "   SelfMod", "   SelfMod", "        Ok", "   SelfMod")]
        [InlineData("Sum(DS, Set(DS, [{Id:1,Name:\"a\"}]); Id)                                       ", "   SelfMod", "   SelfMod", "   SelfMod", "   SelfMod", "        Ok", "   SelfMod")]
        [InlineData("Average(DS, Set(DS, [{Id:1,Name:\"a\"}]); Id)                                   ", "   SelfMod", "   SelfMod", "   SelfMod", "   SelfMod", "        Ok", "   SelfMod")]
        [InlineData("Min(DS, Set(DS, [{Id:1,Name:\"a\"}]); Id)                                       ", "   SelfMod", "   SelfMod", "   SelfMod", "   SelfMod", "        Ok", "   SelfMod")]
        [InlineData("Max(DS, Set(DS, [{Id:1,Name:\"a\"}]); Id)                                       ", "   SelfMod", "   SelfMod", "   SelfMod", "   SelfMod", "        Ok", "   SelfMod")]
        [InlineData("VarP(DS, Set(DS, [{Id:1,Name:\"a\"}]); Id)                                      ", "   SelfMod", "   SelfMod", "   SelfMod", "   SelfMod", "        Ok", "   SelfMod")]
        [InlineData("StdevP(DS, Set(DS, [{Id:1,Name:\"a\"}]); Id)                                    ", "   SelfMod", "   SelfMod", "   SelfMod", "   SelfMod", "        Ok", "   SelfMod")]
        [InlineData("Concat(DS, Set(First(DS).Id, 3); Text(Id))                                      ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Distinct(DS, Set(First(DS).Id, 3); Id)                                          ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Sum(DS, Set(First(DS).Id, 3); Id)                                               ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Average(DS, Set(First(DS).Id, 3); Id)                                           ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Min(DS, Set(First(DS).Id, 3); Id)                                               ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Max(DS, Set(First(DS).Id, 3); Id)                                               ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("VarP(DS, Set(First(DS).Id, 3); Id)                                              ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("StdevP(DS, Set(First(DS).Id, 3); Id)                                            ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]

        // Set self modifying, Filter of DS                                                             delegable DS, enhanced    ,   non-del DS, enhanced    ,  variable DS, enhanced

        [InlineData("ForAll(Filter(DS,1=1), Set(DS, [{Id:1,Name:\"a\"}]))                            ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("ForAll(Filter(DS,1=1), Set(First(DS).Id, 3))                                    ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("AddColumns(Filter(DS,1=1), Num, Set(DS, [{Id:1,Name:\"a\"}]); 2)                ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("AddColumns(Filter(DS,1=1), Num, Set(First(DS).Id, 3); 2)                        ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Concat(Filter(DS,1=1), Set(DS, [{Id:1,Name:\"a\"}]); Text(Id))                  ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Distinct(Filter(DS,1=1), Set(DS, [{Id:1,Name:\"a\"}]); Id)                      ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Sum(Filter(DS,1=1), Set(DS, [{Id:1,Name:\"a\"}]); Id)                           ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Average(Filter(DS,1=1), Set(DS, [{Id:1,Name:\"a\"}]); Id)                       ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Min(Filter(DS,1=1), Set(DS, [{Id:1,Name:\"a\"}]); Id)                           ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Max(Filter(DS,1=1), Set(DS, [{Id:1,Name:\"a\"}]); Id)                           ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("VarP(Filter(DS,1=1), Set(DS, [{Id:1,Name:\"a\"}]); Id)                          ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("StdevP(Filter(DS,1=1), Set(DS, [{Id:1,Name:\"a\"}]); Id)                        ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Concat(Filter(DS,1=1), Set(First(DS).Id, 3); Text(Id))                          ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Distinct(Filter(DS,1=1), Set(First(DS).Id, 3); Id)                              ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Sum(Filter(DS,1=1), Set(First(DS).Id, 3); Id)                                   ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Average(Filter(DS,1=1), Set(First(DS).Id, 3); Id)                               ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Min(Filter(DS,1=1), Set(First(DS).Id, 3); Id)                                   ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Max(Filter(DS,1=1), Set(First(DS).Id, 3); Id)                                   ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("VarP(Filter(DS,1=1), Set(First(DS).Id, 3); Id)                                  ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("StdevP(Filter(DS,1=1), Set(First(DS).Id, 3); Id)                                ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]

        // Set self modifying, Sort DS                                                                  delegable DS, enhanced    ,   non-del DS, enhanced    ,  variable DS, enhanced

        [InlineData("ForAll(Sort(DS,Id), Set(DS, [{Id:1,Name:\"a\"}]))                               ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("ForAll(Sort(DS,Id), Set(First(DS).Id, 3))                                       ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("AddColumns(Sort(DS,Id), Num, Set(DS, [{Id:1,Name:\"a\"}]); 2)                   ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("AddColumns(Sort(DS,Id), Num, Set(First(DS).Id, 3); 2)                           ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Concat(Sort(DS,Id), Set(DS, [{Id:1,Name:\"a\"}]); Text(Id))                     ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Distinct(Sort(DS,Id), Set(DS, [{Id:1,Name:\"a\"}]); Id)                         ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Sum(Sort(DS,Id), Set(DS, [{Id:1,Name:\"a\"}]); Id)                              ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Average(Sort(DS,Id), Set(DS, [{Id:1,Name:\"a\"}]); Id)                          ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Min(Sort(DS,Id), Set(DS, [{Id:1,Name:\"a\"}]); Id)                              ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Max(Sort(DS,Id), Set(DS, [{Id:1,Name:\"a\"}]); Id)                              ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("VarP(Sort(DS,Id), Set(DS, [{Id:1,Name:\"a\"}]); Id)                             ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("StdevP(Sort(DS,Id), Set(DS, [{Id:1,Name:\"a\"}]); Id)                           ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Concat(Sort(DS,Id), Set(First(DS).Id, 3); Text(Id))                             ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Distinct(Sort(DS,Id), Set(First(DS).Id, 3); Id)                                 ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Sum(Sort(DS,Id), Set(First(DS).Id, 3); Id)                                      ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Average(Sort(DS,Id), Set(First(DS).Id, 3); Id)                                  ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Min(Sort(DS,Id), Set(First(DS).Id, 3); Id)                                      ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Max(Sort(DS,Id), Set(First(DS).Id, 3); Id)                                      ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("VarP(Sort(DS,Id), Set(First(DS).Id, 3); Id)                                     ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("StdevP(Sort(DS,Id), Set(First(DS).Id, 3); Id)                                   ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]

        // Set self modifying, Sort composed with Filter DS                                             delegable DS, enhanced    ,   non-del DS, enhanced    ,  variable DS, enhanced

        [InlineData("ForAll(Sort(Filter(DS,1=1),Id), Set(DS, [{Id:1,Name:\"a\"}]))                   ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("ForAll(Sort(Filter(DS,1=1),Id), Set(First(DS).Id, 3))                           ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("AddColumns(Sort(Filter(DS,1=1),Id), Num, Set(DS, [{Id:1,Name:\"a\"}]); 2)       ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("AddColumns(Sort(Filter(DS,1=1),Id), Num, Set(First(DS).Id, 3); 2)               ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Concat(Sort(Filter(DS,1=1),Id), Set(DS, [{Id:1,Name:\"a\"}]); Text(Id))         ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Distinct(Sort(Filter(DS,1=1),Id), Set(DS, [{Id:1,Name:\"a\"}]); Id)             ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Sum(Sort(Filter(DS,1=1),Id), Set(DS, [{Id:1,Name:\"a\"}]); Id)                  ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Average(Sort(Filter(DS,1=1),Id), Set(DS, [{Id:1,Name:\"a\"}]); Id)              ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Min(Sort(Filter(DS,1=1),Id), Set(DS, [{Id:1,Name:\"a\"}]); Id)                  ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Max(Sort(Filter(DS,1=1),Id), Set(DS, [{Id:1,Name:\"a\"}]); Id)                  ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("VarP(Sort(Filter(DS,1=1),Id), Set(DS, [{Id:1,Name:\"a\"}]); Id)                 ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("StdevP(Sort(Filter(DS,1=1),Id), Set(DS, [{Id:1,Name:\"a\"}]); Id)               ", "   SelfMod", "   SelfMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Concat(Sort(Filter(DS,1=1),Id), Set(First(DS).Id, 3); Text(Id))                 ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Distinct(Sort(Filter(DS,1=1),Id), Set(First(DS).Id, 3); Id)                     ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Sum(Sort(Filter(DS,1=1),Id), Set(First(DS).Id, 3); Id)                          ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Average(Sort(Filter(DS,1=1),Id), Set(First(DS).Id, 3); Id)                      ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Min(Sort(Filter(DS,1=1),Id), Set(First(DS).Id, 3); Id)                          ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Max(Sort(Filter(DS,1=1),Id), Set(First(DS).Id, 3); Id)                          ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("VarP(Sort(Filter(DS,1=1),Id), Set(First(DS).Id, 3); Id)                         ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("StdevP(Sort(Filter(DS,1=1),Id), Set(First(DS).Id, 3); Id)                       ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]

        // Set self modifying, SortByColumns DS, non-delegable                                          delegable DS, enhanced    ,   non-del DS, enhanced    ,  variable DS, enhanced

        [InlineData("ForAll(SortByColumns(DS,\"Id\"), Set(DS, [{Id:1,Name:\"a\"}]))                  ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("ForAll(SortByColumns(DS,\"Id\"), Set(First(DS).Id, 3))                          ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("AddColumns(SortByColumns(DS,\"Id\"), Num, Set(DS, [{Id:1,Name:\"a\"}]); 2)      ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("AddColumns(SortByColumns(DS,\"Id\"), Num, Set(First(DS).Id, 3); 2)              ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Concat(SortByColumns(DS,\"Id\"), Set(DS, [{Id:1,Name:\"a\"}]); Text(Id))        ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Distinct(SortByColumns(DS,\"Id\"), Set(DS, [{Id:1,Name:\"a\"}]); Id)            ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Sum(SortByColumns(DS,\"Id\"), Set(DS, [{Id:1,Name:\"a\"}]); Id)                 ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Average(SortByColumns(DS,\"Id\"), Set(DS, [{Id:1,Name:\"a\"}]); Id)             ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Min(SortByColumns(DS,\"Id\"), Set(DS, [{Id:1,Name:\"a\"}]); Id)                 ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Max(SortByColumns(DS,\"Id\"), Set(DS, [{Id:1,Name:\"a\"}]); Id)                 ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("VarP(SortByColumns(DS,\"Id\"), Set(DS, [{Id:1,Name:\"a\"}]); Id)                ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("StdevP(SortByColumns(DS,\"Id\"), Set(DS, [{Id:1,Name:\"a\"}]); Id)              ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Concat(SortByColumns(DS,\"Id\"), Set(First(DS).Id, 3); Text(Id))                ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Distinct(SortByColumns(DS,\"Id\"), Set(First(DS).Id, 3); Id)                    ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Sum(SortByColumns(DS,\"Id\"), Set(First(DS).Id, 3); Id)                         ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Average(SortByColumns(DS,\"Id\"), Set(First(DS).Id, 3); Id)                     ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Min(SortByColumns(DS,\"Id\"), Set(First(DS).Id, 3); Id)                         ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Max(SortByColumns(DS,\"Id\"), Set(First(DS).Id, 3); Id)                         ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("VarP(SortByColumns(DS,\"Id\"), Set(First(DS).Id, 3); Id)                        ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("StdevP(SortByColumns(DS,\"Id\"), Set(First(DS).Id, 3); Id)                      ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]

        // Set self modifying, FirstN DS, non delegable                                                 delegable DS, enhanced    ,   non-del DS, enhanced    ,  variable DS, enhanced

        [InlineData("ForAll(FirstN(DS,10), Set(DS, [{Id:1,Name:\"a\"}]))                             ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("ForAll(FirstN(DS,10), Set(First(DS).Id, 3))                                     ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("AddColumns(FirstN(DS,10), Num, Set(DS, [{Id:1,Name:\"a\"}]); 2)                 ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("AddColumns(FirstN(DS,10), Num, Set(First(DS).Id, 3); 2)                         ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Concat(FirstN(DS,10), Set(DS, [{Id:1,Name:\"a\"}]); Text(Id))                   ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Distinct(FirstN(DS,10), Set(DS, [{Id:1,Name:\"a\"}]); Id)                       ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Sum(FirstN(DS,10), Set(DS, [{Id:1,Name:\"a\"}]); Id)                            ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Average(FirstN(DS,10), Set(DS, [{Id:1,Name:\"a\"}]); Id)                        ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Min(FirstN(DS,10), Set(DS, [{Id:1,Name:\"a\"}]); Id)                            ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Max(FirstN(DS,10), Set(DS, [{Id:1,Name:\"a\"}]); Id)                            ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("VarP(FirstN(DS,10), Set(DS, [{Id:1,Name:\"a\"}]); Id)                           ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("StdevP(FirstN(DS,10), Set(DS, [{Id:1,Name:\"a\"}]); Id)                         ", "   CantMod", "   CantMod", "   CantMod", "   CantMod", "        Ok", "        Ok")]
        [InlineData("Concat(FirstN(DS,10), Set(First(DS).Id, 3); Text(Id))                           ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Distinct(FirstN(DS,10), Set(First(DS).Id, 3); Id)                               ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Sum(FirstN(DS,10), Set(First(DS).Id, 3); Id)                                    ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Average(FirstN(DS,10), Set(First(DS).Id, 3); Id)                                ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Min(FirstN(DS,10), Set(First(DS).Id, 3); Id)                                    ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("Max(FirstN(DS,10), Set(First(DS).Id, 3); Id)                                    ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("VarP(FirstN(DS,10), Set(First(DS).Id, 3); Id)                                   ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]
        [InlineData("StdevP(FirstN(DS,10), Set(First(DS).Id, 3); Id)                                 ", " Immutable", " Immutable", " Immutable", " Immutable", "        Ok", "        Ok")]

        // Remove non self modifying                                                                    delegable DS, enhanced    ,   non-del DS, enhanced    ,  variable DS, enhanced

        [InlineData("ForAll(DS, Remove(TblVar, First(TblVar)))                                       ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("AddColumns(DS, Num, Remove(TblVar, First(TblVar)); 2)                           ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Concat(DS, Remove(TblVar, First(TblVar)); Text(Id))                             ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Distinct(DS, Remove(TblVar, First(TblVar)); Id)                                 ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Sum(DS, Remove(TblVar, First(TblVar)); Id)                                      ", "NonDelWarn", "NonDelWarn", "NonDelWnOp", "NonDelWnOp", "        Ok", "        Ok")]
        [InlineData("Average(DS, Remove(TblVar, First(TblVar)); Id)                                  ", "NonDelWarn", "NonDelWarn", "NonDelWnOp", "NonDelWnOp", "        Ok", "        Ok")]
        [InlineData("Min(DS, Remove(TblVar, First(TblVar)); Id)                                      ", "NonDelWarn", "NonDelWarn", "NonDelWnOp", "NonDelWnOp", "        Ok", "        Ok")]
        [InlineData("Max(DS, Remove(TblVar, First(TblVar)); Id)                                      ", "NonDelWarn", "NonDelWarn", "NonDelWnOp", "NonDelWnOp", "        Ok", "        Ok")]
        [InlineData("VarP(DS, Remove(TblVar, First(TblVar)); Id)                                     ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("StdevP(DS, Remove(TblVar, First(TblVar)); Id)                                   ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]

        // Remove self modifying, direct DS                                                             delegable DS, enhanced    ,   non-del DS, enhanced    ,  variable DS, enhanced

        [InlineData("ForAll(DS, Remove(DS, {}))                                                      ", "   SelfMod", "   SelfMod", "   SelfMod", "   SelfMod", "        Ok", "   SelfMod")]
        [InlineData("AddColumns(DS, Num, Remove(DS, {}); 2)                                          ", "   SelfMod", "   SelfMod", "   SelfMod", "   SelfMod", "        Ok", "   SelfMod")]
        [InlineData("Concat(DS, Remove(DS, {}); Text(Id))                                            ", "   SelfMod", "   SelfMod", "   SelfMod", "   SelfMod", "        Ok", "   SelfMod")]
        [InlineData("Distinct(DS, Remove(DS, {}); Id)                                                ", "   SelfMod", "   SelfMod", "   SelfMod", "   SelfMod", "        Ok", "   SelfMod")]
        [InlineData("Sum(DS, Remove(DS, {}); Id)                                                     ", "   SelfMod", "   SelfMod", "   SelfMod", "   SelfMod", "        Ok", "   SelfMod")]
        [InlineData("Average(DS, Remove(DS, {}); Id)                                                 ", "   SelfMod", "   SelfMod", "   SelfMod", "   SelfMod", "        Ok", "   SelfMod")]
        [InlineData("Min(DS, Remove(DS, {}); Id)                                                     ", "   SelfMod", "   SelfMod", "   SelfMod", "   SelfMod", "        Ok", "   SelfMod")]
        [InlineData("Max(DS, Remove(DS, {}); Id)                                                     ", "   SelfMod", "   SelfMod", "   SelfMod", "   SelfMod", "        Ok", "   SelfMod")]
        [InlineData("VarP(DS, Remove(DS, {}); Id)                                                    ", "   SelfMod", "   SelfMod", "   SelfMod", "   SelfMod", "        Ok", "   SelfMod")]
        [InlineData("StdevP(DS, Remove(DS, {}); Id)                                                  ", "   SelfMod", "   SelfMod", "   SelfMod", "   SelfMod", "        Ok", "   SelfMod")]

        // Remove self modifying, with Sort                                                             delegable DS, enhanced    ,   non-del DS, enhanced    ,  variable DS, enhanced

        [InlineData("ForAll(Sort(DS,Id), Remove(DS, {}))                                             ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("AddColumns(Sort(DS,Id), Num, Remove(DS, {}); 2)                                 ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Concat(Sort(DS,Id), Remove(DS, {}); Text(Id))                                   ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Distinct(Sort(DS,Id), Remove(DS, {}); Id)                                       ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Sum(Sort(DS,Id), Remove(DS, {}); Id)                                            ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Average(Sort(DS,Id), Remove(DS, {}); Id)                                        ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Min(Sort(DS,Id), Remove(DS, {}); Id)                                            ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Max(Sort(DS,Id), Remove(DS, {}); Id)                                            ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("VarP(Sort(DS,Id), Remove(DS, {}); Id)                                           ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("StdevP(Sort(DS,Id), Remove(DS, {}); Id)                                         ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]

        // Remove self modifying, with Filter                                                           delegable DS, enhanced    ,   non-del DS, enhanced    ,  variable DS, enhanced

        [InlineData("ForAll(Filter(DS,1=1), Remove(DS, {}))                                          ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("AddColumns(Filter(DS,1=1), Num, Remove(DS, {}); 2)                              ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Concat(Filter(DS,1=1), Remove(DS, {}); Text(Id))                                ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Distinct(Filter(DS,1=1), Remove(DS, {}); Id)                                    ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Sum(Filter(DS,1=1), Remove(DS, {}); Id)                                         ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Average(Filter(DS,1=1), Remove(DS, {}); Id)                                     ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Min(Filter(DS,1=1), Remove(DS, {}); Id)                                         ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Max(Filter(DS,1=1), Remove(DS, {}); Id)                                         ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("VarP(Filter(DS,1=1), Remove(DS, {}); Id)                                        ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("StdevP(Filter(DS,1=1), Remove(DS, {}); Id)                                      ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]

        // Remove self modifying, Sort composed with Filter DS                                          delegable DS, enhanced    ,   non-del DS, enhanced    ,  variable DS, enhanced

        [InlineData("ForAll(Sort(Filter(DS,1=1),Id), Remove(DS, {}))                                 ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("AddColumns(Sort(Filter(DS,1=1),Id), Num, Remove(DS, {}); 2)                     ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Concat(Sort(Filter(DS,1=1),Id), Remove(DS, {}); Text(Id))                       ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Distinct(Sort(Filter(DS,1=1),Id), Remove(DS, {}); Id)                           ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Sum(Sort(Filter(DS,1=1),Id), Remove(DS, {}); Id)                                ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Average(Sort(Filter(DS,1=1),Id), Remove(DS, {}); Id)                            ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Min(Sort(Filter(DS,1=1),Id), Remove(DS, {}); Id)                                ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Max(Sort(Filter(DS,1=1),Id), Remove(DS, {}); Id)                                ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("VarP(Sort(Filter(DS,1=1),Id), Remove(DS, {}); Id)                               ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("StdevP(Sort(Filter(DS,1=1),Id), Remove(DS, {}); Id)                             ", "   SelfMod", "   SelfMod", "        Ok", "        Ok", "        Ok", "        Ok")]

        // Remove self modifying, SortByColumns DS, non-delegable                                       delegable DS, enhanced    ,   non-del DS, enhanced    ,  variable DS, enhanced

        [InlineData("ForAll(SortByColumns(DS,\"Id\"), Remove(DS, {}))                                ", "NonDelWarn", "NonDelWarn", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("AddColumns(SortByColumns(DS,\"Id\"), Num, Remove(DS, {}); 2)                    ", "NonDelWarn", "NonDelWarn", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Concat(SortByColumns(DS,\"Id\"), Remove(DS, {}); Text(Id))                      ", "NonDelWarn", "NonDelWarn", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Distinct(SortByColumns(DS,\"Id\"), Remove(DS, {}); Id)                          ", "NonDelWarn", "NonDelWarn", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Sum(SortByColumns(DS,\"Id\"), Remove(DS, {}); Id)                               ", "NonDelWarn", "NonDelWarn", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Average(SortByColumns(DS,\"Id\"), Remove(DS, {}); Id)                           ", "NonDelWarn", "NonDelWarn", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Min(SortByColumns(DS,\"Id\"), Remove(DS, {}); Id)                               ", "NonDelWarn", "NonDelWarn", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Max(SortByColumns(DS,\"Id\"), Remove(DS, {}); Id)                               ", "NonDelWarn", "NonDelWarn", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("VarP(SortByColumns(DS,\"Id\"), Remove(DS, {}); Id)                              ", "NonDelWarn", "NonDelWarn", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("StdevP(SortByColumns(DS,\"Id\"), Remove(DS, {}); Id)                            ", "NonDelWarn", "NonDelWarn", "        Ok", "        Ok", "        Ok", "        Ok")]

        // Remove self modifying, FirstN DS, non delegable                                              delegable DS, enhanced    ,   non-del DS, enhanced    ,  variable DS, enhanced

        [InlineData("ForAll(FirstN(DS,10), Remove(DS, {}))                                           ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("AddColumns(FirstN(DS,10), Num, Remove(DS, {}); 2)                               ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Concat(FirstN(DS,10), Remove(DS, {}); Text(Id))                                 ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Distinct(FirstN(DS,10), Remove(DS, {}); Id)                                     ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Sum(FirstN(DS,10), Remove(DS, {}); Id)                                          ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Average(FirstN(DS,10), Remove(DS, {}); Id)                                      ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Min(FirstN(DS,10), Remove(DS, {}); Id)                                          ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("Max(FirstN(DS,10), Remove(DS, {}); Id)                                          ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("VarP(FirstN(DS,10), Remove(DS, {}); Id)                                         ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]
        [InlineData("StdevP(FirstN(DS,10), Remove(DS, {}); Id)                                       ", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok", "        Ok")]

        public void MutationFunctionSelfModifyingDetection(string script, string expectedErrorDSDelegable, string expectedErrorDSDelegableEnhanced = null, string expectedErrorDSNonDelegable = null, string expectedErrorDSNonDelegableEnhanced = null, string expectedErrorVar = null, string expectedErrorVarEnhanced = null)
        {
            TexlTestUtils.TexlCheckFunctionSideEffects(
                symbol =>
                {
                    symbol.EnableMutationFunctions();
                    symbol.AddFunction(new DistinctFunction());
                }, 
                script, 
                expectedErrorDSDelegable, 
                expectedErrorDSDelegableEnhanced, 
                expectedErrorDSNonDelegable, 
                expectedErrorDSNonDelegableEnhanced, 
                expectedErrorVar, 
                expectedErrorVarEnhanced);
        }

        [Fact]
        public void UnknownKindInErrorMessage()
        {
            var config = new PowerFxConfig();
            config.AddFunction(new UnknownReturnFunction());
            config.SymbolTable.EnableMutationFunctions();
            config.SymbolTable.AddVariable("t", FormulaType.Build(TestUtils.DT("*[foo:n]")), mutable: true);

            var engine = new Engine(config);

            var formula = "Collect(t, UnknownReturn())";
            var result = engine.Check(formula, options: new ParserOptions() { AllowsSideEffects = true });

            Assert.False(result.IsSuccess);
            Assert.Contains(result.Errors, e => e.MessageKey == "ErrBadType_Type");
            Assert.DoesNotContain(result.Errors, e => e.Message.Contains("_Min"));
        }

        [Fact]
        public void AppendErrorTests()
        {
            var config = new PowerFxConfig();
            var engine = new RecalcEngine(config);

            config.SymbolTable.EnableMutationFunctions();

            engine.UpdateVariable("t1", new ErrorTableValue());

            var check = engine.Check("Collect(t1, {a:\"abc\"})", options: new ParserOptions() { AllowsSideEffects = true });
            var result = check.GetEvaluator().Eval();

            Assert.IsType<ErrorValue>(result);
        }

        /// <summary>
        /// Meant to test PatchSingleRecordCoreAsync override. Only tables with primary key column are supported.
        /// </summary>
        internal class EntityTableValue : TableValue
        {
            private readonly InMemoryTableValue _inner;

            public override bool CanShallowCopy => true;

            public EntityTableValue(IEnumerable<RecordValue> records)
            : base((TableType)FormulaType.Build(AccountsTypeHelper.GetDTypeCds()))
            {
                _inner = new InMemoryTableValue(IRContext, records.Select(rec => DValue<RecordValue>.Of(rec)));
            }

            public override IEnumerable<DValue<RecordValue>> Rows => _inner.Rows;

            protected override async Task<DValue<RecordValue>> PatchSingleRecordCoreAsync(RecordValue recordValue, CancellationToken cancellationToken)
            {
                var externalTabularDataSource = Type._type.AssociatedDataSources.Single() as IExternalTabularDataSource;

                // TestDateSource has only one key column.
                var keyFieldName = externalTabularDataSource.GetKeyColumns().First();

                foreach (var row in _inner.Rows)
                {
                    var value1 = row.Value.GetField(keyFieldName);
                    var value2 = recordValue.GetField(keyFieldName);

                    if (value1.TryGetPrimitiveValue(out object primaryKeyValue1) && value2.TryGetPrimitiveValue(out object primaryKeyValue2) && primaryKeyValue1.ToString() == primaryKeyValue2.ToString())
                    {
                        return await row.Value.UpdateFieldsAsync(recordValue, cancellationToken);
                    }
                }

                return DValue<RecordValue>.Of(FormulaValue.NewError(CommonErrors.RecordNotFound()));
            }

            protected override async Task<DValue<RecordValue>> PatchCoreAsync(RecordValue baseRecord, RecordValue changeRecord, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var externalTabularDataSource = Type._type.AssociatedDataSources.Single() as IExternalTabularDataSource;

                // TestDateSource has only one key column.
                var keyFieldName = externalTabularDataSource.GetKeyColumns().First();
                var keyValue = baseRecord.GetField(keyFieldName);

                foreach (var row in _inner.Rows)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var value1 = row.Value.GetField(keyFieldName);

                    if (value1.TryGetPrimitiveValue(out object primaryKeyValue1) && keyValue.TryGetPrimitiveValue(out object primaryKeyValue2) && primaryKeyValue1.ToString() == primaryKeyValue2.ToString())
                    {
                        return await row.Value.UpdateFieldsAsync(changeRecord, cancellationToken);
                    }
                }

                return DValue<RecordValue>.Of(FormulaValue.NewError(CommonErrors.RecordNotFound()));
            }

            public override Task<DValue<RecordValue>> AppendAsync(RecordValue record, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var externalTabularDataSource = Type._type.AssociatedDataSources.Single() as IExternalTabularDataSource;
                var keyFieldName = externalTabularDataSource.GetKeyColumns().First();
                var fields = new List<NamedValue>();

                fields.AddRange(record.Fields);

                // If the key field is not present in the record, add it.
                if (!record.Fields.Any(f => f.Name == keyFieldName))
                {
                    var keyValue = New(Guid.NewGuid());
                    fields.Add(new NamedValue(keyFieldName, keyValue));
                }

                return _inner.AppendAsync(FormulaValue.NewRecordFromFields(fields), cancellationToken);
            }
        }

        internal class FileObjectRecordValue : InMemoryRecordValue
        {
            public string SomeProperty { get; set; }

            public FileObjectRecordValue(string someProperty, IRContext irContext, IEnumerable<NamedValue> fields)
                : base(irContext, fields)
            {
                SomeProperty = someProperty;
            }

            public override bool TryShallowCopy(out FormulaValue copy)
            {
                copy = new FileObjectRecordValue(SomeProperty, IRContext, Fields);
                return true;
            }
        }

        private RecalcEngine PatchEngine
        {
            get
            {
                var engine = new RecalcEngine();

                var record1 = FormulaValue.NewRecordFromFields(
                    new List<NamedValue>
                {
                new NamedValue("accountid", FormulaValue.New(Guid.Parse("00000000-0000-0000-0000-000000000001"))),
                new NamedValue("name", FormulaValue.New("John Doe")),
                new NamedValue("address1_city", FormulaValue.New("Chicago"))
                });

                var record2 = FormulaValue.NewRecordFromFields(
                    new List<NamedValue>
                {
                new NamedValue("accountid", FormulaValue.New(Guid.Parse("00000000-0000-0000-0000-000000000002"))),
                new NamedValue("name", FormulaValue.New("Sam Doe")),
                new NamedValue("address1_city", FormulaValue.New("New York"))
                });

                var varTableValue = new EntityTableValue(new List<RecordValue>() { record1, record2 });

                engine.Config.SymbolTable.EnableMutationFunctions();
                engine.UpdateVariable("t1", varTableValue);

                return engine;
            }
        }

        internal class ErrorTableValue : TableValue
        {
            public ErrorTableValue()
                : base(TableType.Empty().Add("a", FormulaType.String))
            {
            }

            public override IEnumerable<DValue<RecordValue>> Rows => throw new NotImplementedException();

            public override bool CanShallowCopy => true;

            // This simulates a possible scenario wher Dataverse turns an error when trying to append a record.
            public override Task<DValue<RecordValue>> AppendAsync(RecordValue record, CancellationToken cancellationToken)
            {
                return Task.FromResult(DValue<RecordValue>.Of(FormulaValue.NewError(CommonErrors.RecordNotFound())));
            }
        }

        /// <summary>
        /// A function with an unknown return type used in testing.
        /// </summary>
        internal class UnknownReturnFunction : TexlFunction
        {
            public UnknownReturnFunction()
                : base(
                      DPath.Root,
                      "UnknownReturn",
                      "UnknownReturn",
                      TexlStrings.AboutSet, // just to add something
                      FunctionCategories.Information,
                      DType.Unknown,
                      0, // no lambdas
                      0, // no args
                      0)
            {
            }

            public override bool IsSelfContained => true;

            public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
            {
                yield break;
            }
        }
    }
}
