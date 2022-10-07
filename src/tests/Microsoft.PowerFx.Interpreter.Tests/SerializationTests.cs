// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.PowerFx.Core.Tests;
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

            foreach (var dt in new[] { DateTime.Now, DateTime.UtcNow, DateTime.Parse("10/10/2022") })
            {
                if (dt.Kind == DateTimeKind.Utc)
                {
                    // UTC is not allowed
                    Assert.Throws<ArgumentException>(() => FormulaValue.New(DateTime.UtcNow));
                }
                else
                {
                    var dateTimeValue = FormulaValue.New(dt);
                    var dateTimeValueDeserialized = (DateTimeValue)engine.Eval(dateTimeValue.ToExpression());

                    Assert.Equal(dateTimeValue.Value, dateTimeValueDeserialized.Value);
                }
            }
        }

        [Fact]
        public void TempTests()
        {
            var engine = new RecalcEngine(new PowerFxConfig());

            var expression = "If(true,";

            var ptBR_opt = new ParserOptions() { Culture = new CultureInfo("pt-BR") };
            var frFR_opt = new ParserOptions() { Culture = new CultureInfo("fr-FR") };
            var enGB_opt = new ParserOptions() { Culture = new CultureInfo("en-GB") };

            var ptBR_check = engine.Check(expression, options: ptBR_opt);
            var frFR_check = engine.Check(expression, options: frFR_opt);
            var enGB_check = engine.Check(expression, options: enGB_opt);

            Assert.True(ptBR_check.IsSuccess);
        }
    }
}
