// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    // Direct ports from JScript. 
    internal static partial class Library
    {
        private static readonly IRandomService _defaultRandService = new DefaultRandomService();

        // Support for aggregators. Helpers to ensure that Scalar and Tabular behave the same.
        private interface IAggregator
        {
            void Apply(FormulaValue value);

            FormulaValue GetResult(IRContext irContext);
        }

        private class SumAgg : IAggregator
        {
            protected int _count;
            protected double _accumulator;

            public void Apply(FormulaValue value)
            {
                if (value is BlankValue)
                {
                    return;
                }

                var n1 = (NumberValue)value;

                _accumulator += n1.Value;
                _count++;
            }

            public virtual FormulaValue NoElementValue(IRContext context)
            {
                return GetDefault(context);
            }

            public FormulaValue GetDefault(IRContext context)
            {
                return new BlankValue(context);
            }

            public virtual FormulaValue GetResult(IRContext irContext)
            {
                if (_count == 0)
                {
                    return GetDefault(irContext);
                }

                if (double.IsInfinity(_accumulator))
                {
                    return CommonErrors.OverflowError(irContext);
                }

                return new NumberValue(irContext, _accumulator);
            }
        }

        private class SumDecimalAgg : IAggregator
        {
            protected int _count;
            protected decimal _accumulator;
            protected bool _overflow;

            public void Apply(FormulaValue value)
            {
                if (value is BlankValue)
                {
                    return;
                }

                var n1 = (DecimalValue)value;

                try
                {
                    _accumulator += n1.Value;
                }
                catch (OverflowException)
                {
                    _overflow = true;
                }

                _count++;
            }

            public virtual FormulaValue NoElementValue(IRContext context)
            {
                return GetDefault(context);
            }

            public FormulaValue GetDefault(IRContext context)
            {
                return new BlankValue(context);
            }

            public virtual FormulaValue GetResult(IRContext irContext)
            {
                if (_count == 0)
                {
                    return GetDefault(irContext);
                }

                if (_overflow)
                {
                    return CommonErrors.OverflowError(irContext);
                }

                return new DecimalValue(irContext, _accumulator);
            }
        }

        private class VarianceAgg : IAggregator
        {
            protected int _count;
            protected double _meanAcc;
            protected double _m2Acc;

            // Implementation of Welford's Algorithm:  https://en.wikipedia.org/wiki/Algorithms_for_calculating_variance

            public void Apply(FormulaValue value)
            {
                if (value is BlankValue)
                {
                    return;
                }

                var n1 = (NumberValue)value;

                _count++;
                var delta = n1.Value - _meanAcc;
                _meanAcc += delta / _count;
                var delta2 = n1.Value - _meanAcc;
                _m2Acc += delta * delta2;
            }

            public FormulaValue NoElementValue(IRContext context)
            {
                return GetDefault(context);
            }

            public FormulaValue GetDefault(IRContext context)
            {
                return CommonErrors.DivByZeroError(context);
            }

            public virtual FormulaValue GetResult(IRContext irContext)
            {
                if (_count == 0)
                {
                    return GetDefault(irContext);
                }
                else
                {
                    return new NumberValue(irContext, _m2Acc / _count);
                }
            }
        }

        private class StdDeviationAgg : VarianceAgg
        {
            public override FormulaValue GetResult(IRContext irContext)
            {
                if (_count == 0)
                {
                    return GetDefault(irContext);
                }
                else
                {
                    return new NumberValue(irContext, Math.Sqrt(_m2Acc / _count));
                }
            }
        }

        private class MinNumberAgg : IAggregator
        {
            protected double _minValue = double.MaxValue;
            protected int _count = 0;

            public void Apply(FormulaValue value)
            {
                if (value is BlankValue)
                {
                    return;
                }

                var n1 = ((NumberValue)value).Value;

                if (n1 < _minValue)
                {
                    _minValue = n1;
                }

                _count++;
            }

            public FormulaValue GetResult(IRContext irContext)
            {
                if (_count == 0)
                {
                    return new BlankValue(irContext);
                }

                return new NumberValue(irContext, _minValue);
            }
        }

        private class MinDecimalAgg : IAggregator
        {
            protected decimal _minValue = decimal.MaxValue;
            protected int _count = 0;

            public void Apply(FormulaValue value)
            {
                if (value is BlankValue)
                {
                    return;
                }

                var n1 = ((DecimalValue)value).Value;

                if (n1 < _minValue)
                {
                    _minValue = n1;
                }

                _count++;
            }

            public FormulaValue GetResult(IRContext irContext)
            {
                if (_count == 0)
                {
                    return new BlankValue(irContext);
                }

                return new DecimalValue(irContext, _minValue);
            }
        }

        private class MinDateAndDateTimeAgg : IAggregator
        {
            protected DateTime _minValueDT = DateTime.MaxValue;
            protected int _count = 0;
            private readonly TimeZoneInfo _timeZoneInfo;

            public MinDateAndDateTimeAgg(TimeZoneInfo timeZoneInfo)
            {
                _timeZoneInfo = timeZoneInfo;
            }

            public MinDateAndDateTimeAgg(IServiceProvider serviceProvider)
                : this(serviceProvider.GetService<TimeZoneInfo>())
            {
            }

            public void Apply(FormulaValue value)
            {
                if (value is BlankValue)
                {
                    return;
                }

                DateTime dt = DateTime.MaxValue;

                switch (value)
                {
                    case DateTimeValue dtv:
                        dt = dtv.GetConvertedValue(_timeZoneInfo);
                        break;
                    case DateValue dv:
                        dt = dv.GetConvertedValue(_timeZoneInfo);
                        break;
                }

                if (dt < _minValueDT)
                {
                    _minValueDT = dt;
                }

                _count++;
            }

            public FormulaValue GetResult(IRContext irContext)
            {
                if (_count == 0)
                {
                    return new BlankValue(irContext);
                }

                if (irContext.ResultType == FormulaType.DateTime)
                {
                    return new DateTimeValue(irContext, _minValueDT);
                }
                else
                {
                    return new DateValue(irContext, _minValueDT);
                }
            }
        }

        private class MinTimeAgg : IAggregator
        {
            protected TimeSpan _minValueT = TimeSpan.MaxValue;
            protected int _count = 0;

            public void Apply(FormulaValue value)
            {
                if (value is BlankValue)
                {
                    return;
                }

                var n1 = ((TimeValue)value).Value;

                if (n1 < _minValueT)
                {
                    _minValueT = n1;
                }

                _count++;
            }

            public FormulaValue GetResult(IRContext irContext)
            {
                if (_count == 0)
                {
                    return new BlankValue(irContext);
                }

                return new TimeValue(irContext, _minValueT);
            }
        }

        private class MaxNumberAgg : IAggregator
        {
            protected double _maxValue = double.MinValue;
            protected int _count = 0;

            public void Apply(FormulaValue value)
            {
                if (value is BlankValue)
                {
                    return;
                }

                var n1 = ((NumberValue)value).Value;

                if (n1 > _maxValue)
                {
                    _maxValue = n1;
                }

                _count++;
            }

            public FormulaValue GetResult(IRContext irContext)
            {
                if (_count == 0)
                {
                    return new BlankValue(irContext);
                }

                return new NumberValue(irContext, _maxValue);
            }
        }

        private class MaxDecimalAgg : IAggregator
        {
            protected decimal _maxValue = decimal.MinValue;
            protected int _count = 0;

            public void Apply(FormulaValue value)
            {
                if (value is BlankValue)
                {
                    return;
                }

                var n1 = ((DecimalValue)value).Value;

                if (n1 > _maxValue)
                {
                    _maxValue = n1;
                }

                _count++;
            }

            public FormulaValue GetResult(IRContext irContext)
            {
                if (_count == 0)
                {
                    return new BlankValue(irContext);
                }

                return new DecimalValue(irContext, _maxValue);
            }
        }

        private class MaxDateAndDateTimeAgg : IAggregator
        {
            protected DateTime _maxValueDT = DateTime.MinValue;
            protected int _count = 0;
            private readonly TimeZoneInfo _timeZoneInfo;

            public MaxDateAndDateTimeAgg(TimeZoneInfo timeZoneInfo)
            {
                _timeZoneInfo = timeZoneInfo;
            }

            public MaxDateAndDateTimeAgg(IServiceProvider serviceProvider) 
                : this(serviceProvider.GetService<TimeZoneInfo>())
            {
            }

            public void Apply(FormulaValue value)
            {
                if (value is BlankValue)
                {
                    return;
                }

                DateTime dt = DateTime.MinValue;
                switch (value)
                {
                    case DateTimeValue dtv:
                        dt = dtv.GetConvertedValue(_timeZoneInfo);
                        break;
                    case DateValue dv:
                        dt = dv.GetConvertedValue(_timeZoneInfo);
                        break;
                }

                if (dt > _maxValueDT)
                {
                    _maxValueDT = dt;
                }

                _count++;
            }

            public FormulaValue GetResult(IRContext irContext)
            {
                if (_count == 0)
                {
                    return new BlankValue(irContext);
                }

                if (irContext.ResultType == FormulaType.DateTime)
                {
                    return new DateTimeValue(irContext, _maxValueDT);
                }
                else
                {
                    return new DateValue(irContext, _maxValueDT);
                }
            }
        }

        private class MaxTimeAgg : IAggregator
        {
            protected TimeSpan _maxValueT = TimeSpan.MinValue;
            protected int _count = 0;

            public void Apply(FormulaValue value)
            {
                if (value is BlankValue)
                {
                    return;
                }

                var n1 = ((TimeValue)value).Value;

                if (n1 > _maxValueT)
                {
                    _maxValueT = n1;
                }

                _count++;
            }

            public FormulaValue GetResult(IRContext irContext)
            {
                if (_count == 0)
                {
                    return new BlankValue(irContext);
                }

                return new TimeValue(irContext, _maxValueT);
            }
        }

        private class AverageAgg : SumAgg
        {
            public override FormulaValue NoElementValue(IRContext context)
            {
                return CommonErrors.DivByZeroError(context);
            }

            public override FormulaValue GetResult(IRContext irContext)
            {
                if (_count == 0)
                {
                    return CommonErrors.DivByZeroError(irContext);
                }

                if (double.IsInfinity(_accumulator))
                {
                    return CommonErrors.OverflowError(irContext);
                }

                return new NumberValue(irContext, _accumulator / _count);
            }
        }

        private class AverageDecimalAgg : SumDecimalAgg
        {
            public override FormulaValue NoElementValue(IRContext context)
            {
                return CommonErrors.DivByZeroError(context);
            }

            public override FormulaValue GetResult(IRContext irContext)
            {
                if (_count == 0)
                {
                    return CommonErrors.DivByZeroError(irContext);
                }

                if (_overflow)
                {
                    return CommonErrors.OverflowError(irContext);
                }

                return new DecimalValue(irContext, _accumulator / _count);
            }
        }

        private static FormulaValue RunAggregator(IAggregator agg, IRContext irContext, FormulaValue[] values)
        {
            foreach (var value in values.Where(v => v is not BlankValue))
            {
                agg.Apply(value);
            }

            return agg.GetResult(irContext);
        }

        private static async Task<FormulaValue> RunAggregatorAsync(string functionName, IAggregator agg, EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (TableValue)args.First();
            var arg1 = (LambdaFormulaValue)args.Skip(1).First();

            foreach (var row in arg0.Rows)
            {
                SymbolContext childContext;
                if (row.IsValue)
                {
                    childContext = context.SymbolContext.WithScopeValues(row.Value);
                }
                else if (row.IsError)
                {
                    childContext = context.SymbolContext.WithScopeValues(row.Error);
                }
                else
                {
                    childContext = context.SymbolContext.WithScopeValues(RecordValue.Empty());
                }

                var value = await arg1.EvalInRowScopeAsync(context.NewScope(childContext)).ConfigureAwait(false);

                if (value is ErrorValue error)
                {
                    return error;
                }

                agg.Apply(value);
            }

            return agg.GetResult(irContext);
        }

        private static FormulaValue Sqrt(IRContext irContext, NumberValue[] args)
        {
            var n1 = args[0];
            var result = Math.Sqrt(n1.Value);

            return new NumberValue(irContext, result);
        }

        // Sum(1,2,3)     
        internal static FormulaValue Sum(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            return RunAggregator(irContext.ResultType == FormulaType.Decimal ? new SumDecimalAgg() : new SumAgg(), irContext, args);
        }

        // Sum([1,2,3], Value * Value)     
        public static async ValueTask<FormulaValue> SumTable(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            return await RunAggregatorAsync("Sum", irContext.ResultType == FormulaType.Decimal ? new SumDecimalAgg() : new SumAgg(), runner, context, irContext, args);
        }

        // VarP(1,2,3)
        internal static FormulaValue Var(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            return RunAggregator(new VarianceAgg(), irContext, args);
        }

        // VarP([1,2,3], Value * Value)
        public static async ValueTask<FormulaValue> VarTable(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            return await RunAggregatorAsync("VarP", new VarianceAgg(), runner, context, irContext, args).ConfigureAwait(false);
        }

        internal static FormulaValue Stdev(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            return RunAggregator(new StdDeviationAgg(), irContext, args);
        }

        public static async ValueTask<FormulaValue> StdevTable(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            return await RunAggregatorAsync("StdevP", new StdDeviationAgg(), runner, context, irContext, args).ConfigureAwait(false);
        }

        // Max(1,2,3)     
        internal static FormulaValue Max(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var agg = GetMinMaxAggType(runner.FunctionServices, irContext, false);

            if (agg != null)
            {
                return RunAggregator(agg, irContext, args);
            }
            else
            {
                return CommonErrors.UnreachableCodeError(irContext);
            }
        }

        // Max([1,2,3], Value * Value)     
        public static async ValueTask<FormulaValue> MaxTable(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var agg = GetMinMaxAggType(runner.FunctionServices, irContext, false);

            if (agg != null)
            {
                return await RunAggregatorAsync("Max", agg, runner, context, irContext, args).ConfigureAwait(false);
            }
            else
            {
                return CommonErrors.UnreachableCodeError(irContext);
            }
        }

        // Min(1,2,3)     
        internal static FormulaValue Min(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var agg = GetMinMaxAggType(runner.FunctionServices, irContext, true);

            if (agg != null)
            {
                return RunAggregator(agg, irContext, args);
            }
            else
            {
                return CommonErrors.UnreachableCodeError(irContext);
            }
        }

        // Min([1,2,3], Value * Value)     
        public static async ValueTask<FormulaValue> MinTable(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var agg = GetMinMaxAggType(runner.FunctionServices, irContext, true);

            if (agg != null)
            {
                return await RunAggregatorAsync("Min", agg, runner, context, irContext, args).ConfigureAwait(false);
            }
            else
            {
                return CommonErrors.UnreachableCodeError(irContext);
            }
        }

        private static IAggregator GetMinMaxAggType(IServiceProvider serviceProvider, IRContext irContext, bool isMin)
        {
            IAggregator agg;
            if (irContext.ResultType == FormulaType.Number)
            {
                agg = isMin ? new MinNumberAgg() : new MaxNumberAgg();
            }
            else if (irContext.ResultType == FormulaType.Decimal)
            {
                agg = isMin ? new MinDecimalAgg() : new MaxDecimalAgg();
            }
            else if (irContext.ResultType == FormulaType.DateTime || irContext.ResultType == FormulaType.Date)
            {
                agg = isMin ? new MinDateAndDateTimeAgg(serviceProvider) : new MaxDateAndDateTimeAgg(serviceProvider);
            }
            else if (irContext.ResultType == FormulaType.Time)
            {
                agg = isMin ? new MinTimeAgg() : new MaxTimeAgg();
            }
            else
            {
                return null;
            }

            return agg;
        }

        // Average ignores blanks.
        // Average(1,2,3)
        public static FormulaValue Average(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            return RunAggregator(irContext.ResultType == FormulaType.Decimal ? new AverageDecimalAgg() : new AverageAgg(), irContext, args);
        }

        // Average([1,2,3], Value * Value)     
        public static async ValueTask<FormulaValue> AverageTable(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (TableValue)args[0];

            if (arg0.Rows.Count() == 0)
            {
                return CommonErrors.DivByZeroError(irContext);
            }

            return await RunAggregatorAsync("Average", irContext.ResultType == FormulaType.Decimal ? new AverageDecimalAgg() : new AverageAgg(), runner, context, irContext, args);
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-mod
        public static FormulaValue Mod(IRContext irContext, FormulaValue[] args)
        {
            if (irContext.ResultType == FormulaType.Decimal)
            {
                decimal arg0 = ((DecimalValue)args[0]).Value;
                decimal arg1 = ((DecimalValue)args[1]).Value;
                decimal q;

                if (arg1 == 0m)
                {
                    return CommonErrors.DivByZeroError(irContext);
                }

                // r = a – N × floor(a/b)
                try
                {
                    q = decimal.Floor(arg0 / arg1);
                }
                catch (OverflowException)
                {
                    return CommonErrors.OverflowError(irContext);
                }

                decimal result = arg0 - (arg1 * q);

                return new DecimalValue(irContext, result);
            }
            else
            {
                double arg0 = ((NumberValue)args[0]).Value;
                double arg1 = ((NumberValue)args[1]).Value;

                if (arg1 == 0)
                {
                    return CommonErrors.DivByZeroError(irContext);
                }

                // r = a – N × floor(a/b)
                double q = Math.Floor(arg0 / arg1);
                if (IsInvalidDouble(q))
                {
                    return CommonErrors.OverflowError(irContext);
                }

                double result = arg0 - (arg1 * ((long)q));

                // We validate the reminder is in a valid range.
                // This is mainly to support very large numbers (like 1E+308) where the calculation could be incorrect
                if (result < -Math.Abs(arg1) || result > Math.Abs(arg1))
                {
                    return CommonErrors.OverflowError(irContext);
                }

                return new NumberValue(irContext, result);
            }
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-sequence
        // Argument 1 (cound) can be a different type from Argument 2 and 3 (start and stop) which must agree
        public static FormulaValue Sequence(IRContext irContext, FormulaValue[] args)
        {
            double records;

            if (args[0] is NumberValue num)
            {
                records = num.Value;
            }
            else if (args[0] is DecimalValue dec)
            {
                records = (double)dec.Value;
            }
            else
            {
                return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            if (records < 0)
            {
                return CommonErrors.ArgumentOutOfRange(irContext);
            }

            if (args[1] is NumberValue startN && args[2] is NumberValue stepN)
            {
                var rows = LazySequence(records, startN.Value, stepN.Value).Select(n => new NumberValue(IRContext.NotInSource(FormulaType.Number), n));
                return new InMemoryTableValue(irContext, StandardTableNodeRecords(irContext, rows.ToArray(), forceSingleColumn: true));
            }
            else if (args[1] is DecimalValue startW && args[2] is DecimalValue stepW)
            { 
                var rows = LazySequenceDecimal(records, startW.Value, stepW.Value).Select(n => new DecimalValue(IRContext.NotInSource(FormulaType.Decimal), n));
                return new InMemoryTableValue(irContext, StandardTableNodeRecords(irContext, rows.ToArray(), forceSingleColumn: true));
            }
            else
            {
                return CommonErrors.RuntimeTypeMismatch(irContext);
            }
        }

        private static IEnumerable<double> LazySequence(double records, double start, double step)
        {
            var x = start;
            for (var i = 1; i <= records; i++)
            {
                yield return x;
                x += step;
            }
        }

        private static IEnumerable<decimal> LazySequenceDecimal(double records, decimal start, decimal step)
        {
            var x = start;
            for (var i = 1; i <= records; i++)
            {
                yield return x;
                x += step;
            }
        }

        public static FormulaValue Abs(IRContext irContext, FormulaValue[] args)
        {
            var arg0 = args[0];

            if (arg0 is NumberValue num)
            {
                if (num == null)
                {
                    return new NumberValue(irContext, 0d);
                }

                double x = num.Value;
                double val = Math.Abs(x);
                return new NumberValue(irContext, val);
            }
            else if (arg0 is DecimalValue dec)
            {
                if (dec == null)
                {
                    return new DecimalValue(irContext, 0m);
                }

                decimal x = dec.Value;
                decimal val = x < 0m ? -x : x;
                return new DecimalValue(irContext, val);
            }
            else
            {
                return CommonErrors.UnreachableCodeError(irContext);
            }
        }

        public static FormulaValue Round(IRContext irContext, FormulaValue[] args)
        {
            int digits;

            if (args.Length == 2 && args[1] is NumberValue numberDigs)
            {
                digits = (int)numberDigs.Value;
            }
            else if (args.Length == 2 && args[1] is DecimalValue decimalDigs)
            {
                digits = (int)decimalDigs.Value;
            }
            else
            { 
                return CommonErrors.UnreachableCodeError(irContext);
            }

            if (args[0] is NumberValue num)
            {
                return RoundFloat(irContext, num, digits);
            }
            else if (args[0] is DecimalValue dec)
            {
                return RoundDecimal(irContext, dec, digits);
            }

            return CommonErrors.UnreachableCodeError(irContext);
        }

        internal static FormulaValue RoundFloat(IRContext irContext, NumberValue num, int dg, RoundType rt = RoundType.Default)
        {
            var number = num.Value;
            var s = number < 0 ? -1d : 1d;
            var n = number * s;

            if (dg < -15 || dg > 15 || number < -1e20d || number > 1e20d)
            {
                return num;
            }

            // Dividing by m, since multiplication was introducing floating point error
            var m = Math.Pow(10d, dg);
            var eps = 1 / (m * 1e12d); // used to manage rounding of 1.4499999999999999999996

            switch (rt)
            {
                case RoundType.Default:
                    return new NumberValue(irContext, s * Math.Floor((n + (1 / (2 * m)) + eps) * m) / m);
                case RoundType.Down:
                    return new NumberValue(irContext, s * Math.Floor(n * m) / m);
                case RoundType.Up:
                    return new NumberValue(irContext, s * Math.Ceiling(n * m) / m);
            }

            return CommonErrors.UnreachableCodeError(irContext);
        }

        private static readonly IReadOnlyList<decimal> DecPow10 = new decimal[]
        {
            1e+00m, 1e+01m, 1e+02m, 1e+03m, 1e+04m, 1e+05m, 1e+06m, 1e+07m, 1e+08m, 1e+09m,
            1e+10m, 1e+11m, 1e+12m, 1e+13m, 1e+14m, 1e+15m, 1e+16m, 1e+17m, 1e+18m, 1e+19m,
            1e+20m, 1e+21m, 1e+22m, 1e+23m, 1e+24m, 1e+25m, 1e+26m, 1e+27m, 1e+28m
        };

        private static readonly IReadOnlyList<decimal> DecNegPow10 = new decimal[]
        {
            1e-00m, 1e-01m, 1e-02m, 1e-03m, 1e-04m, 1e-05m, 1e-06m, 1e-07m, 1e-08m, 1e-09m,
            1e-10m, 1e-11m, 1e-12m, 1e-13m, 1e-14m, 1e-15m, 1e-16m, 1e-17m, 1e-18m, 1e-19m,
            1e-20m, 1e-21m, 1e-22m, 1e-23m, 1e-24m, 1e-25m, 1e-26m, 1e-27m, 1e-28m
        };

        // The algorithm for Decimal is different from that of Float because with less range we are going out of our way to avoid overflow
        // At the time of this writing, the version of .NET being targeted only supports two varieties of MidpointRounding
        // In the future, some of this can be replaced with built in support in decimal.Round()
        internal static FormulaValue RoundDecimal(IRContext irContext, DecimalValue dec, int digits, RoundType roundType = RoundType.Default)
        {
            var signedNumber = dec.Value;
            var sign = signedNumber < 0 ? -1m : 1m;
            var unsignedNumber = signedNumber < 0 ? -signedNumber : signedNumber;

            if (digits < -28)
            {
                return new DecimalValue(irContext, 0m);
            }
            else if (digits > 28)
            {
                return dec;
            }

            try
            {
                switch (roundType)
                {
                    case RoundType.Default:
                        if (digits >= 0)
                        {
                            return new DecimalValue(irContext, decimal.Round(signedNumber, digits, MidpointRounding.AwayFromZero));
                        }
                        else
                        {
                            // safe to divide n and multiply by the same amount, won't overflow unless the result would have overflowed
                            var scale = DecPow10[-digits];
                            return new DecimalValue(irContext, decimal.Round(signedNumber / scale, 0, MidpointRounding.AwayFromZero) * scale);
                        }

                    case RoundType.Down:
                        if (digits == 0)
                        {
                            // this could be covered by the below dg < 0 case, but this is an important scenario to optimize
                            // Trunc with no second argument comes here
                            return new DecimalValue(irContext, sign * decimal.Floor(unsignedNumber));
                        }
                        else if (digits < 0)
                        {
                            // safe to divide n and multiply by the same amount, won't overflow unless the result would have overflowed
                            var scale = DecPow10[-digits];
                            return new DecimalValue(irContext, sign * decimal.Floor(unsignedNumber / scale) * scale);
                        }
                        else
                        {
                            // uses the system Round to avoid overflow and then correct if the result was rounded up
                            var unsignedRound = decimal.Round(unsignedNumber, digits, MidpointRounding.AwayFromZero);
                            return new DecimalValue(irContext, sign * (unsignedRound > unsignedNumber ? unsignedRound - DecNegPow10[digits] : unsignedRound));
                        }

                    case RoundType.Up:
                        if (digits == 0)
                        {
                            // this could be covered by the below dg < 0 case, but this is an important scenario to optimize
                            return new DecimalValue(irContext, sign * decimal.Ceiling(unsignedNumber));
                        }
                        else if (digits < 0)
                        {
                            var scale = DecPow10[-digits];
                            return new DecimalValue(irContext, sign * decimal.Ceiling(unsignedNumber / scale) * scale);
                        }
                        else
                        {
                            // uses the system Round to avoid overflow and then correct if the result was rounded down
                            var unsignedRound = decimal.Round(unsignedNumber, digits, MidpointRounding.AwayFromZero);
                            return new DecimalValue(irContext, sign * (unsignedRound < unsignedNumber ? unsignedRound + DecNegPow10[digits] : unsignedRound));
                        }
                }
            }
            catch (OverflowException)
            {
                return CommonErrors.OverflowError(irContext);
            }

            return CommonErrors.UnreachableCodeError(irContext);
        }

        public enum RoundType
        {
            Default,
            Up,
            Down
        }

        public static FormulaValue RoundUp(IRContext irContext, FormulaValue[] args)
        {
            int digits;

            if (args.Length == 2 && args[1] is NumberValue numberDigs)
            {
                digits = (int)numberDigs.Value;
            }
            else if (args.Length == 2 && args[1] is DecimalValue decimalDigs)
            {
                digits = (int)decimalDigs.Value;
            }
            else
            {
                return CommonErrors.UnreachableCodeError(irContext);
            }

            if (args[0] is NumberValue num)
            {
                return RoundFloat(irContext, num, digits, RoundType.Up);
            }
            else if (args[0] is DecimalValue dec)
            {
                return RoundDecimal(irContext, dec, digits, RoundType.Up);
            }

            return CommonErrors.UnreachableCodeError(irContext);
        }

        public static FormulaValue RoundDown(IRContext irContext, FormulaValue[] args)
        {
            int digits;

            // Trunc uses RoundDown as the implementation, and Trunc's second argument is optional
            if (args.Length == 2 && args[1] is NumberValue numberDigs)
            {
                digits = (int)numberDigs.Value;
            }
            else if (args.Length == 2 && args[1] is DecimalValue decimalDigs)
            {
                digits = (int)decimalDigs.Value;
            }
            else if (args.Length == 1)
            {
                digits = 0;
            }
            else
            {
                return CommonErrors.UnreachableCodeError(irContext);
            }

            if (args[0] is NumberValue num)
            {
                return RoundFloat(irContext, num, digits, RoundType.Down);
            }
            else if (args[0] is DecimalValue dec)
            {
                return RoundDecimal(irContext, dec, digits, RoundType.Down);
            }

            return CommonErrors.UnreachableCodeError(irContext);
        }

        public static FormulaValue Int(IRContext irContext, FormulaValue[] args)
        {
            if (args[0] is NumberValue num)
            {
                var val = Math.Floor(num.Value);
                return new NumberValue(irContext, val);
            }
            else if (args[0] is DecimalValue dec)
            {
                var val = decimal.Floor(dec.Value);
                return new DecimalValue(irContext, val);
            }

            return CommonErrors.UnreachableCodeError(irContext);
        }

        public static FormulaValue Ln(IRContext irContext, NumberValue[] args)
        {
            var number = args[0].Value;
            return new NumberValue(irContext, Math.Log(number));
        }

        public static FormulaValue Log(IRContext irContext, NumberValue[] args)
        {
            var number = args[0].Value;
            var numberBase = args[1].Value;

            if (numberBase == 1)
            {
                return GetDiv0Error(irContext);
            }

            return new NumberValue(irContext, Math.Log(number, numberBase));
        }

        public static FormulaValue Exp(IRContext irContext, NumberValue[] args)
        {
            var exponent = args[0].Value;
            var d = Math.Pow(Math.E, exponent);

            if (double.IsInfinity(d))
            {
                return CommonErrors.OverflowError(irContext);
            }

            return new NumberValue(irContext, d);
        }

        public static FormulaValue Power(IRContext irContext, NumberValue[] args)
        {
            var number = args[0].Value;
            var exponent = args[1].Value;

            if (number == 0)
            {
                if (exponent < 0)
                {
                    return GetDiv0Error(irContext);
                }
                else if (exponent == 0)
                {
                    return new NumberValue(irContext, 1);
                }
            }

            var d = Math.Pow(number, exponent);

            if (double.IsInfinity(d))
            {
                return CommonErrors.OverflowError(irContext);
            }

            return new NumberValue(irContext, d);
        }

        // Since IRandomService is a pluggable service,
        // validate that the implementation is within spec.
        // This catches potential host bugs. 
        private static double SafeNextDouble(this IRandomService random)
        {
            var value = random.NextDouble();

            if (value < 0 || value > 1)
            {
                // This is a bug in the host's IRandomService.
                throw new InvalidOperationException($"IRandomService ({random.GetType().FullName}) returned an illegal value {value}. Must be between 0 and 1");
            }

            return value;
        }

        private static double SafeNextDouble(this IServiceProvider services)
        {
            var random = services.GetService<IRandomService>(_defaultRandService);
            return random.SafeNextDouble();
        }

        private static async ValueTask<FormulaValue> Rand(
            EvalVisitor runner,
            EvalVisitorContext context,
            IRContext irContext,
            FormulaValue[] args)
        {
            var services = runner.FunctionServices;

            var value = services.SafeNextDouble();
            return new NumberValue(irContext, value);
        }

        public static FormulaValue RandBetween(IServiceProvider services, IRContext irContext, NumberValue[] args)
        {
            var lower = args[0].Value;
            var upper = args[1].Value;

            if (lower > upper)
            {
                return new ErrorValue(irContext, new ExpressionError()
                {
                    Message = $"Lower value cannot be greater than Upper value",
                    Span = irContext.SourceContext,
                    Kind = ErrorKind.Numeric
                });
            }

            lower = Math.Ceiling(lower);
            upper = Math.Floor(upper);

            var value = services.SafeNextDouble();
            return new NumberValue(irContext, Math.Floor((value * (upper - lower + 1)) + lower));
        }

        private static FormulaValue Pi(IRContext irContext, FormulaValue[] args)
        {
            return new NumberValue(irContext, Math.PI);
        }

        // Given the absence of Math.Cot function, we compute Cot(x) as 1/Tan(x)
        // Reference: https://en.wikipedia.org/wiki/Trigonometric_functions
        private static FormulaValue Cot(IRContext irContext, NumberValue[] args)
        {
            var arg = args[0].Value;
            var tan = Math.Tan(arg);
            if (tan == 0)
            {
                return GetDiv0Error(irContext);
            }

            var cot = 1 / tan;
            return new NumberValue(irContext, cot);
        }

        // Given the absence of Math.Acot function, we compute acot(x) as pi/2 - atan(x)
        // Reference: https://en.wikipedia.org/wiki/Inverse_trigonometric_functions
        public static FormulaValue Acot(IRContext irContext, NumberValue[] args)
        {
            var arg = args[0].Value;
            var atan = Math.Atan(arg);
            return new NumberValue(irContext, (Math.PI / 2) - atan);
        }

        public static FormulaValue Atan2(IRContext irContext, NumberValue[] args)
        {
            var x = args[0].Value;
            var y = args[1].Value;

            if (x == 0 && y == 0)
            {
                return GetDiv0Error(irContext);
            }

            // Unlike Excel, C#'s Math.Atan2 expects 'y' as first argument and 'x' as second.
            return new NumberValue(irContext, Math.Atan2(y, x));
        }

        public static Func<IRContext, NumberValue[], FormulaValue> SingleArgTrig(Func<double, double> function)
        {
            return (IRContext irContext, NumberValue[] args) =>
            {
                var arg = args[0].Value;
                var result = function(arg);
                return new NumberValue(irContext, result);
            };
        }

        private static ErrorValue GetDiv0Error(IRContext irContext)
        {
            return new ErrorValue(irContext, new ExpressionError
            {
                Kind = ErrorKind.Div0,
                Span = irContext.SourceContext,
                Message = "Division by zero"
            });
        }

        private static FormulaValue Dec2Hex(IRContext irContext, NumberValue[] args)
        {
            var minNumber = -(1L << 39);
            var maxNumber = (1L << 39) - 1;

            var number = Math.Floor(args[0].Value);
            int? places = null;
            if (args.Length > 1)
            {
                places = (int)Math.Floor(args[1].Value);
            }

            if (number < minNumber || number > maxNumber)
            {
                return CommonErrors.OverflowError(irContext);
            }

            // places need to be non-negative and between 1 and 10
            if (places != null && (places < 1 || places > 10))
            {
                return new ErrorValue(irContext, new ExpressionError()
                {
                    Message = $"Places should be between 1 and 10",
                    Span = irContext.SourceContext,
                    Kind = ErrorKind.Numeric
                });
            }

            var roundNumber = (long)number;
            string result;
            /*
             * a long negative will result in 16 characters so
             * negative numbers need to be truncated down to 10 characters
            */
            if (number < 0)
            {
                result = roundNumber.ToString("X", CultureInfo.InvariantCulture);
                result = result.Substring(result.Length - 10, 10);
            }
            else
            {
                result = roundNumber.ToString("X" + places, CultureInfo.InvariantCulture);
            }

            // places need to be greater or equal to length of hexadecimal when number is positive
            if (result.Length > places && number > 0)
            {
                return new ErrorValue(irContext, new ExpressionError()
                {
                    Message = $"Places argument must be big enough to hold the result",
                    Span = irContext.SourceContext,
                    Kind = ErrorKind.Numeric
                });
            }

            return new StringValue(irContext, result);
        }

        private static FormulaValue Hex2Dec(IRContext irContext, StringValue[] args)
        {
            var number = args[0].Value;

            if (string.IsNullOrEmpty(number))
            {
                return new NumberValue(irContext, 0);
            }

            if (number.Length > 10)
            {
                return CommonErrors.OverflowError(irContext);
            }

            // negative numbers starts after 8000000000
            if (number.Length == 10 && string.Compare(number, "8000000000", StringComparison.Ordinal) > 0)
            {
                var maxNumber = (long)(1L << 40);
                long.TryParse(number, System.Globalization.NumberStyles.HexNumber, null, out var negative_result);
                negative_result -= maxNumber;
                return new NumberValue(irContext, negative_result);
            }

            if (!long.TryParse(number, System.Globalization.NumberStyles.HexNumber, null, out var result))
            {
                return CommonErrors.OverflowError(irContext);
            }

            return new NumberValue(irContext, result);
        }
    }
}
