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
        internal static readonly TexlFunctionSet _library;

        public static readonly TexlFunction Abs;
        public static readonly TexlFunction AbsT;
        public static readonly TexlFunction Acos;
        public static readonly TexlFunction AcosT;
        public static readonly TexlFunction Acot;
        public static readonly TexlFunction AcotT;
        public static readonly TexlFunction AddColumns;
        public static readonly TexlFunction AmPm;
        public static readonly TexlFunction AmPmShort;
        public static readonly TexlFunction And;
        public static readonly TexlFunction Asin;
        public static readonly TexlFunction AsinT;
        public static readonly TexlFunction AsType;
        public static readonly TexlFunction Atan;
        public static readonly TexlFunction Atan2;
        public static readonly TexlFunction AtanT;
        public static readonly TexlFunction Average;
        public static readonly TexlFunction AverageT;
        public static readonly TexlFunction Blank;
        public static readonly TexlFunction Boolean;
        public static readonly TexlFunction Boolean_T;
        public static readonly TexlFunction Boolean_UO;
        public static readonly TexlFunction BooleanB;
        public static readonly TexlFunction BooleanB_T;
        public static readonly TexlFunction BooleanN;
        public static readonly TexlFunction BooleanN_T;
        public static readonly TexlFunction BooleanW;
        public static readonly TexlFunction BooleanW_T;
        public static readonly TexlFunction Char;
        public static readonly TexlFunction CharT;
        public static readonly TexlFunction Clock24;
        public static readonly TexlFunction Coalesce;
        public static readonly TexlFunction ColorFade;
        public static readonly TexlFunction ColorFadeT;
        public static readonly TexlFunction ColorValue;
        public static readonly TexlFunction ColorValue_UO;
        public static readonly TexlFunction Column_UO;
        public static readonly TexlFunction ColumnNames_UO;
        public static readonly TexlFunction Concat;
        public static readonly TexlFunction Concatenate;
        public static readonly TexlFunction ConcatenateT;
        public static readonly TexlFunction Cos;
        public static readonly TexlFunction CosT;
        public static readonly TexlFunction Cot;
        public static readonly TexlFunction CotT;
        public static readonly TexlFunction Count;
        public static readonly TexlFunction CountA;
        public static readonly TexlFunction CountIf;
        public static readonly TexlFunction CountRows;
        public static readonly TexlFunction CountRows_UO;
        public static readonly TexlFunction Date;
        public static readonly TexlFunction DateAdd;
        public static readonly TexlFunction DateAddT;
        public static readonly TexlFunction DateDiff;
        public static readonly TexlFunction DateDiffT;
        public static readonly TexlFunction DateTime;
        public static readonly TexlFunction DateTimeValue;
        public static readonly TexlFunction DateTimeValue_UO;
        public static readonly TexlFunction DateValue;
        public static readonly TexlFunction DateValue_UO;
        public static readonly TexlFunction Day;
        public static readonly TexlFunction Dec2Hex;
        public static readonly TexlFunction Dec2HexT;
        public static readonly TexlFunction Degrees;
        public static readonly TexlFunction DegreesT;
        public static readonly TexlFunction DropColumns;
        public static readonly TexlFunction EDate;
        public static readonly TexlFunction EOMonth;
        public static readonly TexlFunction EncodeHTML;
        public static readonly TexlFunction EncodeUrl;
        public static readonly TexlFunction EndsWith;
        public static readonly TexlFunction Error;
        public static readonly TexlFunction Exp;
        public static readonly TexlFunction ExpT;
        public static readonly TexlFunction Filter;
        public static readonly TexlFunction Find;
        public static readonly TexlFunction FindT;
        public static readonly TexlFunction First;
        public static readonly TexlFunction First_UO;
        public static readonly TexlFunction FirstN;
        public static readonly TexlFunction FirstN_UO;
        public static readonly TexlFunction ForAll;
        public static readonly TexlFunction ForAll_UO;
        public static readonly TexlFunction GUID_UO;
        public static readonly TexlFunction GUIDNoArg;
        public static readonly TexlFunction GUIDPure;
        public static readonly TexlFunction Hex2Dec;
        public static readonly TexlFunction Hex2DecT;
        public static readonly TexlFunction Hour;
        public static readonly TexlFunction If;
        public static readonly TexlFunction IfError;
        public static readonly TexlFunction Index;
        public static readonly TexlFunction Index_UO;
        public static readonly TexlFunction Int;
        public static readonly TexlFunction IntT;
        public static readonly TexlFunction IsBlank;
        public static readonly TexlFunction IsBlankOptionSetValue;
        public static readonly TexlFunction IsBlankOrError;
        public static readonly TexlFunction IsBlankOrErrorOptionSetValue;
        public static readonly TexlFunction IsEmpty;
        public static readonly TexlFunction IsError;
        public static readonly TexlFunction IsNumeric;
        public static readonly TexlFunction ISOWeekNum;
        public static readonly TexlFunction IsToday;
        public static readonly TexlFunction Language;
        public static readonly TexlFunction Last;
        public static readonly TexlFunction Last_UO;
        public static readonly TexlFunction LastN;
        public static readonly TexlFunction LastN_UO;
        public static readonly TexlFunction Left;
        public static readonly TexlFunction LeftST;
        public static readonly TexlFunction LeftTS;
        public static readonly TexlFunction LeftTT;
        public static readonly TexlFunction Len;
        public static readonly TexlFunction LenT;
        public static readonly TexlFunction Ln;
        public static readonly TexlFunction LnT;
        public static readonly TexlFunction Log;
        public static readonly TexlFunction LogT;
        public static readonly TexlFunction LookUp;
        public static readonly TexlFunction Lower;
        public static readonly TexlFunction LowerT;
        public static readonly TexlFunction Max;
        public static readonly TexlFunction MaxT;
        public static readonly TexlFunction Mid;
        public static readonly TexlFunction MidT;
        public static readonly TexlFunction Min;
        public static readonly TexlFunction MinT;
        public static readonly TexlFunction Minute;
        public static readonly TexlFunction Mod;
        public static readonly TexlFunction ModT;
        public static readonly TexlFunction Month;
        public static readonly TexlFunction MonthsLong;
        public static readonly TexlFunction MonthsShort;
        public static readonly TexlFunction Not;
        public static readonly TexlFunction Now;
        public static readonly TexlFunction Or;
        public static readonly TexlFunction ParseJSON;
        public static readonly TexlFunction PatchRecord;
        public static readonly TexlFunction Pi;
        public static readonly TexlFunction PlainText;
        public static readonly TexlFunction Power;
        public static readonly TexlFunction PowerT;
        public static readonly TexlFunction Proper;
        public static readonly TexlFunction ProperT;
        public static readonly TexlFunction Radians;
        public static readonly TexlFunction RadiansT;
        public static readonly TexlFunction Rand;
        public static readonly TexlFunction RandBetween;
        public static readonly TexlFunction Refresh;
        public static readonly TexlFunction RenameColumns;
        public static readonly TexlFunction Replace;
        public static readonly TexlFunction ReplaceT;
        public static readonly TexlFunction RGBA;
        public static readonly TexlFunction Right;
        public static readonly TexlFunction RightST;
        public static readonly TexlFunction RightTS;
        public static readonly TexlFunction RightTT;
        public static readonly TexlFunction Round;
        public static readonly TexlFunction RoundDown;
        public static readonly TexlFunction RoundDownT;
        public static readonly TexlFunction RoundT;
        public static readonly TexlFunction RoundUp;
        public static readonly TexlFunction RoundUpT;
        public static readonly TexlFunction Search;
        public static readonly TexlFunction Second;
        public static readonly TexlFunction Sequence;
        public static readonly TexlFunction ShowColumns;
        public static readonly TexlFunction Shuffle;
        public static readonly TexlFunction Sin;
        public static readonly TexlFunction SinT;
        public static readonly TexlFunction Sort;
        public static readonly TexlFunction SortByColumns;
        public static readonly TexlFunction Split;
        public static readonly TexlFunction Sqrt;
        public static readonly TexlFunction SqrtT;
        public static readonly TexlFunction StartsWith;
        public static readonly TexlFunction StdevP;
        public static readonly TexlFunction StdevPT;
        public static readonly TexlFunction Substitute;
        public static readonly TexlFunction SubstituteT;
        public static readonly TexlFunction Sum;
        public static readonly TexlFunction SumT;
        public static readonly TexlFunction Switch;
        public static readonly TexlFunction Table;
        public static readonly TexlFunction Table_UO;
        public static readonly TexlFunction Tan;
        public static readonly TexlFunction TanT;
        public static readonly TexlFunction Text;
        public static readonly TexlFunction Text_UO;
        public static readonly TexlFunction Time;
        public static readonly TexlFunction TimeValue;
        public static readonly TexlFunction TimeValue_UO;
        public static readonly TexlFunction TimeZoneOffset;
        public static readonly TexlFunction Today;
        public static readonly TexlFunction Trace;
        public static readonly TexlFunction Trim;
        public static readonly TexlFunction TrimEnds;
        public static readonly TexlFunction TrimEndsT;
        public static readonly TexlFunction TrimT;
        public static readonly TexlFunction Trunc;
        public static readonly TexlFunction TruncT;
        public static readonly TexlFunction UniChar;
        public static readonly TexlFunction UniCharT;
        public static readonly TexlFunction Upper;
        public static readonly TexlFunction UpperT;
        public static readonly TexlFunction Value;
        public static readonly TexlFunction Value_UO;
        public static readonly TexlFunction VarP;
        public static readonly TexlFunction VarPT;
        public static readonly TexlFunction Weekday;
        public static readonly TexlFunction WeekdaysLong;
        public static readonly TexlFunction WeekdaysShort;
        public static readonly TexlFunction WeekNum;
        public static readonly TexlFunction With;
        public static readonly TexlFunction Year;

        // Don't add new functions here, follow alpha order

        // _featureGateFunctions functions, not present in all platforms
        internal static readonly TexlFunctionSet _featureGateFunctions;

        public static readonly TexlFunction Decimal;
        public static readonly TexlFunction Decimal_UO;
        public static readonly TexlFunction Float;
        public static readonly TexlFunction Float_UO;
        public static readonly TexlFunction IsUTCToday;
        public static readonly TexlFunction UTCNow;
        public static readonly TexlFunction UTCToday;
        public static readonly TexlFunction BooleanL;
        public static readonly TexlFunction BooleanL_T;
        public static readonly TexlFunction Summarize;

        static BuiltinFunctionsCore()
        {
            _library = new TexlFunctionSet();
            Abs = _library.Add(new AbsFunction());
            AbsT = _library.Add(new AbsTableFunction());
            Acos = _library.Add(new AcosFunction());
            AcosT = _library.Add(new AcosTableFunction());
            Acot = _library.Add(new AcotFunction());
            AcotT = _library.Add(new AcotTableFunction());
            AddColumns = _library.Add(new AddColumnsFunction());
            AmPm = _library.Add(new AmPmFunction());
            AmPmShort = _library.Add(new AmPmShortFunction());
            And = _library.Add(new VariadicLogicalFunction(isAnd: true));
            Asin = _library.Add(new AsinFunction());
            AsinT = _library.Add(new AsinTableFunction());
            AsType = _library.Add(new AsTypeFunction());
            Atan = _library.Add(new AtanFunction());
            Atan2 = _library.Add(new Atan2Function());
            AtanT = _library.Add(new AtanTableFunction());
            Average = _library.Add(new AverageFunction());
            AverageT = _library.Add(new AverageTableFunction());
            Blank = _library.Add(new BlankFunction());
            Boolean = _library.Add(new BooleanFunction());
            Boolean_T = _library.Add(new BooleanFunction_T());
            Boolean_UO = _library.Add(new BooleanFunction_UO());
            BooleanB = _library.Add(new BooleanBFunction());
            BooleanB_T = _library.Add(new BooleanBFunction_T());
            BooleanN = _library.Add(new BooleanNFunction());
            BooleanN_T = _library.Add(new BooleanNFunction_T());
            BooleanW = _library.Add(new BooleanWFunction());
            BooleanW_T = _library.Add(new BooleanWFunction_T());
            Char = _library.Add(new CharFunction());
            CharT = _library.Add(new CharTFunction());
            Clock24 = _library.Add(new IsClock24Function());
            Coalesce = _library.Add(new CoalesceFunction());
            ColorFade = _library.Add(new ColorFadeFunction());
            ColorFadeT = _library.Add(new ColorFadeTFunction());
            ColorValue = _library.Add(new ColorValueFunction());
            ColorValue_UO = _library.Add(new ColorValueFunction_UO());
            Column_UO = _library.Add(new ColumnFunction_UO());
            ColumnNames_UO = _library.Add(new ColumnNamesFunction_UO());
            Concat = _library.Add(new ConcatFunction());
            Concatenate = _library.Add(new ConcatenateFunction());
            ConcatenateT = _library.Add(new ConcatenateTableFunction());
            Cos = _library.Add(new CosFunction());
            CosT = _library.Add(new CosTableFunction());
            Cot = _library.Add(new CotFunction());
            CotT = _library.Add(new CotTableFunction());
            Count = _library.Add(new CountFunction());
            CountA = _library.Add(new CountAFunction());
            CountIf = _library.Add(new CountIfFunction());
            CountRows = _library.Add(new CountRowsFunction());
            CountRows_UO = _library.Add(new CountRowsFunction_UO());
            Date = _library.Add(new DateFunction());
            DateAdd = _library.Add(new DateAddFunction());
            DateAddT = _library.Add(new DateAddTFunction());
            DateDiff = _library.Add(new DateDiffFunction());
            DateDiffT = _library.Add(new DateDiffTFunction());
            DateTime = _library.Add(new DateTimeFunction());
            DateTimeValue = _library.Add(new DateTimeValueFunction());
            DateTimeValue_UO = _library.Add(new DateTimeValueFunction_UO());
            DateValue = _library.Add(new DateValueFunction());
            DateValue_UO = _library.Add(new DateValueFunction_UO());
            Day = _library.Add(new DayFunction());
            Dec2Hex = _library.Add(new Dec2HexFunction());
            Dec2HexT = _library.Add(new Dec2HexTFunction());
            Degrees = _library.Add(new DegreesFunction());
            DegreesT = _library.Add(new DegreesTableFunction());
            DropColumns = _library.Add(new DropColumnsFunction());
            EDate = _library.Add(new EDateFunction());
            EOMonth = _library.Add(new EOMonthFunction());
            EncodeHTML = _library.Add(new EncodeHTMLFunction());
            EncodeUrl = _library.Add(new EncodeUrlFunction());
            EndsWith = _library.Add(new EndsWithFunction());
            Error = _library.Add(new ErrorFunction());
            Exp = _library.Add(new ExpFunction());
            ExpT = _library.Add(new ExpTableFunction());
            Filter = _library.Add(new FilterFunction());
            Find = _library.Add(new FindFunction());
            FindT = _library.Add(new FindTFunction());
            First = _library.Add(new FirstLastFunction(isFirst: true));
            First_UO = _library.Add(new FirstLastFunction_UO(isFirst: true));
            FirstN = _library.Add(new FirstLastNFunction(isFirst: true));
            FirstN_UO = _library.Add(new FirstLastNFunction_UO(isFirst: true));
            ForAll = _library.Add(new ForAllFunction());
            ForAll_UO = _library.Add(new ForAllFunction_UO());
            GUID_UO = _library.Add(new GUIDPureFunction_UO());
            GUIDNoArg = _library.Add(new GUIDNoArgFunction());
            GUIDPure = _library.Add(new GUIDPureFunction());
            Hex2Dec = _library.Add(new Hex2DecFunction());
            Hex2DecT = _library.Add(new Hex2DecTFunction());
            Hour = _library.Add(new HourFunction());
            If = _library.Add(new IfFunction());
            IfError = _library.Add(new IfErrorFunction());
            Index = _library.Add(new IndexFunction());
            Index_UO = _library.Add(new IndexFunction_UO());
            Int = _library.Add(new IntFunction());
            IntT = _library.Add(new IntTableFunction());
            IsBlank = _library.Add(new IsBlankFunction());
            IsBlankOptionSetValue = _library.Add(new IsBlankOptionSetValueFunction());
            IsBlankOrError = _library.Add(new IsBlankOrErrorFunction());
            IsBlankOrErrorOptionSetValue = _library.Add(new IsBlankOrErrorOptionSetValueFunction());
            IsEmpty = _library.Add(new IsEmptyFunction());
            IsError = _library.Add(new IsErrorFunction());
            IsNumeric = _library.Add(new IsNumericFunction());
            ISOWeekNum = _library.Add(new ISOWeekNumFunction());
            IsToday = _library.Add(new IsTodayFunction());
            Language = _library.Add(new LanguageFunction());
            Last = _library.Add(new FirstLastFunction(isFirst: false));
            Last_UO = _library.Add(new FirstLastFunction_UO(isFirst: false));
            LastN = _library.Add(new FirstLastNFunction(isFirst: false));
            LastN_UO = _library.Add(new FirstLastNFunction_UO(isFirst: false));
            Left = _library.Add(new LeftRightScalarFunction(isLeft: true));
            LeftST = _library.Add(new LeftRightScalarTableFunction(isLeft: true));
            LeftTS = _library.Add(new LeftRightTableScalarFunction(isLeft: true));
            LeftTT = _library.Add(new LeftRightTableTableFunction(isLeft: true));
            Len = _library.Add(new LenFunction());
            LenT = _library.Add(new LenTFunction());
            Ln = _library.Add(new LnFunction());
            LnT = _library.Add(new LnTableFunction());
            Log = _library.Add(new LogFunction());
            LogT = _library.Add(new LogTFunction());
            LookUp = _library.Add(new LookUpFunction());
            Lower = _library.Add(new LowerUpperFunction(isLower: true));
            LowerT = _library.Add(new LowerUpperTFunction(isLower: true));
            Max = _library.Add(new MinMaxFunction(isMin: false));
            MaxT = _library.Add(new MinMaxTableFunction(isMin: false));
            Mid = _library.Add(new MidFunction());
            MidT = _library.Add(new MidTFunction());
            Min = _library.Add(new MinMaxFunction(isMin: true));
            MinT = _library.Add(new MinMaxTableFunction(isMin: true));
            Minute = _library.Add(new MinuteFunction());
            Mod = _library.Add(new ModFunction());
            ModT = _library.Add(new ModTFunction());
            Month = _library.Add(new MonthFunction());
            MonthsLong = _library.Add(new MonthsLongFunction());
            MonthsShort = _library.Add(new MonthsShortFunction());
            Not = _library.Add(new NotFunction());
            Now = _library.Add(new NowFunction());
            Or = _library.Add(new VariadicLogicalFunction(isAnd: false));
            ParseJSON = _library.Add(new ParseJSONFunction());
            PatchRecord = _library.Add(new PatchRecordFunction());
            Pi = _library.Add(new PiFunction());
            PlainText = _library.Add(new PlainTextFunction());
            Power = _library.Add(new PowerFunction());
            PowerT = _library.Add(new PowerTFunction());
            Proper = _library.Add(new ProperFunction());
            ProperT = _library.Add(new ProperTFunction());
            Radians = _library.Add(new RadiansFunction());
            RadiansT = _library.Add(new RadiansTableFunction());
            Rand = _library.Add(new RandFunction());
            RandBetween = _library.Add(new RandBetweenFunction());
            Refresh = _library.Add(new RefreshFunction());
            RenameColumns = _library.Add(new RenameColumnsFunction());
            Replace = _library.Add(new ReplaceFunction());
            ReplaceT = _library.Add(new ReplaceTFunction());
            RGBA = _library.Add(new RGBAFunction());
            Right = _library.Add(new LeftRightScalarFunction(isLeft: false));
            RightST = _library.Add(new LeftRightScalarTableFunction(isLeft: false));
            RightTS = _library.Add(new LeftRightTableScalarFunction(isLeft: false));
            RightTT = _library.Add(new LeftRightTableTableFunction(isLeft: false));
            Round = _library.Add(new RoundScalarFunction());
            RoundDown = _library.Add(new RoundDownScalarFunction());
            RoundDownT = _library.Add(new RoundDownTableFunction());
            RoundT = _library.Add(new RoundTableFunction());
            RoundUp = _library.Add(new RoundUpScalarFunction());
            RoundUpT = _library.Add(new RoundUpTableFunction());
            Search = _library.Add(new SearchFunction());
            Second = _library.Add(new SecondFunction());
            Sequence = _library.Add(new SequenceFunction());
            ShowColumns = _library.Add(new ShowColumnsFunction());
            Shuffle = _library.Add(new ShuffleFunction());
            Sin = _library.Add(new SinFunction());
            SinT = _library.Add(new SinTableFunction());
            Sort = _library.Add(new SortFunction());
            SortByColumns = _library.Add(new SortByColumnsFunction());
            Split = _library.Add(new SplitFunction());
            Sqrt = _library.Add(new SqrtFunction());
            SqrtT = _library.Add(new SqrtTableFunction());
            StartsWith = _library.Add(new StartsWithFunction());
            StdevP = _library.Add(new StdevPFunction());
            StdevPT = _library.Add(new StdevPTableFunction());
            Substitute = _library.Add(new SubstituteFunction());
            SubstituteT = _library.Add(new SubstituteTFunction());
            Sum = _library.Add(new SumFunction());
            SumT = _library.Add(new SumTableFunction());
            Switch = _library.Add(new SwitchFunction());
            Table = _library.Add(new TableFunction());
            Table_UO = _library.Add(new TableFunction_UO());
            Tan = _library.Add(new TanFunction());
            TanT = _library.Add(new TanTableFunction());
            Text = _library.Add(new TextFunction());
            Text_UO = _library.Add(new TextFunction_UO());
            Time = _library.Add(new TimeFunction());
            TimeValue = _library.Add(new TimeValueFunction());
            TimeValue_UO = _library.Add(new TimeValueFunction_UO());
            TimeZoneOffset = _library.Add(new TimeZoneOffsetFunction());
            Today = _library.Add(new TodayFunction());
            Trace = _library.Add(new TraceFunction());
            Trim = _library.Add(new TrimFunction());
            TrimEnds = _library.Add(new TrimEndsFunction());
            TrimEndsT = _library.Add(new TrimEndsTFunction());
            TrimT = _library.Add(new TrimTFunction());
            Trunc = _library.Add(new TruncFunction());
            TruncT = _library.Add(new TruncTableFunction());
            UniChar = _library.Add(new UniCharFunction());
            UniCharT = _library.Add(new UniCharTFunction());
            Upper = _library.Add(new LowerUpperFunction(isLower: false));
            UpperT = _library.Add(new LowerUpperTFunction(isLower: false));
            Value = _library.Add(new ValueFunction());
            Value_UO = _library.Add(new ValueFunction_UO());
            VarP = _library.Add(new VarPFunction());
            VarPT = _library.Add(new VarPTableFunction());
            Weekday = _library.Add(new WeekdayFunction());
            WeekdaysLong = _library.Add(new WeekdaysLongFunction());
            WeekdaysShort = _library.Add(new WeekdaysShortFunction());
            WeekNum = _library.Add(new WeekNumFunction());
            With = _library.Add(new WithFunction());
            Year = _library.Add(new YearFunction());
            _featureGateFunctions = new TexlFunctionSet();
            Decimal = _featureGateFunctions.Add(new DecimalFunction());
            Decimal_UO = _featureGateFunctions.Add(new DecimalFunction_UO());
            Float = _featureGateFunctions.Add(new FloatFunction());
            Float_UO = _featureGateFunctions.Add(new FloatFunction_UO());
            IsUTCToday = _featureGateFunctions.Add(new IsUTCTodayFunction());
            UTCNow = _featureGateFunctions.Add(new UTCNowFunction());
            UTCToday = _featureGateFunctions.Add(new UTCTodayFunction());
            BooleanL = _featureGateFunctions.Add(new BooleanLFunction());
            BooleanL_T = _featureGateFunctions.Add(new BooleanLFunction_T());
            Summarize = _featureGateFunctions.Add(new SummarizeFunction());
#pragma warning disable CS0618 // Type or member is obsolete        
            _testOnlyLibrary = new TexlFunctionSet(_library.Functions).Add(_featureGateFunctions);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        // Slow API, only use for backward compatibility
#pragma warning disable CS0618 // Type or member is obsolete        
        public static IEnumerable<TexlFunction> BuiltinFunctionsLibrary => _library.Functions;

        private static readonly TexlFunctionSet _testOnlyLibrary;

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
