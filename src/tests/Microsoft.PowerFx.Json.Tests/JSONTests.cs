// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Json.Tests
{
    public class JSONTests
    {        
        [Fact]
        public void Json_IncludeBinaryData_AllowSideEffects()
        {
            var config = new PowerFxConfig();
            config.EnableJsonFunctions();

            var engine = new RecalcEngine(config);

            var result = engine.Eval("JSON(5, JSONFormat.IncludeBinaryData)", options: new ParserOptions() { AllowsSideEffects = true });
            Assert.Equal("5", result.ToObject());
        }

        [Fact]
        public void Json_IncludeBinaryData_NoSideEffects()
        {
            var config = new PowerFxConfig();
            config.EnableJsonFunctions();

            var engine = new RecalcEngine(config);

            var result = engine.Check("JSON(5, JSONFormat.IncludeBinaryData)", options: new ParserOptions() { AllowsSideEffects = false });
            Assert.False(result.IsSuccess);
            Assert.Equal("The JSON function cannot serialize binary data in non-behavioral expression.", result.Errors.First().Message);
        }

        [Fact]
        public void Json_IncludeBinaryData_WithLazyRecord_NoFeature()
        {
            var config = new PowerFxConfig(Features.None);
            config.EnableJsonFunctions();

            var engine = new RecalcEngine(config);

            var record = RecordType.Empty();
            record = record.Add("Property", new LazyRecordType());

            var formulaParams = RecordType.Empty();
            formulaParams = formulaParams.Add("Var", record);

            var result = engine.Check("JSON(Var)", formulaParams);

            Assert.False(result.IsSuccess);
            Assert.Equal("The JSON function cannot serialize tables / objects with a nested property called 'Property' of type 'Record'.", result.Errors.First().Message);
        }

        [Fact]
        public void Json_IncludeBinaryData_WithLazyRecord()
        {
            var config = new PowerFxConfig(Features.PowerFxV1);
            config.EnableJsonFunctions();

            var engine = new RecalcEngine(config);

            var recordType = RecordType.Empty();
            var lazyRecordType = new LazyRecordType();
            recordType = recordType.Add("Property", lazyRecordType);

            var symbolTable = new SymbolTable();
            var varSlot = symbolTable.AddVariable("Var", recordType);

            var result = engine.Check("JSON(Var)", symbolTable: symbolTable);
            Assert.True(result.IsSuccess);

            var symbolValues = new SymbolValues(symbolTable);
            symbolValues.Set(varSlot, FormulaValue.NewRecordFromFields(new NamedValue[] { new NamedValue("Property", new LazyRecordValue(lazyRecordType, 2)) }));
            var runtimeConfig = new RuntimeConfig(symbolValues);            

            var formulaValue = result.GetEvaluator().Eval(runtimeConfig);
            Assert.Equal(@"{""Property"":{""SubProperty"":{""x"":""test2""}}}", (formulaValue as StringValue).Value);
        }

        [Fact]
        public void Json_IncludeBinaryData_WithLazyTable()
        {
            var config = new PowerFxConfig(Features.PowerFxV1);
            config.EnableJsonFunctions();

            var engine = new RecalcEngine(config);

            var recordType = RecordType.Empty();
            var lazyRecordType = new LazyRecordType();
            recordType = recordType.Add("Property", lazyRecordType);
            var tableType = recordType.ToTable();

            var symbolTable = new SymbolTable();
            var varSlot = symbolTable.AddVariable("Var", tableType);

            var result = engine.Check("JSON(Var)", symbolTable: symbolTable);
            Assert.True(result.IsSuccess);

            var symbolValues = new SymbolValues(symbolTable);
            var tableValue = FormulaValue.NewTable(
                recordType,
                FormulaValue.NewRecordFromFields(new NamedValue[] { new ("Property", new LazyRecordValue(lazyRecordType, 1)) }),
                FormulaValue.NewRecordFromFields(new NamedValue[] { new ("Property", new LazyRecordValue(lazyRecordType, 3)) }));
            symbolValues.Set(varSlot, tableValue);
            var runtimeConfig = new RuntimeConfig(symbolValues);

            var formulaValue = result.GetEvaluator().Eval(runtimeConfig);
            Assert.Equal(@"[{""Property"":{""SubProperty"":{""x"":""test1""}}},{""Property"":{""SubProperty"":{""x"":""test3""}}}]", (formulaValue as StringValue).Value);
        }

        [Fact]
        public void Json_IncludeBinaryData_WithLazyRecordCircularRef()
        {
            var config = new PowerFxConfig(Features.PowerFxV1);
            config.EnableJsonFunctions();

            var engine = new RecalcEngine(config);

            var record = RecordType.Empty();
            record = record.Add("Property", new LazyRecordTypeCircularRef());

            var formulaParams = RecordType.Empty();
            formulaParams = formulaParams.Add("Var", record);

            var result = engine.Check("JSON(Var)", formulaParams);

            Assert.False(result.IsSuccess);
            Assert.Equal("The JSON function cannot serialize tables / objects with a nested property called 'Property' of type 'Record'.", result.Errors.First().Message);
        }

        [Fact]
        public void Json_Handles_Null_Records()
        {
            var config = new PowerFxConfig(Features.PowerFxV1);
            config.EnableJsonFunctions();

            var engine = new RecalcEngine(config);

            var checkResult = engine.Check("JSON([{a:1},Blank(),{a:3}])");

            Assert.True(checkResult.IsSuccess);

            var evalResult = checkResult.GetEvaluator().Eval();
            Assert.Equal("[{\"a\":1},null,{\"a\":3}]", (evalResult as StringValue).Value);
        }

        [Fact]
        public void Json_Handles_Error_Records()
        {
            var config = new PowerFxConfig(Features.PowerFxV1);
            config.EnableJsonFunctions();

            var engine = new RecalcEngine(config);

            var checkResult = engine.Check("JSON(Filter(Sequence(5,-2), 1/Value > 0))");

            Assert.True(checkResult.IsSuccess);

            var evalResult = checkResult.GetEvaluator().Eval();
            Assert.True(evalResult is ErrorValue);
            Assert.Equal(ErrorKind.Div0, (evalResult as ErrorValue).Errors.First().Kind);
        }

        public class LazyRecordValue : RecordValue
        {
            private readonly int _i;

            public LazyRecordValue(RecordType type, int i) 
                : base(type)
            {
                _i = i;
            }

            protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
            {
                Assert.Equal("SubProperty", fieldName);
                Assert.Equal("![x:s]", fieldType._type.ToString());
                result = FormulaValue.NewRecordFromFields(new NamedValue[] { new NamedValue("x", FormulaValue.New($"test{_i}")) });
                return true;
            }
        }
        
        public class LazyRecordType : RecordType
        {
            public LazyRecordType()
            {
            }

            public override IEnumerable<string> FieldNames => new string[1] { "SubProperty" };

            public override bool TryGetFieldType(string name, out FormulaType type)
            {
                var subrecord = RecordType.Empty();
                subrecord = subrecord.Add("x", FormulaType.String);

                type = subrecord;
                return true;
            }           

            public override int GetHashCode()
            {
                return 1;
            }

            public override bool Equals(object other)
            {
                if (other == null)
                {
                    return false;
                }

                return true;
            }
        }

        public class LazyRecordTypeCircularRef : RecordType
        {
            public LazyRecordTypeCircularRef()
            {
            }

            public override IEnumerable<string> FieldNames => new string[1] { "SubProperty" };

            public override bool TryGetFieldType(string name, out FormulaType type)
            {
                var subrecord = RecordType.Empty();

                // Circular reference
                subrecord = subrecord.Add("SubProperty2", this);

                type = subrecord;
                return true;
            }

            public override bool Equals(object other)
            {
                return true;
            }

            public override int GetHashCode()
            {
                return 1;
            }
        }
    }
}
