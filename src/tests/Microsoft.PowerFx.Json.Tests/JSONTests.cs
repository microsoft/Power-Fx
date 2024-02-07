// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.PowerFx.Core.Tests;
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
        public void Json_IncludeBinaryData_WithLazyRecord()
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
        public void Json_IncludeBinaryData_WithLazyRecordAndFeature()
        {
            var config = new PowerFxConfig(Features.PowerFxV1);
            config.EnableJsonFunctions();

            var engine = new RecalcEngine(config);

            var record = RecordType.Empty();
            record = record.Add("Property", new LazyRecordType());

            var formulaParams = RecordType.Empty();
            formulaParams = formulaParams.Add("Var", record);

            var result = engine.Check("JSON(Var)", formulaParams);

            Assert.True(result.IsSuccess);
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

            public override bool Equals(object other)
            {
                return true;
            }

            public override int GetHashCode()
            {
                return 1;
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
