// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
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

        [Fact]
        public void ColorSerializationTests()
        {
            var engine = new RecalcEngine(new PowerFxConfig());
            var originalCulture = CultureInfo.CurrentCulture;

            try
            {
                // Verify invariant serialization even under cultures with comma decimal separators
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

                var color = Color.FromArgb(128, 255, 0, 0);
                var colorValue = FormulaValue.New(color);
                var expr = colorValue.ToExpression();

                Assert.Equal("RGBA(255,0,0,0.502)", expr);

                var colorValueDeserialized = (ColorValue)engine.Eval(expr, options: new ParserOptions { Culture = CultureInfo.InvariantCulture });
                Assert.Equal(color.R, colorValueDeserialized.Value.R);
                Assert.Equal(color.G, colorValueDeserialized.Value.G);
                Assert.Equal(color.B, colorValueDeserialized.Value.B);
                Assert.True(Math.Abs(color.A - colorValueDeserialized.Value.A) <= 1);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
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
        public void OptionSetToExpressionWithDotInEntityNameTest()
        {
            var engine = new RecalcEngine(new PowerFxConfig());

            var optionSetDisplayNameProvider = DisplayNameUtility.MakeUnique(new Dictionary<string, string>
            {
                { "0", "Zero" },
                { "1", "One" },
            });

            // Entity name contains a dot - requires escaping in expressions
            var optionSet = new OptionSet("My.OptionSet", optionSetDisplayNameProvider);
            engine.Config.AddOptionSet(optionSet);

            Assert.True(optionSet.FormulaType.TryGetValue("0", out var optionSetValue));
            var expression = optionSetValue.ToExpression();

            Assert.Equal("'My.OptionSet'.'0'", expression);

            var check = engine.Check(expression);
            Assert.True(check.IsSuccess);

            var result = check.GetEvaluator().Eval();
            var resultOptionSetValue = Assert.IsType<OptionSetValue>(result);
            Assert.Equal(optionSetValue.Option, resultOptionSetValue.Option);
            Assert.Equal(optionSetValue.Type.OptionSetName, resultOptionSetValue.Type.OptionSetName);
        }

        [Fact]
        public void OptionSetToExpressionWithSpaceInEntityNameTest()
        {
            var engine = new RecalcEngine(new PowerFxConfig());

            var optionSetDisplayNameProvider = DisplayNameUtility.MakeUnique(new Dictionary<string, string>
            {
                { "0", "Zero" },
                { "1", "One" },
            });

            // Entity name contains a space - requires escaping in expressions
            var optionSet = new OptionSet("My OptionSet", optionSetDisplayNameProvider);
            engine.Config.AddOptionSet(optionSet);

            Assert.True(optionSet.FormulaType.TryGetValue("0", out var optionSetValue));
            var expression = optionSetValue.ToExpression();

            Assert.Equal("'My OptionSet'.'0'", expression);

            var check = engine.Check(expression);
            Assert.True(check.IsSuccess);

            var result = check.GetEvaluator().Eval();
            var resultOptionSetValue = Assert.IsType<OptionSetValue>(result);
            Assert.Equal(optionSetValue.Option, resultOptionSetValue.Option);
            Assert.Equal(optionSetValue.Type.OptionSetName, resultOptionSetValue.Type.OptionSetName);
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
