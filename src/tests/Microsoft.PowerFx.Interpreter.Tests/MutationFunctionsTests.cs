// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Tests.AssociatedDataSourcesTests;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Core.Types;
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
            var functionName = expression.Split("(")[0];
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

        [Theory]
        [InlineData("Patch(t, First(t), {Value:1})")]
        public void MutationPFxV1Disabled(string expression)
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

            // Runtime exception
            Assert.ThrowsAsync<InvalidOperationException>(async () => await evaluator.EvalAsync(CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Theory]        
        [InlineData("Patch(t1, {accountid:GUID(\"00000000-0000-0000-0000-000000000001\"),name:\"Mary Doe\"});First(t1).name", "Mary Doe")]
        [InlineData("Patch(t1, {accountid:GUID(\"00000000-0000-0000-0000-000000000001\"),address1_city:\"Seattle\"});First(t1).name", "John Doe")]
        [InlineData("Patch(t1, {accountid:GUID(\"00000000-0000-0000-0000-000000000003\"),name:\"Microsoft Corporation\",address1_city:\"Seattle\"});Last(t1).name", "Microsoft Corporation")]
        [InlineData("Patch(t1, Table({accountid:GUID(\"00000000-0000-0000-0000-000000000001\"),name:\"Emily Doe\"},{accountid:GUID(\"00000000-0000-0000-0000-000000000002\"),name:\"Benjamin Doe\"}));Last(t1).name", "Benjamin Doe")]
        [InlineData("Patch(t1, Table({accountid:GUID(\"00000000-0000-0000-0000-000000000001\"),address1_city:\"Miami\"},{accountid:GUID(\"00000000-0000-0000-0000-000000000002\"),address1_city:\"Orlando\"}));Last(t1).name", "Sam Doe")]
        [InlineData("Patch(t1, Table({accountid:GUID(\"00000000-0000-0000-0000-000000000003\"),name:\"Microsoft Corporation\",address1_city:\"Seattle\"},{accountid:GUID(\"00000000-0000-0000-0000-000000000004\"),name:\"Bill Gates\",address1_city:\"Seattle\"}));Last(t1).name", "Bill Gates")]
        public void MutationEntityTests(string expression, string expected)
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

            engine.Config.SymbolTable.EnableMutationFunctions();
            engine.UpdateVariable("t1", new EntityTableValue(new List<RecordValue>() { record1, record2 }));

            var check = engine.Check(expression, options: new ParserOptions() { AllowsSideEffects = true });

            Assert.True(check.IsSuccess);

            var result = check.GetEvaluator().Eval();
            Assert.IsNotType<ErrorValue>(result);

            Assert.Equal(expected, ((StringValue)result).Value);
        }

        /// <summary>
        /// Meant to test PatchSingleRecordCoreAsync override. Only tables with primary key column are supported.
        /// </summary>
        internal class EntityTableValue : TableValue
        {
            private readonly InMemoryTableValue _inner;

            public override bool CanShallowCopy => true;

            public EntityTableValue(IEnumerable<RecordValue> records)
            : base((TableType)FormulaType.Build(AccountsTypeHelper.GetDType()))
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
                        return await row.Value.UpdateFieldsAsync(recordValue, cancellationToken).ConfigureAwait(false);
                    }
                }

                return DValue<RecordValue>.Of(FormulaValue.NewError(CommonErrors.RecordNotFound()));
            }

            public override Task<DValue<RecordValue>> AppendAsync(RecordValue record, CancellationToken cancellationToken)
            {
                return _inner.AppendAsync(record, cancellationToken);
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
    }
}
