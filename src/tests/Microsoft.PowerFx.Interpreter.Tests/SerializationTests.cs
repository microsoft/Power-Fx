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
            
            var optionSetDisplayNameProvider = DisplayNameUtility.MakeUnique(new Dictionary<string, string>
            {
                { "1", "One" },
                { "2", "Two" },
                { "0", "Zero" },
                { "4", "Four" },
            });

            engine.Config.AddOptionSet(new MyOptionSet("MyOptionSet", optionSetDisplayNameProvider));

            var optionSetValueType = new OptionSetValueType(new MyOptionSet("MyOptionSet", optionSetDisplayNameProvider));
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

        private class BooleanOptionSet : OptionSet, IExternalOptionSet
        {
            public BooleanOptionSet(string name, DisplayNameProvider displayNameProvider)
                : base(name, displayNameProvider)
            {
            }

            bool IExternalOptionSet.IsBooleanValued => true;
        }

        private class MyOptionSet : OptionSet, IExternalOptionSet
        {
            public MyOptionSet(string name, DisplayNameProvider displayNameProvider)
                : base(name, displayNameProvider)
            {
            }
        }
    }
}
