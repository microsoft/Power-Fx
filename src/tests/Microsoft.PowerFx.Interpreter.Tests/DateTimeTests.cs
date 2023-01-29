// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class DateTimeTests
    {
        private static readonly bool IsLocalUTC = TimeZoneInfo.Local.BaseUtcOffset == TimeSpan.Zero;

        [Fact]
        public async void DateTimeUTCTests()
        {
            var engine = new RecalcEngine(
               new PowerFxConfig(System.Globalization.CultureInfo.InvariantCulture));
            var utcNow = DateTime.UtcNow;
            var localNow = utcNow.ToLocalTime();

            // myDateTime variable has system's current local time.
            var symbolTable = new SymbolTable();
            var symbolValues = new SymbolValues(symbolTable);
            var myDateTimeSlot = symbolTable.AddVariable("myDateTime", FormulaType.DateTime);
            symbolValues.Set(myDateTimeSlot, FormulaValue.New(localNow));
            
            // Creates Today's expression such that IsToday() can be true for local
            // and false for UTC
            var todayLocalDateTimeSlot = symbolTable.AddVariable("todayLocalDateTime", FormulaType.DateTime);
            var localToday = DateTime.Today;
            if (TimeZoneInfo.Local.BaseUtcOffset > TimeSpan.Zero)
            {
                symbolValues.Set(todayLocalDateTimeSlot, FormulaValue.New(new DateTime(localNow.Year, localNow.Month, localNow.Day, 0, 30, 0)));
            }
            else if (TimeZoneInfo.Local.BaseUtcOffset < TimeSpan.Zero)
            {
                symbolValues.Set(todayLocalDateTimeSlot, FormulaValue.New(new DateTime(localNow.Year, localNow.Month, localNow.Day, 23, 30, 0)));
            }

            var utcSymbol = new RuntimeConfig(symbolValues);
            utcSymbol.SetTimeZone(TimeZoneInfo.Utc);

            var localSymbol = new RuntimeConfig(symbolValues);

            // Test Local now as a variable
            var evaluator = engine.Check("myDateTime", null, symbolTable).GetEvaluator();           
            var result = await evaluator.EvalAsync(default, localSymbol);
            Assert.Equal(localNow.ToString("g"), ((DateTimeValue)result).Value.ToString("g"));
            
            // Test again, but with SetTimeZone(Utc)
            var utcResult = await evaluator.EvalAsync(default, utcSymbol);
            Assert.Equal(utcNow.ToString("g"), ((DateTimeValue)utcResult).Value.ToString("g"));
           
            // Test Now()
            evaluator = engine.Check("Now()", null, symbolTable).GetEvaluator();
            result = await evaluator.EvalAsync(default, localSymbol);
            Assert.Equal(localNow.ToString("g"), ((DateTimeValue)result).Value.ToString("g"));
         
            // Test Now() again, but with SetTimeZone(Utc)
            utcResult = await evaluator.EvalAsync(default, utcSymbol);
            Assert.Equal(utcNow.ToString("g"), ((DateTimeValue)utcResult).Value.ToString("g"));

            // Test DateTime function
            evaluator = engine.Check("DateTime(2022,11,17,1,0,0)", options: null, symbolTable).GetEvaluator();
            result = await evaluator.EvalAsync(default, localSymbol);
            Assert.Equal(DateTimeKind.Unspecified, ((DateTimeValue)result).Value.Kind);

            // Test DateTime function, but with SetTimeZone(Utc)
            utcResult = await evaluator.EvalAsync(default, utcSymbol);
            Assert.Equal(((DateTimeValue)result).Value.ToString("g"), ((DateTimeValue)utcResult).Value.ToString("g"));
            Assert.Equal(DateTimeKind.Utc, ((DateTimeValue)utcResult).Value.Kind);

            // Test Date function
            evaluator = engine.Check("Date(2022,11,17)", options: null, symbolTable).GetEvaluator();
            result = await evaluator.EvalAsync(default, localSymbol);
            Assert.Equal(DateTimeKind.Unspecified, ((DateValue)result).Value.Kind);

            // Test Date function, but with SetTimeZone(Utc)
            utcResult = await evaluator.EvalAsync(default, utcSymbol);
            Assert.Equal(((DateValue)result).Value.ToString("g"), ((DateValue)utcResult).Value.ToString("g"));
            Assert.Equal(DateTimeKind.Utc, ((DateValue)utcResult).Value.Kind);

            // Test IsToday()
            string isTodayExpression = "IsToday(todayLocalDateTime)";
            evaluator = engine.Check(isTodayExpression, options: null, symbolTable).GetEvaluator();
            result = await evaluator.EvalAsync(default, localSymbol);
            Assert.True(((BooleanValue)result).Value);

            // Test IsToday(), but with SetTimeZone(Utc)
            utcResult = await evaluator.EvalAsync(default, utcSymbol);
            Assert.True(((BooleanValue)utcResult).Value);
        }
    }
}
