using BenchmarkDotNet.Attributes;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq.Expressions;
using PowerFXBenchmark.Builders;
using PowerFXBenchmark.Inputs.Models;
using PowerFXBenchmark.UntypedObjects;
using PowerFXBenchmark.Inputs;

namespace PowerFXBenchmark
{
    public class Benchmark
    {
        private readonly EngineWrapper engine;

        private readonly RecordType untypedRecordtype;
        private readonly RecordType stronglyTypedRecordtype;
        private readonly IExpression Expression_Untyped;
        private readonly IExpression Expression_StronglyTyped;
        private readonly IExpression parseJsonExpression;

        private readonly IExpression Expression_Complexity1;
        private readonly IExpression Expression_Complexity2;

        private readonly RecordValue stronglyTypedInput;
        private readonly TestObject testObj;
        private readonly string testObjJson;
        private readonly string telemetryJson;
        private readonly JsonElement telemetryJsonElement;


        public Benchmark()
        {
            engine = new EngineWrapper();

            // Input
            testObj = InputGenerator.GenerateTestObject();
            testObjJson = InputGenerator.GenerateJson("susoTestObject");
            telemetryJson = InputGenerator.GenerateJson("telemetry_6KB");
            telemetryJsonElement = JsonDocument.Parse(telemetryJson).RootElement;

            stronglyTypedInput = RecordValue.NewRecordFromFields(
                new NamedValue("testObj", RecordValue.FromJson(testObjJson)),
                new NamedValue("event", RecordValue.FromJson(telemetryJson)));

            // Input schema
            untypedRecordtype = new RecordType()
                .Add(new NamedFormulaType("testObj", FormulaType.UntypedObject))
                .Add(new NamedFormulaType("event", FormulaType.UntypedObject));
            stronglyTypedRecordtype = stronglyTypedInput.Type;

            // Compiled expressions
            parseJsonExpression = engine.CompiledSingleExpression(
                "ParseJSON(x)",
                new RecordType().Add(new NamedFormulaType("x", FormulaType.String)));

            Expression_Untyped = ParseNCheckExpression_UntypedInput();
            Expression_StronglyTyped = ParseNCheckExpression_StronglyTypedInput();
            Expression_Complexity1 = engine.CompiledSingleExpression(
                "(Value(event.data.temperature) - 32) * 5 / 9 > 10",
                untypedRecordtype);
            Expression_Complexity2 = engine.CompiledSingleExpression(
                "(Value(event.data.temperature) - 32) * 5 / 9 > 10 && Text(testObj.'$metadata'.type) = \"powerfx-test-1\"",
                untypedRecordtype);
        }

        [Benchmark]
        public IExpression ParseNCheckExpression_StronglyTypedInput()
        {
            return engine.CompiledSingleExpression(
                "(testObj.Temperature - 32) * 5 / 9 > 10 && !event.data.isActive && event.data.testId <> \"mytest\"",
                //"(Value(testObj.Temperature) - 32) * 5 / 9 > 10  && !event.data.isActive && Text(event.data.testId) <> \"mytest\"",
                stronglyTypedRecordtype);
        }

        [Benchmark]
        public IExpression ParseNCheckExpression_UntypedInput()
        {
            return engine.CompiledSingleExpression(
                "(Value(testObj.Temperature) - 32) * 5 / 9 > 10  && !Boolean(event.data.isActive) && Text(event.data.testId) <> \"mytest\"",
                untypedRecordtype);
        }

        /// <summary>
        /// Evaluate "pre-compiled" expression, including parsing JSON string to untyped object and explicit conversion within the expression
        /// </summary>
        [Benchmark]
        public async Task<object> Evaluation_UntypedInput_ParseJSON()
        {
            var t1 = parseJsonExpression.EvalAsync(RecordValue.NewRecordFromFields(
                new NamedValue("x", FormulaValue.New(testObjJson))), default);
            var t2 = parseJsonExpression.EvalAsync(RecordValue.NewRecordFromFields(
                new NamedValue("x", FormulaValue.New(telemetryJson))), default);

            await Task.WhenAll(t1, t2);
            var parameters = RecordValue.NewRecordFromFields(
                new NamedValue("testObj", t1.Result),
                new NamedValue("event", t2.Result));

            var result = await Expression_Untyped
                .EvalAsync(parameters, default)
                .ConfigureAwait(false);

            return result.ToObject();
        }

        /// <summary>
        /// Evaluate "pre-compiled" expression, with C# TestObject and json string as input.
        /// </summary>
        [Benchmark]
        public async Task<object> Evaluation_UntypedInput_CustomUntypedObject()
        {
            var builder = new RecordValueBuilder();
            var parameters = builder
                .WithTestObject(testObj)
                .WithEventJson(telemetryJson)
                .Build();
            var result = await Expression_Untyped
                .EvalAsync(parameters, default)
                .ConfigureAwait(false);

            return result.ToObject();
        }

        /// <summary>
        /// Evaluate "pre-compiled" expression, including parsing JSON string into strongly typed record.
        /// </summary>
        [Benchmark]
        public async Task<object> Evaluation_StronglyTypedInput()
        {
            var parameters = RecordValue
                .NewRecordFromFields(
                    new NamedValue("testObj", RecordValue.FromJson(testObjJson)),
                    new NamedValue("event", RecordValue.FromJson(telemetryJsonElement)));
            var result = await Expression_StronglyTyped
                .EvalAsync(parameters, default)
                .ConfigureAwait(false);

            return result.ToObject();
        }

        [Benchmark]
        public async Task<object> Evaluation_Expression_Complexity1()
        {
            var builder = new RecordValueBuilder();
            var parameters = builder
                .WithTestObject(testObj)
                .WithEventJson(telemetryJson)
                .Build();
            var result = await Expression_Complexity1
                .EvalAsync(parameters, default)
                .ConfigureAwait(false);

            return result.ToObject();
        }

        [Benchmark]
        public async Task<object> Evaluation_Expression_Complexity2()
        {
            var builder = new RecordValueBuilder();
            var parameters = builder
                .WithTestObject(testObj)
                .WithEventJson(telemetryJson)
                .Build();
            var result = await Expression_Complexity2
                .EvalAsync(parameters, default)
                .ConfigureAwait(false);

            return result.ToObject();
        }
    }
}
