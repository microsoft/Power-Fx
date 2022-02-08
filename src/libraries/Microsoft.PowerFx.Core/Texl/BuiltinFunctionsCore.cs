// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl
{
    // List of TexlFunction instances in Power Fx core.
    // - no ControlInfo dependency 
    // - just functions that are ported over to Language.Core
    internal class BuiltinFunctionsCore
    {
        public static IEnumerable<TexlFunction> BuiltinFunctionsLibrary => _library;

        // Functions in this list are shared and may show up in other hosts by default.
        private static readonly List<TexlFunction> _library = new List<TexlFunction>(200);
        
        public static readonly TexlFunction AmPm = _library.Append(new AmPmFunction());
        public static readonly TexlFunction AmPmShort = _library.Append(new AmPmShortFunction());
        public static readonly TexlFunction Abs = _library.Append(new AbsFunction());
        public static readonly TexlFunction AbsT = _library.Append(new AbsTableFunction());
        public static readonly TexlFunction Acos = _library.Append(new AcosFunction());
        public static readonly TexlFunction AcosT = _library.Append(new AcosTableFunction());
        public static readonly TexlFunction Acot = _library.Append(new AcotFunction());
        public static readonly TexlFunction AcotT = _library.Append(new AcotTableFunction());
        public static readonly TexlFunction AddColumns = _library.Append(new AddColumnsFunction());
        public static readonly TexlFunction And = _library.Append(new VariadicLogicalFunction(isAnd: true));
        public static readonly TexlFunction AsType = _library.Append(new AsTypeFunction());
        public static readonly TexlFunction Atan = _library.Append(new AtanFunction());
        public static readonly TexlFunction AtanT = _library.Append(new AtanTableFunction());
        public static readonly TexlFunction Atan2 = _library.Append(new Atan2Function());
        public static readonly TexlFunction Average = _library.Append(new AverageFunction());
        public static readonly TexlFunction AverageT = _library.Append(new AverageTableFunction());
        public static readonly TexlFunction Blank = _library.Append(new BlankFunction());
        public static readonly TexlFunction Clock24 = _library.Append(new IsClock24Function());
        public static readonly TexlFunction Char = _library.Append(new CharFunction());
        public static readonly TexlFunction CharT = _library.Append(new CharTFunction());
        public static readonly TexlFunction Coalesce = _library.Append(new CoalesceFunction());
        public static readonly TexlFunction ColorFade = _library.Append(new ColorFadeFunction());
        public static readonly TexlFunction ColorFadeT = _library.Append(new ColorFadeTFunction());
        public static readonly TexlFunction ColorValue = _library.Append(new ColorValueFunction());
        public static readonly TexlFunction Concat = _library.Append(new ConcatFunction());
        public static readonly TexlFunction Concatenate = _library.Append(new ConcatenateFunction());
        public static readonly TexlFunction ConcatenateT = _library.Append(new ConcatenateTableFunction());
        public static readonly TexlFunction Cos = _library.Append(new CosFunction());
        public static readonly TexlFunction CosT = _library.Append(new CosTableFunction());
        public static readonly TexlFunction Cot = _library.Append(new CotFunction());
        public static readonly TexlFunction CotT = _library.Append(new CotTableFunction());
        public static readonly TexlFunction Count = _library.Append(new CountFunction());
        public static readonly TexlFunction CountA = _library.Append(new CountAFunction());
        public static readonly TexlFunction CountIf = _library.Append(new CountIfFunction());
        public static readonly TexlFunction CountRows = _library.Append(new CountRowsFunction());
        public static readonly TexlFunction Date = _library.Append(new DateFunction());
        public static readonly TexlFunction DateAdd = _library.Append(new DateAddFunction());
        public static readonly TexlFunction DateAddT = _library.Append(new DateAddTFunction());
        public static readonly TexlFunction DateDiff = _library.Append(new DateDiffFunction());
        public static readonly TexlFunction DateDiffT = _library.Append(new DateDiffTFunction());
        public static readonly TexlFunction DateTimeValue = _library.Append(new DateTimeValueFunction());
        public static readonly TexlFunction DateValue = _library.Append(new DateValueFunction());
        public static readonly TexlFunction Day = _library.Append(new DayFunction());
        public static readonly TexlFunction Degrees = _library.Append(new DegreesFunction());
        public static readonly TexlFunction DegreesT = _library.Append(new DegreesTableFunction());
        public static readonly TexlFunction EndsWith = _library.Append(new EndsWithFunction());
        public static readonly TexlFunction Error = _library.Append(new ErrorFunction());
        public static readonly TexlFunction Exp = _library.Append(new ExpFunction());
        public static readonly TexlFunction ExpT = _library.Append(new ExpTableFunction());
        public static readonly TexlFunction Filter = _library.Append(new FilterFunction());
        public static readonly TexlFunction Find = _library.Append(new FindFunction());
        public static readonly TexlFunction FindT = _library.Append(new FindTFunction());
        public static readonly TexlFunction First = _library.Append(new FirstLastFunction(isFirst: true));
        public static readonly TexlFunction FirstN = _library.Append(new FirstLastNFunction(isFirst: true));
        public static readonly TexlFunction ForAll = _library.Append(new ForAllFunction());
        public static readonly TexlFunction Hour = _library.Append(new HourFunction());
        public static readonly TexlFunction If = _library.Append(new IfFunction());
        public static readonly TexlFunction IfError = _library.Append(new IfErrorFunction());
        public static readonly TexlFunction Int = _library.Append(new IntFunction());
        public static readonly TexlFunction IntT = _library.Append(new IntTableFunction());
        public static readonly TexlFunction IsBlank = _library.Append(new IsBlankFunction());
        public static readonly TexlFunction IsError = _library.Append(new IsErrorFunction());
        public static readonly TexlFunction IsToday = _library.Append(new IsTodayFunction());
        public static readonly TexlFunction IsNumeric = _library.Append(new IsNumericFunction());
        public static readonly TexlFunction ISOWeekNum = _library.Append(new ISOWeekNumFunction());
        public static readonly TexlFunction Last = _library.Append(new FirstLastFunction(isFirst: false));
        public static readonly TexlFunction LastN = _library.Append(new FirstLastNFunction(isFirst: false));
        public static readonly TexlFunction Left = _library.Append(new LeftRightScalarFunction(isLeft: true));
        public static readonly TexlFunction LeftTS = _library.Append(new LeftRightTableScalarFunction(isLeft: true));
        public static readonly TexlFunction LeftTT = _library.Append(new LeftRightTableTableFunction(isLeft: true));
        public static readonly TexlFunction LeftST = _library.Append(new LeftRightScalarTableFunction(isLeft: true));
        public static readonly TexlFunction Len = _library.Append(new LenFunction());
        public static readonly TexlFunction LenT = _library.Append(new LenTFunction());
        public static readonly TexlFunction Ln = _library.Append(new LnFunction());
        public static readonly TexlFunction LnT = _library.Append(new LnTableFunction());
        public static readonly TexlFunction Log = _library.Append(new LogFunction());
        public static readonly TexlFunction LogT = _library.Append(new LogTFunction());
        public static readonly TexlFunction Lower = _library.Append(new LowerUpperFunction(isLower: true));
        public static readonly TexlFunction LowerT = _library.Append(new LowerUpperTFunction(isLower: true));
        public static readonly TexlFunction Max = _library.Append(new MinMaxFunction(isMin: false));
        public static readonly TexlFunction MaxT = _library.Append(new MinMaxTableFunction(isMin: false));
        public static readonly TexlFunction Mid = _library.Append(new MidFunction());
        public static readonly TexlFunction MidT = _library.Append(new MidTFunction());
        public static readonly TexlFunction Min = _library.Append(new MinMaxFunction(isMin: true));
        public static readonly TexlFunction MinT = _library.Append(new MinMaxTableFunction(isMin: true));
        public static readonly TexlFunction Minute = _library.Append(new MinuteFunction());
        public static readonly TexlFunction Mod = _library.Append(new ModFunction());
        public static readonly TexlFunction ModT = _library.Append(new ModTFunction());
        public static readonly TexlFunction Month = _library.Append(new MonthFunction());
        public static readonly TexlFunction MonthsLong = _library.Append(new MonthsLongFunction());
        public static readonly TexlFunction MonthsShort = _library.Append(new MonthsShortFunction());
        public static readonly TexlFunction Not = _library.Append(new NotFunction());
        public static readonly TexlFunction Now = _library.Append(new NowFunction());
        public static readonly TexlFunction Or = _library.Append(new VariadicLogicalFunction(isAnd: false));
        public static readonly TexlFunction Power = _library.Append(new PowerFunction());
        public static readonly TexlFunction PowerT = _library.Append(new PowerTFunction());
        public static readonly TexlFunction Pi = _library.Append(new PiFunction());
        public static readonly TexlFunction Proper = _library.Append(new ProperFunction());
        public static readonly TexlFunction ProperT = _library.Append(new ProperTFunction());
        public static readonly TexlFunction Radians = _library.Append(new RadiansFunction());
        public static readonly TexlFunction RadiansT = _library.Append(new RadiansTableFunction());
        public static readonly TexlFunction Rand = _library.Append(new RandFunction());
        public static readonly TexlFunction RandBetween = _library.Append(new RandBetweenFunction());
        public static readonly TexlFunction Replace = _library.Append(new ReplaceFunction());
        public static readonly TexlFunction ReplaceT = _library.Append(new ReplaceTFunction());
        public static readonly TexlFunction RGBA = _library.Append(new RGBAFunction());
        public static readonly TexlFunction Right = _library.Append(new LeftRightScalarFunction(isLeft: false));
        public static readonly TexlFunction RightTS = _library.Append(new LeftRightTableScalarFunction(isLeft: false));
        public static readonly TexlFunction RightTT = _library.Append(new LeftRightTableTableFunction(isLeft: false));
        public static readonly TexlFunction RightST = _library.Append(new LeftRightScalarTableFunction(isLeft: false));
        public static readonly TexlFunction Round = _library.Append(new RoundScalarFunction());
        public static readonly TexlFunction RoundT = _library.Append(new RoundTableFunction());
        public static readonly TexlFunction RoundDown = _library.Append(new RoundDownScalarFunction());
        public static readonly TexlFunction RoundDownT = _library.Append(new RoundDownTableFunction());
        public static readonly TexlFunction RoundUp = _library.Append(new RoundUpScalarFunction());
        public static readonly TexlFunction RoundUpT = _library.Append(new RoundUpTableFunction());
        public static readonly TexlFunction Second = _library.Append(new SecondFunction());
        public static readonly TexlFunction Sequence = _library.Append(new SequenceFunction());
        public static readonly TexlFunction Shuffle = _library.Append(new ShuffleFunction());
        public static readonly TexlFunction Sin = _library.Append(new SinFunction());
        public static readonly TexlFunction Sort = _library.Append(new SortFunction());
        public static readonly TexlFunction SortByColumns = _library.Append(new SortByColumnsFunction());
        public static readonly TexlFunction SortByColumnsOrderTable = _library.Append(new SortByColumnsOrderTableFunction());
        public static readonly TexlFunction SinT = _library.Append(new SinTableFunction());
        public static readonly TexlFunction Split = _library.Append(new SplitFunction());
        public static readonly TexlFunction Sqrt = _library.Append(new SqrtFunction());
        public static readonly TexlFunction SqrtT = _library.Append(new SqrtTableFunction());
        public static readonly TexlFunction StartsWith = _library.Append(new StartsWithFunction());
        public static readonly TexlFunction StdevP = _library.Append(new StdevPFunction());
        public static readonly TexlFunction StdevPT = _library.Append(new StdevPTableFunction());
        public static readonly TexlFunction Substitute = _library.Append(new SubstituteFunction());
        public static readonly TexlFunction SubstituteT = _library.Append(new SubstituteTFunction());
        public static readonly TexlFunction Sum = _library.Append(new SumFunction());
        public static readonly TexlFunction SumT = _library.Append(new SumTableFunction());
        public static readonly TexlFunction Switch = _library.Append(new SwitchFunction());
        public static readonly TexlFunction Table = _library.Append(new TableFunction());
        public static readonly TexlFunction Tan = _library.Append(new TanFunction());
        public static readonly TexlFunction TanT = _library.Append(new TanTableFunction());
        public static readonly TexlFunction Time = _library.Append(new TimeFunction());
        public static readonly TexlFunction TimeValue = _library.Append(new TimeValueFunction());
        public static readonly TexlFunction TimeZoneOffset = _library.Append(new TimeZoneOffsetFunction());
        public static readonly TexlFunction Today = _library.Append(new TodayFunction());
        public static readonly TexlFunction Trim = _library.Append(new TrimFunction());
        public static readonly TexlFunction TrimT = _library.Append(new TrimTFunction());
        public static readonly TexlFunction TrimEnds = _library.Append(new TrimEndsFunction());
        public static readonly TexlFunction TrimEndsT = _library.Append(new TrimEndsTFunction());
        public static readonly TexlFunction Trunc = _library.Append(new TruncFunction());
        public static readonly TexlFunction TruncT = _library.Append(new TruncTableFunction());
        public static readonly TexlFunction Upper = _library.Append(new LowerUpperFunction(isLower: false));
        public static readonly TexlFunction UpperT = _library.Append(new LowerUpperTFunction(isLower: false));
        public static readonly TexlFunction Value = _library.Append(new ValueFunction());
        public static readonly TexlFunction VarP = _library.Append(new VarPFunction());
        public static readonly TexlFunction VarPT = _library.Append(new VarPTableFunction());
        public static readonly TexlFunction Text = _library.Append(new TextFunction());
        public static readonly TexlFunction Weekday = _library.Append(new WeekdayFunction());
        public static readonly TexlFunction WeekdaysLong = _library.Append(new WeekdaysLongFunction());
        public static readonly TexlFunction WeekdaysShort = _library.Append(new WeekdaysShortFunction());
        public static readonly TexlFunction WeekNum = _library.Append(new WeekNumFunction());
        public static readonly TexlFunction With = _library.Append(new WithFunction());
        public static readonly TexlFunction Year = _library.Append(new YearFunction());

        // NOTE: These functions should not be part of the core library until they are implemented in all runtimes
        public static readonly TexlFunction Index_UO = new IndexFunction_UO();
        public static readonly TexlFunction ParseJson = new ParseJsonFunction();
        public static readonly TexlFunction Table_UO = new TableFunction_UO();
        public static readonly TexlFunction Text_UO = new TextFunction_UO();
        public static readonly TexlFunction Value_UO = new ValueFunction_UO();
        public static readonly TexlFunction Boolean = new BooleanFunction();
        public static readonly TexlFunction Boolean_UO = new BooleanFunction_UO();
        public static readonly TexlFunction StringInterpolation = new StringInterpolationFunction();

        public static readonly TexlFunction IsUTCToday = new IsUTCTodayFunction();
        public static readonly TexlFunction UTCNow = new UTCNowFunction();
        public static readonly TexlFunction UTCToday = new UTCTodayFunction();
    }
}
