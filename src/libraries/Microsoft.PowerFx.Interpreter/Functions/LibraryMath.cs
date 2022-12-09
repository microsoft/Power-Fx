// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        private class MinDateAndDateTimeAgg : IAggregator
        {
            protected DateTime _minValueDT = DateTime.MaxValue;
            protected int _count = 0;

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
                        dt = dtv.Value;
                        break;
                    case DateValue dv:
                        dt = dv.Value;
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

        private class MaxDateAndDateTimeAgg : IAggregator
        {
            protected DateTime _maxValueDT = DateTime.MinValue;
            protected int _count = 0;

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
                        dt = dtv.Value;
                        break;
                    case DateValue dv:
                        dt = dv.Value;
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

                var value = await arg1.EvalInRowScopeAsync(context.NewScope(childContext));

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
        internal static FormulaValue Sum(IRContext irContext, FormulaValue[] args)
        {
            return RunAggregator(new SumAgg(), irContext, args);
        }

        // Sum([1,2,3], Value * Value)     
        public static async ValueTask<FormulaValue> SumTable(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            return await RunAggregatorAsync("Sum", new SumAgg(), runner, context, irContext, args);
        }

        // VarP(1,2,3)
        internal static FormulaValue Var(IRContext irContext, FormulaValue[] args)
        {
            return RunAggregator(new VarianceAgg(), irContext, args);
        }

        // VarP([1,2,3], Value * Value)
        public static async ValueTask<FormulaValue> VarTable(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            return await RunAggregatorAsync("VarP", new VarianceAgg(), runner, context, irContext, args);
        }

        internal static FormulaValue Stdev(IRContext irContext, FormulaValue[] args)
        {
            return RunAggregator(new StdDeviationAgg(), irContext, args);
        }

        public static async ValueTask<FormulaValue> StdevTable(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            return await RunAggregatorAsync("StdevP", new StdDeviationAgg(), runner, context, irContext, args);
        }

        // Max(1,2,3)     
        internal static FormulaValue Max(IRContext irContext, FormulaValue[] args)
        {
            var agg = GetMinMaxAggType(irContext, false);

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
            var agg = GetMinMaxAggType(irContext, false);

            if (agg != null)
            {
                return await RunAggregatorAsync("Max", agg, runner, context, irContext, args);
            }
            else
            {
                return CommonErrors.UnreachableCodeError(irContext);
            }
        }

        // Min(1,2,3)     
        internal static FormulaValue Min(IRContext irContext, FormulaValue[] args)
        {
            var agg = GetMinMaxAggType(irContext, true);

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
            var agg = GetMinMaxAggType(irContext, true);

            if (agg != null)
            {
                return await RunAggregatorAsync("Min", agg, runner, context, irContext, args);
            }
            else
            {
                return CommonErrors.UnreachableCodeError(irContext);
            }
        }

        private static IAggregator GetMinMaxAggType(IRContext irContext, bool isMin)
        {
            IAggregator agg;
            if (irContext.ResultType == FormulaType.Number)
            {
                agg = isMin ? new MinNumberAgg() : new MaxNumberAgg();
            }
            else if (irContext.ResultType == FormulaType.DateTime || irContext.ResultType == FormulaType.Date)
            {
                agg = isMin ? new MinDateAndDateTimeAgg() : new MaxDateAndDateTimeAgg();
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
        public static FormulaValue Average(IRContext irContext, FormulaValue[] args)
        {
            return RunAggregator(new AverageAgg(), irContext, args);
        }

        // Average([1,2,3], Value * Value)     
        public static async ValueTask<FormulaValue> AverageTable(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (TableValue)args[0];

            if (arg0.Rows.Count() == 0)
            {
                return CommonErrors.DivByZeroError(irContext);
            }

            return await RunAggregatorAsync("Average", new AverageAgg(), runner, context, irContext, args);
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-mod
        public static FormulaValue Mod(IRContext irContext, NumberValue[] args)
        {
            var arg0 = args[0].Value;
            var arg1 = args[1].Value;

            if (arg1 == 0)
            {
                return CommonErrors.DivByZeroError(irContext);
            }

            // r = a – N × floor(a/b)
            var q = Math.Floor(arg0 / arg1);
            if (IsInvalidDouble(q))
            {
                return CommonErrors.OverflowError(irContext);
            }

            var result = arg0 - (arg1 * ((long)q));

            // We validate the reminder is in a valid range.
            // This is mainly to support very large numbers (like 1E+308) where the calculation could be incorrect
            if (result < -Math.Abs(arg1) || result > Math.Abs(arg1))
            {
                return CommonErrors.OverflowError(irContext);
            }

            return new NumberValue(irContext, result);
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-sequence
        public static FormulaValue Sequence(IRContext irContext, NumberValue[] args)
        {
            var records = args[0].Value;
            var start = args[1].Value;
            var step = args[2].Value;

            if (records < 0)
            {
                return CommonErrors.ArgumentOutOfRange(irContext);
            }

            var rows = LazySequence(records, start, step).Select(n => new NumberValue(IRContext.NotInSource(FormulaType.Number), n));

            return new InMemoryTableValue(irContext, StandardTableNodeRecords(irContext, rows.ToArray(), forceSingleColumn: true));
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

        public static FormulaValue Abs(IRContext irContext, NumberValue[] args)
        {
            var arg0 = args[0];

            if (arg0 == null)
            {
                return new NumberValue(irContext, 0d);
            }

            var x = arg0.Value;
            var val = Math.Abs(x);
            return new NumberValue(irContext, val);
        }

        public static FormulaValue Round(IRContext irContext, NumberValue[] args)
        {
            var numberArg = args[0].Value;
            var digitsArg = args[1].Value;

            var x = Round(numberArg, digitsArg);
            return new NumberValue(irContext, x);
        }

        internal static double Round(double number, double digits, RoundType rt = RoundType.Default)
        {
            var s = number < 0 ? -1d : 1d;
            var n = number * s;
            var dg = digits < 0 ? (int)Math.Ceiling(digits) : (int)Math.Floor(digits);

            if (dg < -15 || dg > 15 || number < -1e20d || number > 1e20d)
            {
                return number;
            }

            // Dividing by m, since multiplication was introducing floating point error
            var m = Math.Pow(10d, dg);
            var eps = 1 / (m * 1e12d); // used to manage rounding of 1.4499999999999999999996

            switch (rt)
            {
                case RoundType.Default:
                    return s * Math.Floor((n + (1 / (2 * m)) + eps) * m) / m;
                case RoundType.Down:
                    return s * Math.Floor(n * m) / m;
                case RoundType.Up:
                    return s * Math.Ceiling(n * m) / m;
            }

            return 0;
        }

        public enum RoundType
        {
            Default,
            Up,
            Down
        }

        // Char is used for PA string escaping 
        public static FormulaValue RoundUp(IRContext irContext, NumberValue[] args)
        {
            var numberArg = args[0].Value;
            var digitsArg = args[1].Value;

            var x = Round(numberArg, digitsArg, RoundType.Up);
            return new NumberValue(irContext, x);
        }

        public static FormulaValue RoundDown(IRContext irContext, NumberValue[] args)
        {
            var numberArg = args[0].Value;
            var digitsArg = args[1].Value;

            var x = Round(numberArg, digitsArg, RoundType.Down);
            return new NumberValue(irContext, x);
        }

        public static FormulaValue Int(IRContext irContext, NumberValue[] args)
        {
            var arg0 = args[0];
            var x = arg0.Value;
            var val = Math.Floor(x);
            return new NumberValue(irContext, val);
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

            return new NumberValue(irContext, 1 / tan);
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
            var places = (int)Math.Floor(args[1].Value);

            if (number < minNumber || number > maxNumber)
            {
                return CommonErrors.OverflowError(irContext);
            }

            // places need to be non-negative and 10 or less
            if (places < 0 || places > 10)
            {
                return new ErrorValue(irContext, new ExpressionError()
                {
                    Message = $"Places should be between 0 and 10",
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
                result = roundNumber.ToString("X");
                result = result.Substring(result.Length - 10, 10);
            }
            else
            {
                result = roundNumber.ToString("X" + places);
            }

            // places need to be greater or equal to length of hexadecimal when number is positive
            if (places != 0 && result.Length > places && number > 0)
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
            if (number.Length == 10 && number.CompareTo("8000000000") > 0)
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
