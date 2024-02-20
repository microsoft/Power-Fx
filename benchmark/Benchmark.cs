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
using PowerFXBenchmark.TypedObjects;
using PowerFXBenchmark.Inputs;
using System.Text.Json.Nodes;

namespace PowerFXBenchmark
{
    public class Benchmark
    {
        public readonly RecalcEngine engine;

        public readonly TestObject testObj;
        public readonly TestObjectSchema testObjSchema;

        private readonly string jsonString;
        private readonly JsonElement jsonElement;

        public readonly RecordType testObjRecordType;
        public readonly RecordType jsonRecordType;
        public readonly RecordValue testObjRecordValue;
        public readonly RecordValue jsonRecordValue;

        public readonly ParseResult parseResult;
        public readonly IExpressionEvaluator expressionEvaluator;

        private readonly string expression = "testObj.Temperature < 10 && testObj.DisplayName = \"blah\" && (testObj.ComfortIndex * 3.14 > 140) && json.data.properties.MyTemperature < 10 && json.data.properties.MyName = \"blah\" && (json.data.comfortIndex * 3.14 > 140)";

        public Benchmark()
        {
            engine = new RecalcEngine();

            // Raw Inputs
            testObj = InputGenerator.GenerateTestObject();
            testObjSchema = InputGenerator.GenerateTestObjectSchema();
            jsonString = InputGenerator.GenerateJson("json_small");

            // Intermediate Inputs
            jsonElement = JsonDocument.Parse(jsonString).RootElement;

            // Power FX formats
            testObjRecordType = Convert_TestObjSchema_To_PowerFX_RecordType();
            testObjRecordValue = Convert_TestObj_To_PowerFX_RecordValue();
            jsonRecordValue = (RecordValue)Convert_JsonElement_To_PowerFX_RecordValue();
            jsonRecordType = jsonRecordValue.Type;

            parseResult = Parse();
            expressionEvaluator = TypeCheck();
        }

        [Benchmark]
        public RecordType Convert_TestObjSchema_To_PowerFX_RecordType()
        {
            return testObjSchema.ToRecordType();
        }

        [Benchmark]
        public FormulaValue Convert_JsonElement_To_PowerFX_RecordValue()
        {
            return FormulaValue.FromJson(jsonElement);
        }

        [Benchmark]
        public RecordValue Convert_TestObj_To_PowerFX_RecordValue()
        {
            return new TestObjectRecordValue(testObj, testObjSchema, testObjRecordType);
        }

        [Benchmark]
        public ParseResult Parse()
        {
            var parse = engine.Parse(expression);
            if (!parse.IsSuccess)
            {
                throw new Exception("Parse error");
            }

            return parse;
        }

        [Benchmark]
        public IExpressionEvaluator TypeCheck()
        {
            var symbolTable = new SymbolTable();
            symbolTable.AddVariable("testObj", testObjRecordType);
            symbolTable.AddVariable("json", jsonRecordType);

            var checkResult = engine.Check(parseResult, null, symbolTable);
            if (!checkResult.IsSuccess)
            {
                throw new Exception("Check error");
            }

            return checkResult.GetEvaluator();
        }

        [Benchmark]
        public async Task<object> EvaluateAsync()
        {
            var symbolValues = new SymbolValues()
                .Add("testObj", testObjRecordValue)
                .Add("json", jsonRecordValue);

            return (await expressionEvaluator.EvalAsync(default, symbolValues).ConfigureAwait(false)).ToObject();
        }
    }
}
