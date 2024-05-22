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
        // This is the list of Power Apps functions that aren't supported/implemeted in Power Fx
        // Binder will recognize these functions names and return a "recognized but not yet supported function" message 
        // instead of the classic "unknown or unsupported function".
        internal static readonly IReadOnlyCollection<string> OtherKnownFunctions = new HashSet<string>()
        {
            "Assert", "Back", "Choices", "ClearData", "Concurrent", "Confirm", "Copy", "DataSourceInfo", "Defaults", "Disable", "Distinct", "Download", "EditForm", "Enable", "Errors", "Exit", "GUID",
            "GroupBy", "HashTags", "IsMatch", "IsType", "JSON", "Launch", "LoadData", "Match", "MatchAll", "Navigate", "NewForm", "Notify", "PDF", "Param", "Pending", "Print", "ReadNFC",
            "RecordInfo", "Relate", "RemoveAll", "RemoveIf", "RequestHide", "Reset", "ResetForm", "Revert", "SaveData", "ScanBarcode", "Select", "SetFocus",
            "SetProperty", "ShowColumns", "State", "SubmitForm", "TraceValue", "Ungroup", "Unrelate", "Update", "UpdateContext", "UpdateIf", "User", "Validate", "ValidateRecord", "ViewForm",
            "Collect", "Clear", "Patch", "Remove", "ClearCollect", "Set"
        };

        // Functions in this list are shared and may show up in other hosts by default.
        internal static readonly TexlFunctionSet _library = new TexlFunctionSet(new List<TexlFunction>()
        {
            Abs,
            AbsT,
            Acos,
            AcosT,
            Acot,
            AcotT,
            AddColumns,
            AmPm,
            AmPmShort,
            And,
            Asin,
            AsinT,
            AsType,
            Atan,
            Atan2,
            AtanT,
            Average,
            AverageT,
            Blank,
            Boolean,
            Boolean_T,
            Boolean_UO,
            BooleanB,
            BooleanB_T,
            BooleanN,
            BooleanN_T,
            BooleanW,
            BooleanW_T,
            Char,
            CharT,
            Clock24,
            Coalesce,
            ColorFade,
            ColorFadeT,
            ColorValue,
            ColorValue_UO,
            Column_UO,
            ColumnNames_UO,
            Concat,
            Concatenate,
            ConcatenateT,
            Cos,
            CosT,
            Cot,
            CotT,
            Count,
            CountA,
            CountIf,
            CountRows,
            CountRows_UO,
            Date,
            DateAdd,
            DateAddT,
            DateDiff,
            DateDiffT,
            DateTime,
            DateTimeValue,
            DateTimeValue_UO,
            DateValue,
            DateValue_UO,
            Day,
            Dec2Hex,
            Dec2HexT,
            Degrees,
            DegreesT,
            DropColumns,
            EDate,
            EncodeHTML,
            EncodeUrl,
            EndsWith,
            EOMonth,
            Error,
            Exp,
            ExpT,
            Filter,
            Find,
            FindT,
            First,
            First_UO,
            FirstN,
            FirstN_UO,
            ForAll,
            ForAll_UO,
            GUID_UO,
            GUIDNoArg,
            GUIDPure,
            Hex2Dec,
            Hex2DecT,
            Hour,
            If,
            IfError,
            Index,
            Index_UO,
            Int,
            IntT,
            IsBlank,
            IsBlankOptionSetValue,
            IsBlankOrError,
            IsBlankOrErrorOptionSetValue,
            IsEmpty,
            IsError,
            IsNumeric,
            ISOWeekNum,
            IsToday,
            Language,
            Last,
            Last_UO,
            LastN,
            LastN_UO,
            Left,
            LeftST,
            LeftTS,
            LeftTT,
            Len,
            LenT,
            Ln,
            LnT,
            Log,
            LogT,
            LookUp,
            Lower,
            LowerT,
            Max,
            MaxT,
            Mid,
            MidT,
            Min,
            MinT,
            Minute,
            Mod,
            ModT,
            Month,
            MonthsLong,
            MonthsShort,
            Not,
            Now,
            Or,
            ParseJSON,
            PatchRecord,
            Pi,
            PlainText,
            Power,
            PowerT,
            Proper,
            ProperT,
            Radians,
            RadiansT,
            Rand,
            RandBetween,
            Refresh,
            RenameColumns,
            Replace,
            ReplaceT,
            RGBA,
            Right,
            RightST,
            RightTS,
            RightTT,
            Round,
            RoundDown,
            RoundDownT,
            RoundT,
            RoundUp,
            RoundUpT,
            Search,
            Second,
            Sequence,
            ShowColumns,
            Shuffle,
            Sin,
            SinT,
            Sort,
            SortByColumns,
            Split,
            Sqrt,
            SqrtT,
            StartsWith,
            StdevP,
            StdevPT,
            Substitute,
            SubstituteT,
            Sum,
            SumT,
            Switch,
            Table,
            Table_UO,
            Tan,
            TanT,
            Text,
            Text_UO,
            Time,
            TimeValue,
            TimeValue_UO,
            TimeZoneOffset,
            Today,
            Trace,
            Trim,
            TrimEnds,
            TrimEndsT,
            TrimT,
            Trunc,
            TruncT,
            UniChar,
            UniCharT,
            Upper,
            UpperT,
            Value,
            Value_UO,
            VarP,
            VarPT,
            Weekday,
            WeekdaysLong,
            WeekdaysShort,
            WeekNum,
            With,
            Year
        });

        public static readonly TexlFunction Abs = new AbsFunction();
        public static readonly TexlFunction AbsT = new AbsTableFunction();
        public static readonly TexlFunction Acos = new AcosFunction();
        public static readonly TexlFunction AcosT = new AcosTableFunction();
        public static readonly TexlFunction Acot = new AcotFunction();
        public static readonly TexlFunction AcotT = new AcotTableFunction();
        public static readonly TexlFunction AddColumns = new AddColumnsFunction();
        public static readonly TexlFunction AmPm = new AmPmFunction();
        public static readonly TexlFunction AmPmShort = new AmPmShortFunction();
        public static readonly TexlFunction And = new VariadicLogicalFunction(isAnd: true);
        public static readonly TexlFunction Asin = new AsinFunction();
        public static readonly TexlFunction AsinT = new AsinTableFunction();
        public static readonly TexlFunction AsType = new AsTypeFunction();
        public static readonly TexlFunction Atan = new AtanFunction();
        public static readonly TexlFunction Atan2 = new Atan2Function();
        public static readonly TexlFunction AtanT = new AtanTableFunction();
        public static readonly TexlFunction Average = new AverageFunction();
        public static readonly TexlFunction AverageT = new AverageTableFunction();
        public static readonly TexlFunction Blank = new BlankFunction();
        public static readonly TexlFunction Boolean = new BooleanFunction();
        public static readonly TexlFunction Boolean_T = new BooleanFunction_T();
        public static readonly TexlFunction Boolean_UO = new BooleanFunction_UO();
        public static readonly TexlFunction BooleanB = new BooleanBFunction();
        public static readonly TexlFunction BooleanB_T = new BooleanBFunction_T();
        public static readonly TexlFunction BooleanN = new BooleanNFunction();
        public static readonly TexlFunction BooleanN_T = new BooleanNFunction_T();
        public static readonly TexlFunction BooleanW = new BooleanWFunction();
        public static readonly TexlFunction BooleanW_T = new BooleanWFunction_T();
        public static readonly TexlFunction Char = new CharFunction();
        public static readonly TexlFunction CharT = new CharTFunction();
        public static readonly TexlFunction Clock24 = new IsClock24Function();
        public static readonly TexlFunction Coalesce = new CoalesceFunction();
        public static readonly TexlFunction ColorFade = new ColorFadeFunction();
        public static readonly TexlFunction ColorFadeT = new ColorFadeTFunction();
        public static readonly TexlFunction ColorValue = new ColorValueFunction();
        public static readonly TexlFunction ColorValue_UO = new ColorValueFunction_UO();
        public static readonly TexlFunction Column_UO = new ColumnFunction_UO();
        public static readonly TexlFunction ColumnNames_UO = new ColumnNamesFunction_UO();
        public static readonly TexlFunction Concat = new ConcatFunction();
        public static readonly TexlFunction Concatenate = new ConcatenateFunction();
        public static readonly TexlFunction ConcatenateT = new ConcatenateTableFunction();
        public static readonly TexlFunction Cos = new CosFunction();
        public static readonly TexlFunction CosT = new CosTableFunction();
        public static readonly TexlFunction Cot = new CotFunction();
        public static readonly TexlFunction CotT = new CotTableFunction();
        public static readonly TexlFunction Count = new CountFunction();
        public static readonly TexlFunction CountA = new CountAFunction();
        public static readonly TexlFunction CountIf = new CountIfFunction();
        public static readonly TexlFunction CountRows = new CountRowsFunction();
        public static readonly TexlFunction CountRows_UO = new CountRowsFunction_UO();
        public static readonly TexlFunction Date = new DateFunction();
        public static readonly TexlFunction DateAdd = new DateAddFunction();
        public static readonly TexlFunction DateAddT = new DateAddTFunction();
        public static readonly TexlFunction DateDiff = new DateDiffFunction();
        public static readonly TexlFunction DateDiffT = new DateDiffTFunction();
        public static readonly TexlFunction DateTime = new DateTimeFunction();
        public static readonly TexlFunction DateTimeValue = new DateTimeValueFunction();
        public static readonly TexlFunction DateTimeValue_UO = new DateTimeValueFunction_UO();
        public static readonly TexlFunction DateValue = new DateValueFunction();
        public static readonly TexlFunction DateValue_UO = new DateValueFunction_UO();
        public static readonly TexlFunction Day = new DayFunction();
        public static readonly TexlFunction Dec2Hex = new Dec2HexFunction();
        public static readonly TexlFunction Dec2HexT = new Dec2HexTFunction();
        public static readonly TexlFunction Degrees = new DegreesFunction();
        public static readonly TexlFunction DegreesT = new DegreesTableFunction();
        public static readonly TexlFunction DropColumns = new DropColumnsFunction();
        public static readonly TexlFunction EDate = new EDateFunction();
        public static readonly TexlFunction EncodeHTML = new EncodeHTMLFunction();
        public static readonly TexlFunction EncodeUrl = new EncodeUrlFunction();
        public static readonly TexlFunction EndsWith = new EndsWithFunction();
        public static readonly TexlFunction EOMonth = new EOMonthFunction();
        public static readonly TexlFunction Error = new ErrorFunction();
        public static readonly TexlFunction Exp = new ExpFunction();
        public static readonly TexlFunction ExpT = new ExpTableFunction();
        public static readonly TexlFunction Filter = new FilterFunction();
        public static readonly TexlFunction Find = new FindFunction();
        public static readonly TexlFunction FindT = new FindTFunction();
        public static readonly TexlFunction First = new FirstLastFunction(isFirst: true);
        public static readonly TexlFunction First_UO = new FirstLastFunction_UO(isFirst: true);
        public static readonly TexlFunction FirstN = new FirstLastNFunction(isFirst: true);
        public static readonly TexlFunction FirstN_UO = new FirstLastNFunction_UO(isFirst: true);
        public static readonly TexlFunction ForAll = new ForAllFunction();
        public static readonly TexlFunction ForAll_UO = new ForAllFunction_UO();
        public static readonly TexlFunction GUID_UO = new GUIDPureFunction_UO();
        public static readonly TexlFunction GUIDNoArg = new GUIDNoArgFunction();
        public static readonly TexlFunction GUIDPure = new GUIDPureFunction();
        public static readonly TexlFunction Hex2Dec = new Hex2DecFunction();
        public static readonly TexlFunction Hex2DecT = new Hex2DecTFunction();
        public static readonly TexlFunction Hour = new HourFunction();
        public static readonly TexlFunction If = new IfFunction();
        public static readonly TexlFunction IfError = new IfErrorFunction();
        public static readonly TexlFunction Index = new IndexFunction();
        public static readonly TexlFunction Index_UO = new IndexFunction_UO();
        public static readonly TexlFunction Int = new IntFunction();
        public static readonly TexlFunction IntT = new IntTableFunction();
        public static readonly TexlFunction IsBlank = new IsBlankFunction();
        public static readonly TexlFunction IsBlankOptionSetValue = new IsBlankOptionSetValueFunction();
        public static readonly TexlFunction IsBlankOrError = new IsBlankOrErrorFunction();
        public static readonly TexlFunction IsBlankOrErrorOptionSetValue = new IsBlankOrErrorOptionSetValueFunction();
        public static readonly TexlFunction IsEmpty = new IsEmptyFunction();
        public static readonly TexlFunction IsError = new IsErrorFunction();
        public static readonly TexlFunction IsNumeric = new IsNumericFunction();
        public static readonly TexlFunction ISOWeekNum = new ISOWeekNumFunction();
        public static readonly TexlFunction IsToday = new IsTodayFunction();
        public static readonly TexlFunction Language = new LanguageFunction();
        public static readonly TexlFunction Last = new FirstLastFunction(isFirst: false);
        public static readonly TexlFunction Last_UO = new FirstLastFunction_UO(isFirst: false);
        public static readonly TexlFunction LastN = new FirstLastNFunction(isFirst: false);
        public static readonly TexlFunction LastN_UO = new FirstLastNFunction_UO(isFirst: false);
        public static readonly TexlFunction Left = new LeftRightScalarFunction(isLeft: true);
        public static readonly TexlFunction LeftST = new LeftRightScalarTableFunction(isLeft: true);
        public static readonly TexlFunction LeftTS = new LeftRightTableScalarFunction(isLeft: true);
        public static readonly TexlFunction LeftTT = new LeftRightTableTableFunction(isLeft: true);
        public static readonly TexlFunction Len = new LenFunction();
        public static readonly TexlFunction LenT = new LenTFunction();
        public static readonly TexlFunction Ln = new LnFunction();
        public static readonly TexlFunction LnT = new LnTableFunction();
        public static readonly TexlFunction Log = new LogFunction();
        public static readonly TexlFunction LogT = new LogTFunction();
        public static readonly TexlFunction LookUp = new LookUpFunction();
        public static readonly TexlFunction Lower = new LowerUpperFunction(isLower: true);
        public static readonly TexlFunction LowerT = new LowerUpperTFunction(isLower: true);
        public static readonly TexlFunction Max = new MinMaxFunction(isMin: false);
        public static readonly TexlFunction MaxT = new MinMaxTableFunction(isMin: false);
        public static readonly TexlFunction Mid = new MidFunction();
        public static readonly TexlFunction MidT = new MidTFunction();
        public static readonly TexlFunction Min = new MinMaxFunction(isMin: true);
        public static readonly TexlFunction MinT = new MinMaxTableFunction(isMin: true);
        public static readonly TexlFunction Minute = new MinuteFunction();
        public static readonly TexlFunction Mod = new ModFunction();
        public static readonly TexlFunction ModT = new ModTFunction();
        public static readonly TexlFunction Month = new MonthFunction();
        public static readonly TexlFunction MonthsLong = new MonthsLongFunction();
        public static readonly TexlFunction MonthsShort = new MonthsShortFunction();
        public static readonly TexlFunction Not = new NotFunction();
        public static readonly TexlFunction Now = new NowFunction();
        public static readonly TexlFunction Or = new VariadicLogicalFunction(isAnd: false);
        public static readonly TexlFunction ParseJSON = new ParseJSONFunction();
        public static readonly TexlFunction PatchRecord = new PatchRecordFunction();
        public static readonly TexlFunction Pi = new PiFunction();
        public static readonly TexlFunction PlainText = new PlainTextFunction();
        public static readonly TexlFunction Power = new PowerFunction();
        public static readonly TexlFunction PowerT = new PowerTFunction();
        public static readonly TexlFunction Proper = new ProperFunction();
        public static readonly TexlFunction ProperT = new ProperTFunction();
        public static readonly TexlFunction Radians = new RadiansFunction();
        public static readonly TexlFunction RadiansT = new RadiansTableFunction();
        public static readonly TexlFunction Rand = new RandFunction();
        public static readonly TexlFunction RandBetween = new RandBetweenFunction();
        public static readonly TexlFunction Refresh = new RefreshFunction();
        public static readonly TexlFunction RenameColumns = new RenameColumnsFunction();
        public static readonly TexlFunction Replace = new ReplaceFunction();
        public static readonly TexlFunction ReplaceT = new ReplaceTFunction();
        public static readonly TexlFunction RGBA = new RGBAFunction();
        public static readonly TexlFunction Right = new LeftRightScalarFunction(isLeft: false);
        public static readonly TexlFunction RightST = new LeftRightScalarTableFunction(isLeft: false);
        public static readonly TexlFunction RightTS = new LeftRightTableScalarFunction(isLeft: false);
        public static readonly TexlFunction RightTT = new LeftRightTableTableFunction(isLeft: false);
        public static readonly TexlFunction Round = new RoundScalarFunction();
        public static readonly TexlFunction RoundDown = new RoundDownScalarFunction();
        public static readonly TexlFunction RoundDownT = new RoundDownTableFunction();
        public static readonly TexlFunction RoundT = new RoundTableFunction();
        public static readonly TexlFunction RoundUp = new RoundUpScalarFunction();
        public static readonly TexlFunction RoundUpT = new RoundUpTableFunction();
        public static readonly TexlFunction Search = new SearchFunction();
        public static readonly TexlFunction Second = new SecondFunction();
        public static readonly TexlFunction Sequence = new SequenceFunction();
        public static readonly TexlFunction ShowColumns = new ShowColumnsFunction();
        public static readonly TexlFunction Shuffle = new ShuffleFunction();
        public static readonly TexlFunction Sin = new SinFunction();
        public static readonly TexlFunction SinT = new SinTableFunction();
        public static readonly TexlFunction Sort = new SortFunction();
        public static readonly TexlFunction SortByColumns = new SortByColumnsFunction();
        public static readonly TexlFunction Split = new SplitFunction();
        public static readonly TexlFunction Sqrt = new SqrtFunction();
        public static readonly TexlFunction SqrtT = new SqrtTableFunction();
        public static readonly TexlFunction StartsWith = new StartsWithFunction();
        public static readonly TexlFunction StdevP = new StdevPFunction();
        public static readonly TexlFunction StdevPT = new StdevPTableFunction();
        public static readonly TexlFunction Substitute = new SubstituteFunction();
        public static readonly TexlFunction SubstituteT = new SubstituteTFunction();
        public static readonly TexlFunction Sum = new SumFunction();
        public static readonly TexlFunction SumT = new SumTableFunction();
        public static readonly TexlFunction Switch = new SwitchFunction();
        public static readonly TexlFunction Table = new TableFunction();
        public static readonly TexlFunction Table_UO = new TableFunction_UO();
        public static readonly TexlFunction Tan = new TanFunction();
        public static readonly TexlFunction TanT = new TanTableFunction();
        public static readonly TexlFunction Text = new TextFunction();
        public static readonly TexlFunction Text_UO = new TextFunction_UO();
        public static readonly TexlFunction Time = new TimeFunction();
        public static readonly TexlFunction TimeValue = new TimeValueFunction();
        public static readonly TexlFunction TimeValue_UO = new TimeValueFunction_UO();
        public static readonly TexlFunction TimeZoneOffset = new TimeZoneOffsetFunction();
        public static readonly TexlFunction Today = new TodayFunction();
        public static readonly TexlFunction Trace = new TraceFunction();
        public static readonly TexlFunction Trim = new TrimFunction();
        public static readonly TexlFunction TrimEnds = new TrimEndsFunction();
        public static readonly TexlFunction TrimEndsT = new TrimEndsTFunction();
        public static readonly TexlFunction TrimT = new TrimTFunction();
        public static readonly TexlFunction Trunc = new TruncFunction();
        public static readonly TexlFunction TruncT = new TruncTableFunction();
        public static readonly TexlFunction UniChar = new UniCharFunction();
        public static readonly TexlFunction UniCharT = new UniCharTFunction();
        public static readonly TexlFunction Upper = new LowerUpperFunction(isLower: false);
        public static readonly TexlFunction UpperT = new LowerUpperTFunction(isLower: false);
        public static readonly TexlFunction Value = new ValueFunction();
        public static readonly TexlFunction Value_UO = new ValueFunction_UO();
        public static readonly TexlFunction VarP = new VarPFunction();
        public static readonly TexlFunction VarPT = new VarPTableFunction();
        public static readonly TexlFunction Weekday = new WeekdayFunction();
        public static readonly TexlFunction WeekdaysLong = new WeekdaysLongFunction();
        public static readonly TexlFunction WeekdaysShort = new WeekdaysShortFunction();
        public static readonly TexlFunction WeekNum = new WeekNumFunction();
        public static readonly TexlFunction With = new WithFunction();
        public static readonly TexlFunction Year = new YearFunction();

        // Don't add new functions here, follow alpha order

        // _featureGateFunctions functions, not present in all platforms
        internal static readonly TexlFunctionSet _featureGateFunctions = new TexlFunctionSet(new List<TexlFunction>()
        {
            BooleanL,
            BooleanL_T,
            Decimal,
            Decimal_UO,
            Float,
            Float_UO,
            IsUTCToday,
            Summarize,
            UTCNow,
            UTCToday
        });

        public static readonly TexlFunction BooleanL = new BooleanLFunction();
        public static readonly TexlFunction BooleanL_T = new BooleanLFunction_T();
        public static readonly TexlFunction Decimal = new DecimalFunction();
        public static readonly TexlFunction Decimal_UO = new DecimalFunction_UO();
        public static readonly TexlFunction Float = new FloatFunction();
        public static readonly TexlFunction Float_UO = new FloatFunction_UO();
        public static readonly TexlFunction IsUTCToday = new IsUTCTodayFunction();
        public static readonly TexlFunction Summarize = new SummarizeFunction();
        public static readonly TexlFunction UTCNow = new UTCNowFunction();
        public static readonly TexlFunction UTCToday = new UTCTodayFunction();

        // Slow API, only use for backward compatibility
#pragma warning disable CS0618 // Type or member is obsolete        
        public static IEnumerable<TexlFunction> BuiltinFunctionsLibrary => _library.Functions;

        private static readonly TexlFunctionSet _testOnlyLibrary = new TexlFunctionSet(_library.Functions).Add(_featureGateFunctions);

        // Slow API, only use for backward compatibility
        internal static IEnumerable<TexlFunction> TestOnly_AllBuiltinFunctions => _testOnlyLibrary.Functions;
#pragma warning restore CS0618 // Type or member is obsolete

        public static bool IsKnownPublicFunction(string functionName)
        {
            if (_library.AnyWithName(functionName) || OtherKnownFunctions.Contains(functionName) || _featureGateFunctions.AnyWithName(functionName))
            {
                return true;
            }

            return false;
        }
    }
}
