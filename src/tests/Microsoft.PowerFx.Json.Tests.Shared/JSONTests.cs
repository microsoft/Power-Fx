// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Types;
using Xunit;
using static Microsoft.PowerFx.Json.Tests.JSONTests.ServiceNowTableMetadata;

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
        public void Json_IncludeBinaryData_WithLazyRecord_DepthOne()
        {
            var config = new PowerFxConfig(Features.PowerFxV1);
            config.EnableJsonFunctions(new JsonSettings() { MaxDepth = 1 });

            var engine = new RecalcEngine(config);

            var recordType = RecordType.Empty();
            var lazyRecordType = new LazyRecordType();
            recordType = recordType.Add("Property", lazyRecordType);

            var symbolTable = new SymbolTable();
            var varSlot = symbolTable.AddVariable("Var", recordType);

            var result = engine.Check("JSON(Var)", symbolTable: symbolTable);
            Assert.True(result.IsSuccess, string.Join(", ", result.Errors.Select(er => er.Message)));

            var symbolValues = new SymbolValues(symbolTable);
            symbolValues.Set(varSlot, FormulaValue.NewRecordFromFields(new NamedValue[] { new NamedValue("Property", new LazyRecordValue(lazyRecordType, 2)) }));
            var runtimeConfig = new RuntimeConfig(symbolValues);

            var formulaValue = result.GetEvaluator().Eval(runtimeConfig);
            Assert.Equal(@"{""Property"":{}}", (formulaValue as StringValue).Value);
        }

        [Fact]
        public void Json_IncludeBinaryData_WithLazyRecord_DepthTwo()
        {
            var config = new PowerFxConfig(Features.PowerFxV1);
            config.EnableJsonFunctions(new JsonSettings() { MaxDepth = 2 });

            var engine = new RecalcEngine(config);

            var recordType = RecordType.Empty();
            var lazyRecordType = new LazyRecordType();
            recordType = recordType.Add("Property", lazyRecordType);

            var symbolTable = new SymbolTable();
            var varSlot = symbolTable.AddVariable("Var", recordType);

            var result = engine.Check("JSON(Var)", symbolTable: symbolTable);
            Assert.True(result.IsSuccess, string.Join(", ", result.Errors.Select(er => er.Message)));

            var symbolValues = new SymbolValues(symbolTable);
            symbolValues.Set(varSlot, FormulaValue.NewRecordFromFields(new NamedValue[] { new NamedValue("Property", new LazyRecordValue(lazyRecordType, 2)) }));
            var runtimeConfig = new RuntimeConfig(symbolValues);

            var formulaValue = result.GetEvaluator().Eval(runtimeConfig);
            Assert.Equal(@"{""Property"":{""SubProperty"":{}}}", (formulaValue as StringValue).Value);
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

            // We are protected by MaxDepth default value
            Assert.True(result.IsSuccess);
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
            Assert.IsType<ErrorValue>(evalResult);
            Assert.Equal(ErrorKind.Div0, (evalResult as ErrorValue).Errors.First().Kind);
        }

        [Theory]
        [InlineData(1, "local", 1, false, @"{ ""logical1"": ""a"", ""logical2"": ""b"" }", @"{""logical1"":""a"",""logical2"":""b"",""ref"":null}")]
        [InlineData(2, "local", 2, true, @"{ ""logical1"": ""a"", ""ref"": { ""Link"": ""someLink"", ""Value"": ""2b"" } }", @"{""logical1"":""a"",""logical2"":null,""ref"":{""logical1"":""a"",""logical2"":""b"",""ref"":null}}")]
        [InlineData(3, "local", 1, true, @"{ ""logical1"": ""a"", ""ref"": { ""Link"": ""someLink"", ""Value"": ""2d"" } }", @"{""logical1"":""a"",""logical2"":null,""ref"":{}}")]
        public void Json_ServiceNow(int id, string tableName, int maxDepth, bool includeCachedItems, string json, string expected)
        {
            var config = new PowerFxConfig(Features.PowerFxV1);
            config.EnableJsonFunctions(new JsonSettings() { MaxDepth = maxDepth });

            ServiceNowDataSource resolver = new ServiceNowDataSource(maxDepth);

            if (includeCachedItems)
            {
                resolver.CachedItems = new Dictionary<(string, string), string>()
                {
                    { ("someLink", "2b"),  @"{ ""logical1"": ""a"", ""logical2"": ""b"" }" },
                    { ("someLink", "2e"),  @"{ ""logical1"": ""d"", ""logical2"": ""e"" }" }
                };
            }

            ServiceNowRecordType snrt = new ServiceNowRecordType($"foo{id}", ServiceNowDataSource.GetMetadata(tableName), resolver);
            SymbolTable st = new SymbolTable();
            ISymbolSlot snrowSlot = st.AddVariable("snrow", snrt);

            RecalcEngine engine = new RecalcEngine(config);
            CheckResult checkResult = engine.Check("JSON(snrow, JSONFormat.IgnoreUnsupportedTypes)", symbolTable: st);

            Assert.True(checkResult.IsSuccess);

            SymbolValues sv = new SymbolValues(st);
            JsonObject jo = JsonSerializer.Deserialize<JsonObject>(json);
            ServiceNowRow snRow = new ServiceNowRow(jo);
            sv.Set(snrowSlot, new ServiceNowRecordValue(snRow, snrt, resolver));
            RuntimeConfig rc = new RuntimeConfig(sv);

            FormulaValue evalResult = checkResult.GetEvaluator().Eval(rc);
            StringValue strVal = Assert.IsType<StringValue>(evalResult);

            Assert.Equal(expected, strVal.Value);
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

        public class ServiceNowRecordValue : RecordValue
        {
            private ServiceNowRow _item;

            private readonly ServiceNowRecordType _recordType;
            private readonly ServiceNowReferenceValue _refValue;
            private readonly IServiceNowTableResolver _resolver;
            
            internal ServiceNowRecordValue(ServiceNowRow item, ServiceNowRecordType recordType, IServiceNowTableResolver resolver)
                : base(recordType)
            {
                _item = item ?? throw new ArgumentNullException(nameof(item));
                _recordType = recordType ?? throw new ArgumentNullException(nameof(recordType));
                _resolver = resolver;
            }
            
            internal ServiceNowRecordValue(ServiceNowReferenceValue refValue, ServiceNowRecordType recordType, IServiceNowTableResolver resolver)                
                : base(recordType)
            {
                _refValue = refValue;
                _recordType = recordType ?? throw new ArgumentNullException(nameof(recordType));
                _resolver = resolver;
            }            

            protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
            {                                
                if (!Type.TryGetFieldType(fieldName, out FormulaType propType))
                {
                    Assert.Fail($"Cannot find {fieldName}");
                }                
                
                _item ??= _resolver.GetRowData(_refValue);                

                object rawValue = _item.GetRawValue(fieldName);

                if (rawValue is ServiceNowReferenceValue refVal)
                {
                    result = new ServiceNowRecordValue(refVal, (ServiceNowRecordType)fieldType, _resolver);
                    return true;
                }

                string propStrValue = rawValue?.ToString();
                
                FormulaValue val;

                if (string.IsNullOrEmpty(propStrValue))
                {
                    val = FormulaValue.NewBlank(fieldType);
                }
                else if (fieldType == FormulaType.String)
                {
                    val = FormulaValue.New(propStrValue);
                }                
                else
                {
                    result = null;
                    return false;
                }

                result = val;
                return true;
            }
        }

        public class ServiceNowRow
        {
            private readonly JsonObject _item;

            public ServiceNowRow(JsonObject item)
            {
                _item = item;
            }

            public object GetRawValue(string fieldName)
            {
                var val = _item[fieldName];

                if (val != null)
                {
                    if (val is JsonObject obj)
                    {
                        if (obj.ContainsKey("Link"))
                        {
                            return obj.Deserialize<ServiceNowReferenceValue>();
                        }
                        
                        Assert.Fail($"Unrecognized json object: " + val.ToJsonString());                        
                    }
                    else if (val is JsonValue val2)
                    {                        
                        return val.ToString();                        
                    }
                   
                    Assert.Fail($"Unrecognized json kind: " + val.ToJsonString());                    
                }

                return null;
            }
        }

        internal class ServiceNowRecordType : RecordType
        {
            private readonly string _logicalName;
            private readonly ServiceNowTableMetadata _metadata;
            private readonly IServiceNowTableResolver _resolver;
            
            private static DisplayNameProvider CreateDSP(ServiceNowTableMetadata metadata)
            {
                var map = new Dictionary<string, string>();
                foreach (var kv in metadata.Columns)
                {
                    string fieldLogicalName = kv.Key;
                    string fieldDisplayName = kv.Value.DisplayName;
                    map[fieldLogicalName] = fieldDisplayName;
                }

                return DisplayNameUtility.MakeUnique(map);                
            }

            public ServiceNowRecordType(string logicalName, ServiceNowTableMetadata metadata, IServiceNowTableResolver resolver)
                : base(CreateDSP(metadata))
            {
                _logicalName = logicalName ?? throw new ArgumentNullException(nameof(logicalName));
                _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
                _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));                
            }

            public override string TableSymbolName => this._logicalName;

            public override IEnumerable<string> FieldNames => _metadata.Columns.Keys;

            public override bool TryGetFieldType(string name, out FormulaType type)
            {               
                if (!_metadata.Columns.TryGetValue(name, out var info))
                {
                    type = null;
                    return false;
                }

                if (info.TableReference != null)
                {                    
                    string referencedTableName = info.TableReference;

                    type = new ServiceNowRecordType(referencedTableName, _metadata, _resolver);
                    return true;
                }

                type = FormulaType.String;
                return true;                
            }

            public override bool Equals(object other)
            {
                return other is ServiceNowRecordType otherRecord && otherRecord._logicalName == this._logicalName;
            }

            public override int GetHashCode()
            {
                throw new NotImplementedException("Don't hash this");
            }
        }

        public class ServiceNowReferenceValue
        {                        
            public string Link { get; set; }
            
            public string Value { get; set; }
        }

        public class ServiceNowTableMetadata
        {
            public Dictionary<string, ColumnEntry> Columns { get; set; }            

            public ServiceNowTableMetadata(Dictionary<string, ColumnEntry> props)
            {
                Columns = props;                
            }            

            public class ColumnEntry
            {
                public string DisplayName { get; set; }
                
                public ColumnType ColumnType { get; set; }
                
                public string TableReference { get; set; }
            }

            // case-insensitive
            [JsonConverter(typeof(JsonStringEnumConverter))]
            public enum ColumnType
            {
                String,
                Reference
            }
        }

        internal interface IServiceNowTableResolver
        {            
            ServiceNowRow GetRowData(ServiceNowReferenceValue refValue);
        }

        public class ServiceNowDataSource : IServiceNowTableResolver
        {            
            internal readonly int _maxDepth;

            public Dictionary<(string, string), string> CachedItems = new Dictionary<(string, string), string>();

            public PowerFxConfig Config
            {
                get
                {
                    if (_config == null)
                    {
                        _config = new PowerFxConfig();
                        _config.EnableJsonFunctions(new JsonSettings() { MaxDepth = _maxDepth });
                    }

                    return _config;
                }

                set => _config = value ?? throw new ArgumentNullException();
            }

            private PowerFxConfig _config;

            public ServiceNowDataSource(int maxDepth = 20)
            {                
                _maxDepth = maxDepth;
            }

            public RecordType GetTableTypeAsync(string tableName) => this.GetTableTypeWorker(tableName);            

            private ServiceNowRecordType GetTableTypeWorker(string tableName) => new ServiceNowRecordType(tableName, GetMetadata(tableName), this);                

            public static ServiceNowTableMetadata GetMetadata(string tableName) 
            {
                return tableName switch
                {
                    "local" => new ServiceNowTableMetadata(new Dictionary<string, ColumnEntry>()
                    {
                        { "logical1", new ColumnEntry() { DisplayName = "Display 1", ColumnType = ColumnType.String } },
                        { "logical2", new ColumnEntry() { DisplayName = "Display 2", ColumnType = ColumnType.String } },
                        { "ref", new ColumnEntry() { DisplayName = "Reference", ColumnType = ColumnType.Reference, TableReference = "ref0" } }
                    }),                    

                    _ => Fail<ServiceNowTableMetadata>($"Unknown tableName {tableName}")
                };
            }

            private static T Fail<T>(string message)
                where T : class
            {
                Assert.Fail(message);
                return null;
            }            

            ServiceNowRow IServiceNowTableResolver.GetRowData(ServiceNowReferenceValue refValue)
            {                
                if (CachedItems.TryGetValue((refValue.Link, refValue.Value), out string json))
                {
                    JsonObject jo = JsonSerializer.Deserialize<JsonObject>(json);

                    return new ServiceNowRow(jo);
                }

                Assert.Fail("Cannot find cached item");

                // Just to make the compiler happy
                return null;
            }
        }
    }
}
