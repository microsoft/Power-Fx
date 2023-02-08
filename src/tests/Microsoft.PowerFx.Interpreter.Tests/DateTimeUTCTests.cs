// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class DateTimeUTCTests
    {
        private readonly RecalcEngine _engine;
        private readonly TimeZoneInfo _istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        private readonly SymbolTable _symbolTable;
        private readonly SymbolValues _symbolValues;
        private readonly RuntimeConfig _utcSymbol;
        private readonly RuntimeConfig _localSymbol;
        private readonly DateTime _utcNow = DateTime.UtcNow;
        private readonly DateTime _istNow;

        public DateTimeUTCTests() 
        {
            _engine = new RecalcEngine(
               new PowerFxConfig(System.Globalization.CultureInfo.InvariantCulture));
            _symbolTable = new SymbolTable();
            _symbolValues = new SymbolValues(_symbolTable);

            _utcSymbol = new RuntimeConfig(_symbolValues);
            _utcSymbol.SetTimeZone(TimeZoneInfo.Utc);

            _localSymbol = new RuntimeConfig(_symbolValues);
            _localSymbol.SetTimeZone(_istTimeZone);

            _istNow = TimeZoneInfo.ConvertTimeFromUtc(_utcNow, _istTimeZone);
        }

        [Fact]
        public async void NowTest()
        {
            // Test Now()
            var evaluator = _engine.Check("Now()", null, _symbolTable).GetEvaluator();
            var result = await evaluator.EvalAsync(default, _localSymbol);
            Assert.Equal(_istNow.ToString("g"), ((DateTimeValue)result).GetConvertedValue(null).ToString("g"));

            // Test Now() again, but with SetTimeZone(Utc)
            var utcResult = await evaluator.EvalAsync(default, _utcSymbol);
            Assert.Equal(_utcNow.ToString("g"), ((DateTimeValue)utcResult).GetConvertedValue(null).ToString("g"));
        }

        [Fact]
        public async void DateTimeFunctionTest()
        {
            var currentTimeZoneKind = TimeZoneInfo.Local.BaseUtcOffset == TimeSpan.Zero ?
                DateTimeKind.Utc :
                DateTimeKind.Unspecified;

            var localDateTimeExpected = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var utcDateTimeExpected = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            
            // DateTime function tests
            var evaluator = _engine.Check("DateTime(2023,1,1,0,0,0)", null, _symbolTable).GetEvaluator();
            var result = await evaluator.EvalAsync(default, _localSymbol);
            Assert.Equal(localDateTimeExpected.ToString("o"), ((DateTimeValue)result).GetConvertedValue(_istTimeZone).ToString("o"));

            var utcResult = await evaluator.EvalAsync(default, _utcSymbol);
            Assert.Equal(utcDateTimeExpected.ToString("o"), ((DateTimeValue)utcResult).GetConvertedValue(null).ToString("o"));
            
            Assert.NotEqual(((DateTimeValue)result).GetConvertedValue(_istTimeZone).ToString("o"), ((DateTimeValue)utcResult).GetConvertedValue(null).ToString("o"));

            // Date function tests.
            evaluator = _engine.Check("Date(2023,1,1)", null, _symbolTable).GetEvaluator();
            result = await evaluator.EvalAsync(default, _localSymbol);
            Assert.Equal(localDateTimeExpected.ToString("o"), ((DateValue)result).GetConvertedValue(_istTimeZone).ToString("o"));

            utcResult = await evaluator.EvalAsync(default, _utcSymbol);
            Assert.Equal(utcDateTimeExpected.ToString("o"), ((DateValue)utcResult).GetConvertedValue(null).ToString("o"));

            Assert.NotEqual(((DateValue)result).GetConvertedValue(_istTimeZone).ToString("o"), ((DateValue)utcResult).GetConvertedValue(null).ToString("o"));
        }

        [Fact]
        public async void IsTodayTest()
        {
            // this is today's IST today's date 23:30:00, so when converted to IST this will change to next day. 
            var myDateTimeUTC = new DateTime(_istNow.Year, _istNow.Month, _istNow.Day, 23, 30, 0, DateTimeKind.Utc);

            // this is today's IST today's date 23:30:00, so when converted to IST this will remain same. 
            var myDateTimeIST = new DateTime(_istNow.Year, _istNow.Month, _istNow.Day, 23, 30, 0, DateTimeKind.Unspecified);
            
            var myDateTimeUTCToday = new DateTime(_utcNow.Year, _utcNow.Month, _utcNow.Day, 23, 30, 0, DateTimeKind.Utc);

            var myDateTimeUTCSlot = _symbolTable.AddVariable("myDateTimeUTC", FormulaType.DateTime);
            _symbolValues.Set(myDateTimeUTCSlot, FormulaValue.New(myDateTimeUTC));
            
            var myDateTimeISTSlot = _symbolTable.AddVariable("myDateTimeIST", FormulaType.DateTime);
            _symbolValues.Set(myDateTimeISTSlot, FormulaValue.New(myDateTimeIST));

            var myDateTimeUTCTodaySlot = _symbolTable.AddVariable("myDateTimeUTCToday", FormulaType.DateTime);
            _symbolValues.Set(myDateTimeUTCTodaySlot, FormulaValue.New(myDateTimeUTCToday));

            var evaluator = _engine.Check("IsToday(myDateTimeUTC)", null, _symbolTable).GetEvaluator();
            var result = await evaluator.EvalAsync(default, _localSymbol);
            Assert.False(((BooleanValue)result).Value);

            evaluator = _engine.Check("IsToday(myDateTimeIST)", null, _symbolTable).GetEvaluator();
            result = await evaluator.EvalAsync(default, _localSymbol);
            Assert.True(((BooleanValue)result).Value);

            evaluator = _engine.Check("IsToday(myDateTimeUTCToday)", null, _symbolTable).GetEvaluator();
            var utcresult = await evaluator.EvalAsync(default, _utcSymbol);
            Assert.True(((BooleanValue)utcresult).Value);
        }

        [Fact]
        public async void DateDiffTest()
        {
            var nonUTCDateTimeSlot = _symbolTable.AddVariable("nonUTCDateTime", FormulaType.DateTime);
            _symbolValues.Set(nonUTCDateTimeSlot, FormulaValue.New(new DateTime(2023, 1, 30)));

            var utcDateTimeSlot = _symbolTable.AddVariable("utcDateTime", FormulaType.DateTime);
            _symbolValues.Set(utcDateTimeSlot, FormulaValue.New(new DateTime(2023, 1, 30, 23, 30, 0, DateTimeKind.Utc)));

            // DateDiff between IST nonUTCDateTime => DateTime(1,30,2023)
            // utcDateTime => DateTime(1,30,2023)

            // In TZ Indian standard time (GMT+5:30) DateDiff(DateTime(2023,1,30,0,0,0), DateTime(2023,1,31,4,30,0)) = 1
            var evaluator = _engine.Check("DateDiff(nonUTCDateTime, utcDateTime)", options: null, _symbolTable).GetEvaluator();
            var result = await evaluator.EvalAsync(default, _localSymbol);
            Assert.Equal(1, ((NumberValue)result).Value);

            // In TZ UTC DateDiff(DateTime(2023,1,30,0,0,0), DateTime(2023,1,30,23,30,0)) = 0
            var utcResult = await evaluator.EvalAsync(default, _utcSymbol);
            Assert.Equal(0, ((NumberValue)utcResult).Value);
        }

        [Fact]
        public async void TodayTest()
        {
            var utcToday = DateTime.UtcNow;
            var istToday = TimeZoneInfo.ConvertTime(utcToday, _istTimeZone).Date;
            utcToday = utcToday.Date;
            
            var evaluator = _engine.Check("Today()", options: null, _symbolTable).GetEvaluator();
            var result = await evaluator.EvalAsync(default, _localSymbol);
            Assert.Equal(istToday, ((DateValue)result).GetConvertedValue(_istTimeZone));

            var utcResult = await evaluator.EvalAsync(default, _utcSymbol);
            Assert.Equal(utcToday, ((DateValue)utcResult).GetConvertedValue(TimeZoneInfo.Utc));
        }

        [Fact]
        public void GetConvertedDateTimeValueTest()
        {
            var result = DateTimeValue.GetConvertedDateTimeValue(_utcNow, _istTimeZone);
            Assert.Equal(_istNow, result);

            result = DateTimeValue.GetConvertedDateTimeValue(_istNow, _istTimeZone);
            Assert.Equal(_istNow, result);

            result = DateTimeValue.GetConvertedDateTimeValue(_utcNow, TimeZoneInfo.Utc);
            Assert.Equal(_utcNow, result);

            result = DateTimeValue.GetConvertedDateTimeValue(_istNow, TimeZoneInfo.Utc);
            Assert.Equal(DateTime.SpecifyKind(_istNow, DateTimeKind.Utc), result);

            var localtime = DateTime.SpecifyKind(_istNow, DateTimeKind.Local);
            
            result = DateTimeValue.GetConvertedDateTimeValue(localtime, _istTimeZone);
            Assert.Equal(_istNow, result);

            result = DateTimeValue.GetConvertedDateTimeValue(localtime, TimeZoneInfo.Utc);
            Assert.Equal(DateTime.SpecifyKind(_istNow, DateTimeKind.Utc), result);
        }

        [Fact]
        public async Task DateTimeValueTests()
        {
            _engine.Config.AddFunction(new ToUntypedObjectFunction());

            var expectedUTC = new DateTime(2022, 11, 21, 12, 13, 30, DateTimeKind.Utc);

            var expectedLocal = TimeZoneInfo.ConvertTimeFromUtc(expectedUTC, _istTimeZone);

            // UTC string
            var evaluator = _engine.Check(@"DateTimeValue(""2022-11-21T12:13:30Z"")", options: null, _symbolTable).GetEvaluator();
            var result = await evaluator.EvalAsync(default, _localSymbol);
            Assert.Equal(expectedLocal, ((DateTimeValue)result).GetConvertedValue(_istTimeZone));

            var utcResult = await evaluator.EvalAsync(default, _utcSymbol);
            Assert.Equal(expectedUTC, ((DateTimeValue)utcResult).GetConvertedValue(TimeZoneInfo.Utc));

            // Local string
            evaluator = _engine.Check(@"DateTimeValue(""2022-11-21T17:43:30.0000000+05:30"")", options: null, _symbolTable).GetEvaluator();
            result = await evaluator.EvalAsync(default, _localSymbol);
            Assert.Equal(expectedLocal, ((DateTimeValue)result).GetConvertedValue(_istTimeZone));

            utcResult = await evaluator.EvalAsync(default, _utcSymbol);
            Assert.Equal(expectedUTC, ((DateTimeValue)utcResult).GetConvertedValue(TimeZoneInfo.Utc));

            // UnSpecified string
            evaluator = _engine.Check(@"DateTimeValue(""2022-11-21T17:43:30.0000000"")", options: null, _symbolTable).GetEvaluator();
            result = await evaluator.EvalAsync(default, _localSymbol);
            Assert.Equal(expectedLocal, ((DateTimeValue)result).GetConvertedValue(_istTimeZone));

            utcResult = await evaluator.EvalAsync(default, _utcSymbol);
            Assert.Equal(DateTime.SpecifyKind(expectedLocal, DateTimeKind.Utc), ((DateTimeValue)utcResult).GetConvertedValue(TimeZoneInfo.Utc));

            // With UntypedObject
            // UTC string
            evaluator = _engine.Check(@"DateTimeValue(ToUntypedObject(""2022-11-21T12:13:30Z""))", options: null, _symbolTable).GetEvaluator();
            result = await evaluator.EvalAsync(default, _localSymbol);
            Assert.Equal(expectedLocal, ((DateTimeValue)result).GetConvertedValue(_istTimeZone));
            
            utcResult = await evaluator.EvalAsync(default, _utcSymbol);
            Assert.Equal(expectedUTC, ((DateTimeValue)utcResult).GetConvertedValue(TimeZoneInfo.Utc));

            // UnSpecified string
            evaluator = _engine.Check(@"DateTimeValue(ToUntypedObject(""2022-11-21T17:43:30.0000000""))", options: null, _symbolTable).GetEvaluator();
            result = await evaluator.EvalAsync(default, _localSymbol);
            Assert.Equal(expectedLocal, ((DateTimeValue)result).GetConvertedValue(_istTimeZone));

            utcResult = await evaluator.EvalAsync(default, _utcSymbol);
            Assert.Equal(DateTime.SpecifyKind(expectedLocal, DateTimeKind.Utc), ((DateTimeValue)utcResult).GetConvertedValue(TimeZoneInfo.Utc));
        }

        [Fact]
        public async Task DateValueTests()
        {
            _engine.Config.AddFunction(new ToUntypedObjectFunction());

            var expectedUTC = new DateTime(2022, 11, 21, 22, 13, 30, DateTimeKind.Utc);

            var expectedLocal = TimeZoneInfo.ConvertTimeFromUtc(expectedUTC, _istTimeZone).Date;

            expectedUTC = expectedUTC.Date;

            // UTC string
            var evaluator = _engine.Check(@"DateValue(""2022-11-21T22:13:30Z"")", options: null, _symbolTable).GetEvaluator();
            var result = await evaluator.EvalAsync(default, _localSymbol);
            Assert.Equal(expectedLocal, ((DateValue)result).GetConvertedValue(_istTimeZone));

            var utcResult = await evaluator.EvalAsync(default, _utcSymbol);
            Assert.Equal(expectedUTC, ((DateValue)utcResult).GetConvertedValue(TimeZoneInfo.Utc));

            // Local string
            evaluator = _engine.Check(@"DateValue(""2022-11-22T03:43:30.0000000+05:30"")", options: null, _symbolTable).GetEvaluator();
            result = await evaluator.EvalAsync(default, _localSymbol);
            Assert.Equal(expectedLocal, ((DateValue)result).GetConvertedValue(_istTimeZone));

            utcResult = await evaluator.EvalAsync(default, _utcSymbol);
            Assert.Equal(expectedUTC, ((DateValue)utcResult).GetConvertedValue(TimeZoneInfo.Utc));

            // UnSpecified string
            evaluator = _engine.Check(@"DateValue(""2022-11-22T03:43:30.0000000"")", options: null, _symbolTable).GetEvaluator();
            result = await evaluator.EvalAsync(default, _localSymbol);
            Assert.Equal(expectedLocal, ((DateValue)result).GetConvertedValue(_istTimeZone));

            utcResult = await evaluator.EvalAsync(default, _utcSymbol);
            Assert.Equal(DateTime.SpecifyKind(expectedLocal, DateTimeKind.Utc), ((DateValue)utcResult).GetConvertedValue(TimeZoneInfo.Utc));

            // With UntypedObject
            // UTC string
            evaluator = _engine.Check(@"DateValue(ToUntypedObject(""2022-11-21T22:13:30Z""))", options: null, _symbolTable).GetEvaluator();
            result = await evaluator.EvalAsync(default, _localSymbol);
            Assert.Equal(expectedLocal, ((DateValue)result).GetConvertedValue(_istTimeZone));

            utcResult = await evaluator.EvalAsync(default, _utcSymbol);
            Assert.Equal(expectedUTC, ((DateValue)utcResult).GetConvertedValue(TimeZoneInfo.Utc));

            // UnSpecified string
            evaluator = _engine.Check(@"DateValue(ToUntypedObject(""2022-11-22T03:43:30.0000000""))", options: null, _symbolTable).GetEvaluator();
            result = await evaluator.EvalAsync(default, _localSymbol);
            Assert.Equal(expectedLocal, ((DateValue)result).GetConvertedValue(_istTimeZone));

            utcResult = await evaluator.EvalAsync(default, _utcSymbol);
            Assert.Equal(DateTime.SpecifyKind(expectedLocal, DateTimeKind.Utc), ((DateValue)utcResult).GetConvertedValue(TimeZoneInfo.Utc));
        }

        private class ToUntypedObjectFunction : ReflectionFunction
        {
            public ToUntypedObjectFunction()
                : base("ToUntypedObject", FormulaType.UntypedObject, FormulaType.String)
            {
            }

            public FormulaValue Execute(StringValue val)
            {
                return FormulaValue.New(new SimpleObject(val));
            }
        }

        private class SimpleObject : IUntypedObject
        {
            private readonly FormulaValue _value;

            public SimpleObject(FormulaValue value)
            {
                _value = value;
            }

            public IUntypedObject this[int index] => throw new NotImplementedException();

            public FormulaType Type => _value.Type;

            public int GetArrayLength()
            {
                throw new NotImplementedException();
            }

            public bool GetBoolean()
            {
                return ((BooleanValue)_value).Value;
            }

            public double GetDouble()
            {
                return ((NumberValue)_value).Value;
            }

            public string GetString()
            {
                return ((StringValue)_value).Value;
            }

            public bool TryGetProperty(string value, out IUntypedObject result)
            {
                throw new NotImplementedException();
            }
        }
    }
}
