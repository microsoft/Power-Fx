﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class SerializationTests : PowerFxTest
    {
        [Fact]
        public void DateTimeSerializationTests()
        {
            var engine = new RecalcEngine(new PowerFxConfig());

            DateTime[] dateTimeArray = new[]
            {
                WithoutSubMilliseconds(DateTime.Now),
                WithoutSubMilliseconds(DateTime.UtcNow),
                DateTime.Parse("10/10/2022")
            };

            foreach (var dt in dateTimeArray)
            {
                var dateTimeValue = FormulaValue.New(dt);
                var dateTimeValueDeserialized = (DateTimeValue)engine.Eval(dateTimeValue.ToExpression());

                Assert.Equal(dateTimeValue.GetConvertedValue(null), dateTimeValueDeserialized.GetConvertedValue(null));
            }
        }

        /// <summary>
        /// This is necessary due to the fact that serialization ignores tick precision.
        /// https://github.com/microsoft/Power-Fx/issues/849.
        /// </summary>
        private static DateTime WithoutSubMilliseconds(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond, dt.Kind);
        }

        [Fact]
        public void OptionSetDefaultExpressionValueTests()
        {
            var engine = new RecalcEngine(new PowerFxConfig());
            var symbol = new SymbolTable();
            
            var optionSetDisplayNameProvider = DisplayNameUtility.MakeUnique(new Dictionary<string, string>
            {
                { "1", "One" },
                { "2", "Two" },
                { "0", "Zero" },
                { "4", "Four" },
            });

            var optionSet = new OptionSet("MyOptionSet", optionSetDisplayNameProvider);

            engine.Config.AddOptionSet(optionSet);

            var optionSetValueType = new OptionSetValueType(optionSet);
            var optionSetDefaultExpressionValue = optionSetValueType.DefaultExpressionValue();

            Assert.Equal("MyOptionSet.Zero", optionSetDefaultExpressionValue);

            var check = engine.Check(optionSetDefaultExpressionValue);
            Assert.True(check.IsSuccess);

            var result = check.GetEvaluator().Eval();

            Assert.IsType<OptionSetValue>(result);
        }

        [Fact]
        public void OptionSetDefaultExpressionValueErrorTests()
        {
            var engine = new RecalcEngine(new PowerFxConfig());
            var symbol = new SymbolTable();

            // Option set with zero options
            var boolOptionSetDisplayNameProvider = DisplayNameUtility.MakeUnique(new Dictionary<string, string>());

            engine.Config.AddOptionSet(new BooleanOptionSet("BoolOptionSet", boolOptionSetDisplayNameProvider));

            var optionSetValueType = new OptionSetValueType(new BooleanOptionSet("BoolOptionSet", boolOptionSetDisplayNameProvider));

            var optionSetDefaultExpressionValue = optionSetValueType.DefaultExpressionValue();

            var check = engine.Check(optionSetDefaultExpressionValue);
            Assert.True(check.IsSuccess);

            var result = check.GetEvaluator().Eval();

            Assert.IsType<ErrorValue>(result);
        }

        [Fact]
        public void RecordValueKeyworkSerializationTest()
        {
            var engine = new RecalcEngine(new PowerFxConfig());
            var symbol = new SymbolTable();

            // Combining both keywords and reserved keywords
            string[] keywords = new string[] { "null", "empty", "none", "nothing", "undefined", "This", "Is", "Child", "Children", "Siblings", "true", "false", "in", "exactin", "Self", "Parent", "And", "Or", "Not", "As" };

            foreach (var keyword in keywords)
            {
                var expectedRecord = $"{{{keyword.ToUpperInvariant()}:Decimal(0),'{keyword}':Decimal(0)}}";
                var expectedTable = $"Table({{{keyword.ToUpperInvariant()}:Decimal(0),'{keyword}':Decimal(0)}})";

                var fields = new List<NamedValue>()
                {
                    new NamedValue(keyword, FormulaValue.New(0)),
                    new NamedValue(keyword.ToUpperInvariant(), FormulaValue.New(0))
                };

                var record = FormulaValue.NewRecordFromFields(fields);
                var table = FormulaValue.NewTable(record.Type, record);

                var recordSerialized = record.ToExpression();
                var tableSerialized = table.ToExpression();

                Assert.Equal(expectedRecord, recordSerialized);
                Assert.Equal(expectedTable, tableSerialized);

                var checkRecord = engine.Check(recordSerialized);
                var checkTable = engine.Check(tableSerialized);

                Assert.True(checkRecord.IsSuccess);                
                Assert.True(checkTable.IsSuccess);
            }
        }

        internal class BooleanOptionSet : OptionSet, IExternalOptionSet
        {            
            public BooleanOptionSet(string name, DisplayNameProvider displayNameProvider)
                : base(name, displayNameProvider)
            {
            }

            DKind IExternalOptionSet.BackingKind => DKind.Boolean;

            public DType Type => DType.CreateOptionSetType(this);

            public OptionSetValueType OptionSetValueType => new OptionSetValueType(this);

            public new bool TryGetValue(DName fieldName, out OptionSetValue optionSetValue)
            {
                if (fieldName.Value == "0" || fieldName.Value == "1")
                {
                    optionSetValue = new OptionSetValue(fieldName.Value, this.OptionSetValueType, fieldName.Value == "1");
                    return true;
                }

                optionSetValue = null;
                return false;
            }
        }
    }
}
