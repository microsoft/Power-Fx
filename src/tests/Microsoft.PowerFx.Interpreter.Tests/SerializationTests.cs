// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Tests;
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

            var boolOptionSetDisplayNameProvider = DisplayNameUtility.MakeUnique(new Dictionary<string, string>
            {
                { "0", "Negative" },
                { "1", "Positive" },
            });

            engine.Config.AddOptionSet(new BooleanOptionSet("BoolOptionSet", boolOptionSetDisplayNameProvider));

            var optionSetValueType = new OptionSetValueType(new BooleanOptionSet("BoolOptionSet", boolOptionSetDisplayNameProvider));
            var optionSetValuePositive = new OptionSetValue("Positive", optionSetValueType);

            var optionSetDefaultExpressionValue = optionSetValueType.DefaultExpressionValue();

            Assert.Equal("BoolOptionSet.Negative", optionSetDefaultExpressionValue);

            var expr = $"If({optionSetDefaultExpressionValue}, 0, {optionSetValuePositive.ToExpression()}, 1, 2)";

            var check = engine.Check(expr);
            Assert.True(check.IsSuccess);

            var result = check.GetEvaluator().Eval() as NumberValue;

            Assert.Equal(1, result.Value);
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

        private class BooleanOptionSet : OptionSet, IExternalOptionSet
        {
            public BooleanOptionSet(string name, DisplayNameProvider displayNameProvider)
                : base(name, displayNameProvider)
            {
            }

            bool IExternalOptionSet.IsBooleanValued => true;
        }
    }
}
