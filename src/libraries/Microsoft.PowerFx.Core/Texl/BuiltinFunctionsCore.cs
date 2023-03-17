// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Texl.Builtins;

namespace Microsoft.PowerFx.Core.Texl
{
    // List of TexlFunction instances in Power Fx core.
    // - no ControlInfo dependency 
    // - just functions that are ported over to Language.Core
    internal class BuiltinFunctionsCore
    {        
        // Functions in this list are shared and may show up in other hosts by default.
        internal static readonly TexlFunctionSet _library = new TexlFunctionSet();
        private static readonly TexlFunctionSet _featureGateFunctions = new TexlFunctionSet();

        public static readonly TexlFunction AmPm = _library.Add(new AmPmFunction());
        public static readonly TexlFunction AmPmShort = _library.Add(new AmPmShortFunction());
        public static readonly TexlFunction Abs = _library.Add(new AbsFunction());
        public static readonly TexlFunction AbsT = _library.Add(new AbsTableFunction());
        public static readonly TexlFunction Acos = _library.Add(new AcosFunction());
        public static readonly TexlFunction AcosT = _library.Add(new AcosTableFunction());
        public static readonly TexlFunction Acot = _library.Add(new AcotFunction());
        public static readonly TexlFunction AcotT = _library.Add(new AcotTableFunction());
        public static readonly TexlFunction AddColumns = _library.Add(new AddColumnsFunction());
        public static readonly TexlFunction And = _library.Add(new VariadicLogicalFunction(isAnd: true));
        public static readonly TexlFunction Asin = _library.Add(new AsinFunction());
        public static readonly TexlFunction AsinT = _library.Add(new AsinTableFunction());
        public static readonly TexlFunction AsType = _library.Add(new AsTypeFunction());
        public static readonly TexlFunction Atan = _library.Add(new AtanFunction());
        public static readonly TexlFunction AtanT = _library.Add(new AtanTableFunction());
        public static readonly TexlFunction Atan2 = _library.Add(new Atan2Function());
        public static readonly TexlFunction Average = _library.Add(new AverageFunction());
        public static readonly TexlFunction AverageT = _library.Add(new AverageTableFunction());
        public static readonly TexlFunction Blank = _library.Add(new BlankFunction());
        public static readonly TexlFunction Boolean = _library.Add(new BooleanFunction());
        public static readonly TexlFunction Boolean_T = _library.Add(new BooleanFunction_T());
        public static readonly TexlFunction BooleanN = _library.Add(new BooleanNFunction());
        public static readonly TexlFunction BooleanN_T = _library.Add(new BooleanNFunction_T());
        public static readonly TexlFunction BooleanB = _library.Add(new BooleanBFunction());
        public static readonly TexlFunction BooleanB_T = _library.Add(new BooleanBFunction_T());
        public static readonly TexlFunction Boolean_UO = _library.Add(new BooleanFunction_UO());
        public static readonly TexlFunction Clock24 = _library.Add(new IsClock24Function());
        public static readonly TexlFunction Char = _library.Add(new CharFunction());
        public static readonly TexlFunction CharT = _library.Add(new CharTFunction());
        public static readonly TexlFunction Coalesce = _library.Add(new CoalesceFunction());
        public static readonly TexlFunction ColorFade = _library.Add(new ColorFadeFunction());
        public static readonly TexlFunction ColorFadeT = _library.Add(new ColorFadeTFunction());
        public static readonly TexlFunction ColorValue = _library.Add(new ColorValueFunction());
        public static readonly TexlFunction ColorValue_UO = _library.Add(new ColorValueFunction_UO());
        public static readonly TexlFunction Concat = _library.Add(new ConcatFunction());
        public static readonly TexlFunction Concatenate = _library.Add(new ConcatenateFunction());
        public static readonly TexlFunction ConcatenateT = _library.Add(new ConcatenateTableFunction());
        public static readonly TexlFunction Cos = _library.Add(new CosFunction());
        public static readonly TexlFunction CosT = _library.Add(new CosTableFunction());
        public static readonly TexlFunction Cot = _library.Add(new CotFunction());
        public static readonly TexlFunction CotT = _library.Add(new CotTableFunction());
        public static readonly TexlFunction Count = _library.Add(new CountFunction());
        public static readonly TexlFunction CountA = _library.Add(new CountAFunction());
        public static readonly TexlFunction CountIf = _library.Add(new CountIfFunction());
        public static readonly TexlFunction CountRows = _library.Add(new CountRowsFunction());
        public static readonly TexlFunction CountRows_UO = _library.Add(new CountRowsFunction_UO());
        public static readonly TexlFunction Date = _library.Add(new DateFunction());
        public static readonly TexlFunction DateAdd = _library.Add(new DateAddFunction());
        public static readonly TexlFunction DateAddT = _library.Add(new DateAddTFunction());
        public static readonly TexlFunction DateDiff = _library.Add(new DateDiffFunction());
        public static readonly TexlFunction DateDiffT = _library.Add(new DateDiffTFunction());
        public static readonly TexlFunction DateTime = _library.Add(new DateTimeFunction());
        public static readonly TexlFunction DateTimeValue = _library.Add(new DateTimeValueFunction());
        public static readonly TexlFunction DateTimeValue_UO = _library.Add(new DateTimeValueFunction_UO());
        public static readonly TexlFunction DateValue = _library.Add(new DateValueFunction());
        public static readonly TexlFunction DateValue_UO = _library.Add(new DateValueFunction_UO());
        public static readonly TexlFunction Day = _library.Add(new DayFunction());
        public static readonly TexlFunction Dec2Hex = _library.Add(new Dec2HexFunction());
        public static readonly TexlFunction Dec2HexT = _library.Add(new Dec2HexTFunction());
        public static readonly TexlFunction Degrees = _library.Add(new DegreesFunction());
        public static readonly TexlFunction DegreesT = _library.Add(new DegreesTableFunction());
        public static readonly TexlFunction DropColumns = _library.Add(new DropColumnsFunction());
        public static readonly TexlFunction EncodeUrl = _library.Add(new EncodeUrlFunction());
        public static readonly TexlFunction EndsWith = _library.Add(new EndsWithFunction());
        public static readonly TexlFunction Error = _library.Add(new ErrorFunction());
        public static readonly TexlFunction Exp = _library.Add(new ExpFunction());
        public static readonly TexlFunction ExpT = _library.Add(new ExpTableFunction());
        public static readonly TexlFunction Filter = _library.Add(new FilterFunction());
        public static readonly TexlFunction Find = _library.Add(new FindFunction());
        public static readonly TexlFunction FindT = _library.Add(new FindTFunction());
        public static readonly TexlFunction First = _library.Add(new FirstLastFunction(isFirst: true));
        public static readonly TexlFunction FirstN = _library.Add(new FirstLastNFunction(isFirst: true));
        public static readonly TexlFunction First_UO = _library.Add(new FirstLastFunction_UO(isFirst: true));
        public static readonly TexlFunction FirstN_UO = _library.Add(new FirstLastNFunction_UO(isFirst: true));
        public static readonly TexlFunction ForAll = _library.Add(new ForAllFunction());
        public static readonly TexlFunction ForAll_UO = _library.Add(new ForAllFunction_UO());
        public static readonly TexlFunction GUIDPure = _library.Add(new GUIDPureFunction());
        public static readonly TexlFunction GUID_UO = _library.Add(new GUIDPureFunction_UO());
        public static readonly TexlFunction Hex2Dec = _library.Add(new Hex2DecFunction());
        public static readonly TexlFunction Hex2DecT = _library.Add(new Hex2DecTFunction());
        public static readonly TexlFunction Hour = _library.Add(new HourFunction());
        public static readonly TexlFunction If = _library.Add(new IfFunction());
        public static readonly TexlFunction IfError = _library.Add(new IfErrorFunction());
        public static readonly TexlFunction Index = _library.Add(new IndexFunction());
        public static readonly TexlFunction Index_UO = _library.Add(new IndexFunction_UO());
        public static readonly TexlFunction Int = _library.Add(new IntFunction());
        public static readonly TexlFunction IntT = _library.Add(new IntTableFunction());
        public static readonly TexlFunction IsBlank = _library.Add(new IsBlankFunction());
        public static readonly TexlFunction IsBlankOptionSetValue = _library.Add(new IsBlankOptionSetValueFunction());
        public static readonly TexlFunction IsBlankOrError = _library.Add(new IsBlankOrErrorFunction());
        public static readonly TexlFunction IsBlankOrErrorOptionSetValue = _library.Add(new IsBlankOrErrorOptionSetValueFunction());
        public static readonly TexlFunction IsEmpty = _library.Add(new IsEmptyFunction());
        public static readonly TexlFunction IsError = _library.Add(new IsErrorFunction());
        public static readonly TexlFunction IsToday = _library.Add(new IsTodayFunction());
        public static readonly TexlFunction IsNumeric = _library.Add(new IsNumericFunction());
        public static readonly TexlFunction ISOWeekNum = _library.Add(new ISOWeekNumFunction());
        public static readonly TexlFunction Language = _library.Add(new LanguageFunction());
        public static readonly TexlFunction Last = _library.Add(new FirstLastFunction(isFirst: false));
        public static readonly TexlFunction LastN = _library.Add(new FirstLastNFunction(isFirst: false));
        public static readonly TexlFunction Last_UO = _library.Add(new FirstLastFunction_UO(isFirst: false));
        public static readonly TexlFunction LastN_UO = _library.Add(new FirstLastNFunction_UO(isFirst: false));
        public static readonly TexlFunction Left = _library.Add(new LeftRightScalarFunction(isLeft: true));
        public static readonly TexlFunction LeftTS = _library.Add(new LeftRightTableScalarFunction(isLeft: true));
        public static readonly TexlFunction LeftTT = _library.Add(new LeftRightTableTableFunction(isLeft: true));
        public static readonly TexlFunction LeftST = _library.Add(new LeftRightScalarTableFunction(isLeft: true));
        public static readonly TexlFunction Len = _library.Add(new LenFunction());
        public static readonly TexlFunction LenT = _library.Add(new LenTFunction());
        public static readonly TexlFunction Ln = _library.Add(new LnFunction());
        public static readonly TexlFunction LnT = _library.Add(new LnTableFunction());
        public static readonly TexlFunction Log = _library.Add(new LogFunction());
        public static readonly TexlFunction LogT = _library.Add(new LogTFunction());
        public static readonly TexlFunction LookUp = _library.Add(new LookUpFunction());
        public static readonly TexlFunction Lower = _library.Add(new LowerUpperFunction(isLower: true));
        public static readonly TexlFunction LowerT = _library.Add(new LowerUpperTFunction(isLower: true));
        public static readonly TexlFunction Max = _library.Add(new MinMaxFunction(isMin: false));
        public static readonly TexlFunction MaxT = _library.Add(new MinMaxTableFunction(isMin: false));
        public static readonly TexlFunction Mid = _library.Add(new MidFunction());
        public static readonly TexlFunction MidT = _library.Add(new MidTFunction());
        public static readonly TexlFunction Min = _library.Add(new MinMaxFunction(isMin: true));
        public static readonly TexlFunction MinT = _library.Add(new MinMaxTableFunction(isMin: true));
        public static readonly TexlFunction Minute = _library.Add(new MinuteFunction());
        public static readonly TexlFunction Mod = _library.Add(new ModFunction());
        public static readonly TexlFunction ModT = _library.Add(new ModTFunction());
        public static readonly TexlFunction Month = _library.Add(new MonthFunction());
        public static readonly TexlFunction MonthsLong = _library.Add(new MonthsLongFunction());
        public static readonly TexlFunction MonthsShort = _library.Add(new MonthsShortFunction());
        public static readonly TexlFunction Not = _library.Add(new NotFunction());
        public static readonly TexlFunction Now = _library.Add(new NowFunction());
        public static readonly TexlFunction OptionsSetInfo = _featureGateFunctions.Add(new OptionSetInfoFunction());
        public static readonly TexlFunction Or = _library.Add(new VariadicLogicalFunction(isAnd: false));
        public static readonly TexlFunction ParseJSON = _library.Add(new ParseJSONFunction());
        public static readonly TexlFunction Power = _library.Add(new PowerFunction());
        public static readonly TexlFunction PowerT = _library.Add(new PowerTFunction());
        public static readonly TexlFunction Pi = _library.Add(new PiFunction());
        public static readonly TexlFunction Proper = _library.Add(new ProperFunction());
        public static readonly TexlFunction ProperT = _library.Add(new ProperTFunction());
        public static readonly TexlFunction Radians = _library.Add(new RadiansFunction());
        public static readonly TexlFunction RadiansT = _library.Add(new RadiansTableFunction());
        public static readonly TexlFunction Rand = _library.Add(new RandFunction());
        public static readonly TexlFunction RandBetween = _library.Add(new RandBetweenFunction());
        public static readonly TexlFunction Replace = _library.Add(new ReplaceFunction());
        public static readonly TexlFunction ReplaceT = _library.Add(new ReplaceTFunction());
        public static readonly TexlFunction RGBA = _library.Add(new RGBAFunction());
        public static readonly TexlFunction Right = _library.Add(new LeftRightScalarFunction(isLeft: false));
        public static readonly TexlFunction RightTS = _library.Add(new LeftRightTableScalarFunction(isLeft: false));
        public static readonly TexlFunction RightTT = _library.Add(new LeftRightTableTableFunction(isLeft: false));
        public static readonly TexlFunction RightST = _library.Add(new LeftRightScalarTableFunction(isLeft: false));
        public static readonly TexlFunction Round = _library.Add(new RoundScalarFunction());
        public static readonly TexlFunction RoundT = _library.Add(new RoundTableFunction());
        public static readonly TexlFunction RoundDown = _library.Add(new RoundDownScalarFunction());
        public static readonly TexlFunction RoundDownT = _library.Add(new RoundDownTableFunction());
        public static readonly TexlFunction RoundUp = _library.Add(new RoundUpScalarFunction());
        public static readonly TexlFunction RoundUpT = _library.Add(new RoundUpTableFunction());
        public static readonly TexlFunction Second = _library.Add(new SecondFunction());
        public static readonly TexlFunction Sequence = _library.Add(new SequenceFunction());
        public static readonly TexlFunction Shuffle = _library.Add(new ShuffleFunction());
        public static readonly TexlFunction Sin = _library.Add(new SinFunction());
        public static readonly TexlFunction Sort = _library.Add(new SortFunction());
        public static readonly TexlFunction SortByColumns = _library.Add(new SortByColumnsFunction());
        public static readonly TexlFunction SortByColumnsOrderTable = _library.Add(new SortByColumnsOrderTableFunction());
        public static readonly TexlFunction SinT = _library.Add(new SinTableFunction());
        public static readonly TexlFunction Split = _library.Add(new SplitFunction());
        public static readonly TexlFunction Sqrt = _library.Add(new SqrtFunction());
        public static readonly TexlFunction SqrtT = _library.Add(new SqrtTableFunction());
        public static readonly TexlFunction StartsWith = _library.Add(new StartsWithFunction());
        public static readonly TexlFunction StdevP = _library.Add(new StdevPFunction());
        public static readonly TexlFunction StdevPT = _library.Add(new StdevPTableFunction());
        public static readonly TexlFunction Substitute = _library.Add(new SubstituteFunction());
        public static readonly TexlFunction SubstituteT = _library.Add(new SubstituteTFunction());
        public static readonly TexlFunction Sum = _library.Add(new SumFunction());
        public static readonly TexlFunction SumT = _library.Add(new SumTableFunction());
        public static readonly TexlFunction Switch = _library.Add(new SwitchFunction());
        public static readonly TexlFunction Table = _library.Add(new TableFunction());
        public static readonly TexlFunction Table_UO = _library.Add(new TableFunction_UO());
        public static readonly TexlFunction Tan = _library.Add(new TanFunction());
        public static readonly TexlFunction TanT = _library.Add(new TanTableFunction());
        public static readonly TexlFunction Time = _library.Add(new TimeFunction());
        public static readonly TexlFunction TimeValue = _library.Add(new TimeValueFunction());
        public static readonly TexlFunction TimeValue_UO = _library.Add(new TimeValueFunction_UO());
        public static readonly TexlFunction TimeZoneOffset = _library.Add(new TimeZoneOffsetFunction());
        public static readonly TexlFunction Today = _library.Add(new TodayFunction());
        public static readonly TexlFunction Trim = _library.Add(new TrimFunction());
        public static readonly TexlFunction TrimT = _library.Add(new TrimTFunction());
        public static readonly TexlFunction TrimEnds = _library.Add(new TrimEndsFunction());
        public static readonly TexlFunction TrimEndsT = _library.Add(new TrimEndsTFunction());
        public static readonly TexlFunction Trunc = _library.Add(new TruncFunction());
        public static readonly TexlFunction TruncT = _library.Add(new TruncTableFunction());
        public static readonly TexlFunction Upper = _library.Add(new LowerUpperFunction(isLower: false));
        public static readonly TexlFunction UpperT = _library.Add(new LowerUpperTFunction(isLower: false));
        public static readonly TexlFunction Value = _library.Add(new ValueFunction());
        public static readonly TexlFunction Value_UO = _library.Add(new ValueFunction_UO());
        public static readonly TexlFunction VarP = _library.Add(new VarPFunction());
        public static readonly TexlFunction VarPT = _library.Add(new VarPTableFunction());
        public static readonly TexlFunction Text = _library.Add(new TextFunction());
        public static readonly TexlFunction Text_UO = _library.Add(new TextFunction_UO());
        public static readonly TexlFunction Weekday = _library.Add(new WeekdayFunction());
        public static readonly TexlFunction WeekdaysLong = _library.Add(new WeekdaysLongFunction());
        public static readonly TexlFunction WeekdaysShort = _library.Add(new WeekdaysShortFunction());
        public static readonly TexlFunction WeekNum = _library.Add(new WeekNumFunction());
        public static readonly TexlFunction With = _library.Add(new WithFunction());
        public static readonly TexlFunction Year = _library.Add(new YearFunction());

        public static readonly TexlFunction IsUTCToday = _featureGateFunctions.Add(new IsUTCTodayFunction());
        public static readonly TexlFunction UTCNow = _featureGateFunctions.Add(new UTCNowFunction());
        public static readonly TexlFunction UTCToday = _featureGateFunctions.Add(new UTCTodayFunction());

        // Slow API, only use for backward compatibility
#pragma warning disable CS0618 // Type or member is obsolete        
        public static IEnumerable<TexlFunction> BuiltinFunctionsLibrary => _library.Functions;
        
        private static readonly TexlFunctionSet _testOnlyLibrary = new TexlFunctionSet(_library.Functions).Add(_featureGateFunctions);

        // Slow API, only use for backward compatibility
        internal static IEnumerable<TexlFunction> TestOnly_AllBuiltinFunctions => _testOnlyLibrary.Functions;
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
