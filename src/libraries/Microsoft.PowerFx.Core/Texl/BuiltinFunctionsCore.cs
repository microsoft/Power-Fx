// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
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

        static BuiltinFunctionsCore()
        {
            Abs = new AbsFunction();
            AbsT = new AbsTableFunction();
            Acos = new AcosFunction();
            AcosT = new AcosTableFunction();
            Acot = new AcotFunction();
            AcotT = new AcotTableFunction();
            AddColumns = new AddColumnsFunction();
            AmPm = new AmPmFunction();
            AmPmShort = new AmPmShortFunction();
            And = new VariadicLogicalFunction(isAnd: true);
            Asin = new AsinFunction();
            AsinT = new AsinTableFunction();
            AsType = new AsTypeFunction();
            Atan = new AtanFunction();
            Atan2 = new Atan2Function();
            AtanT = new AtanTableFunction();
            Average = new AverageFunction();
            AverageT = new AverageTableFunction();
            Blank = new BlankFunction();
            Boolean = new BooleanFunction();
            Boolean_T = new BooleanFunction_T();
            Boolean_UO = new BooleanFunction_UO();
            BooleanB = new BooleanBFunction();
            BooleanB_T = new BooleanBFunction_T();
            BooleanN = new BooleanNFunction();
            BooleanN_T = new BooleanNFunction_T();
            BooleanW = new BooleanWFunction();
            BooleanW_T = new BooleanWFunction_T();
            Char = new CharFunction();
            CharT = new CharTFunction();
            Clock24 = new IsClock24Function();
            Coalesce = new CoalesceFunction();
            ColorFade = new ColorFadeFunction();
            ColorFadeT = new ColorFadeTFunction();
            ColorValue = new ColorValueFunction();
            ColorValue_UO = new ColorValueFunction_UO();
            Column_UO = new ColumnFunction_UO();
            ColumnNames_UO = new ColumnNamesFunction_UO();
            Concat = new ConcatFunction();
            Concatenate = new ConcatenateFunction();
            ConcatenateT = new ConcatenateTableFunction();
            Cos = new CosFunction();
            CosT = new CosTableFunction();
            Cot = new CotFunction();
            CotT = new CotTableFunction();
            Count = new CountFunction();
            CountA = new CountAFunction();
            CountIf = new CountIfFunction();
            CountRows = new CountRowsFunction();
            CountRows_UO = new CountRowsFunction_UO();
            Date = new DateFunction();
            DateAdd = new DateAddFunction();
            DateAddT = new DateAddTFunction();
            DateDiff = new DateDiffFunction();
            DateDiffT = new DateDiffTFunction();
            DateTime = new DateTimeFunction();
            DateTimeValue = new DateTimeValueFunction();
            DateTimeValue_UO = new DateTimeValueFunction_UO();
            DateValue = new DateValueFunction();
            DateValue_UO = new DateValueFunction_UO();
            Day = new DayFunction();
            Dec2Hex = new Dec2HexFunction();
            Dec2HexT = new Dec2HexTFunction();
            Degrees = new DegreesFunction();
            DegreesT = new DegreesTableFunction();
            DropColumns = new DropColumnsFunction();
            EDate = new EDateFunction();
            EOMonth = new EOMonthFunction();
            EncodeHTML = new EncodeHTMLFunction();
            EncodeUrl = new EncodeUrlFunction();
            EndsWith = new EndsWithFunction();
            Error = new ErrorFunction();
            Exp = new ExpFunction();
            ExpT = new ExpTableFunction();
            Filter = new FilterFunction();
            Find = new FindFunction();
            FindT = new FindTFunction();
            First = new FirstLastFunction(isFirst: true);
            First_UO = new FirstLastFunction_UO(isFirst: true);
            FirstN = new FirstLastNFunction(isFirst: true);
            FirstN_UO = new FirstLastNFunction_UO(isFirst: true);
            ForAll = new ForAllFunction();
            ForAll_UO = new ForAllFunction_UO();
            GUID_UO = new GUIDPureFunction_UO();
            GUIDNoArg = new GUIDNoArgFunction();
            GUIDPure = new GUIDPureFunction();
            Hex2Dec = new Hex2DecFunction();
            Hex2DecT = new Hex2DecTFunction();
            Hour = new HourFunction();
            If = new IfFunction();
            IfError = new IfErrorFunction();
            Index = new IndexFunction();
            Index_UO = new IndexFunction_UO();
            Int = new IntFunction();
            IntT = new IntTableFunction();
            IsBlank = new IsBlankFunction();
            IsBlankOptionSetValue = new IsBlankOptionSetValueFunction();
            IsBlankOrError = new IsBlankOrErrorFunction();
            IsBlankOrErrorOptionSetValue = new IsBlankOrErrorOptionSetValueFunction();
            IsEmpty = new IsEmptyFunction();
            IsError = new IsErrorFunction();
            IsNumeric = new IsNumericFunction();
            ISOWeekNum = new ISOWeekNumFunction();
            IsToday = new IsTodayFunction();
            Language = new LanguageFunction();
            Last = new FirstLastFunction(isFirst: false);
            Last_UO = new FirstLastFunction_UO(isFirst: false);
            LastN = new FirstLastNFunction(isFirst: false);
            LastN_UO = new FirstLastNFunction_UO(isFirst: false);
            Left = new LeftRightScalarFunction(isLeft: true);
            LeftST = new LeftRightScalarTableFunction(isLeft: true);
            LeftTS = new LeftRightTableScalarFunction(isLeft: true);
            LeftTT = new LeftRightTableTableFunction(isLeft: true);
            Len = new LenFunction();
            LenT = new LenTFunction();
            Ln = new LnFunction();
            LnT = new LnTableFunction();
            Log = new LogFunction();
            LogT = new LogTFunction();
            LookUp = new LookUpFunction();
            Lower = new LowerUpperFunction(isLower: true);
            LowerT = new LowerUpperTFunction(isLower: true);
            Max = new MinMaxFunction(isMin: false);
            MaxT = new MinMaxTableFunction(isMin: false);
            Mid = new MidFunction();
            MidT = new MidTFunction();
            Min = new MinMaxFunction(isMin: true);
            MinT = new MinMaxTableFunction(isMin: true);
            Minute = new MinuteFunction();
            Mod = new ModFunction();
            ModT = new ModTFunction();
            Month = new MonthFunction();
            MonthsLong = new MonthsLongFunction();
            MonthsShort = new MonthsShortFunction();
            Not = new NotFunction();
            Now = new NowFunction();
            Or = new VariadicLogicalFunction(isAnd: false);
            ParseJSON = new ParseJSONFunction();
            PatchRecord = new PatchRecordFunction();
            Pi = new PiFunction();
            PlainText = new PlainTextFunction();
            Power = new PowerFunction();
            PowerT = new PowerTFunction();
            Proper = new ProperFunction();
            ProperT = new ProperTFunction();
            Radians = new RadiansFunction();
            RadiansT = new RadiansTableFunction();
            Rand = new RandFunction();
            RandBetween = new RandBetweenFunction();
            Refresh = new RefreshFunction();
            RenameColumns = new RenameColumnsFunction();
            Replace = new ReplaceFunction();
            ReplaceT = new ReplaceTFunction();
            RGBA = new RGBAFunction();
            Right = new LeftRightScalarFunction(isLeft: false);
            RightST = new LeftRightScalarTableFunction(isLeft: false);
            RightTS = new LeftRightTableScalarFunction(isLeft: false);
            RightTT = new LeftRightTableTableFunction(isLeft: false);
            Round = new RoundScalarFunction();
            RoundDown = new RoundDownScalarFunction();
            RoundDownT = new RoundDownTableFunction();
            RoundT = new RoundTableFunction();
            RoundUp = new RoundUpScalarFunction();
            RoundUpT = new RoundUpTableFunction();
            Search = new SearchFunction();
            Second = new SecondFunction();
            Sequence = new SequenceFunction();
            ShowColumns = new ShowColumnsFunction();
            Shuffle = new ShuffleFunction();
            Sin = new SinFunction();
            SinT = new SinTableFunction();
            Sort = new SortFunction();
            SortByColumns = new SortByColumnsFunction();
            Split = new SplitFunction();
            Sqrt = new SqrtFunction();
            SqrtT = new SqrtTableFunction();
            StartsWith = new StartsWithFunction();
            StdevP = new StdevPFunction();
            StdevPT = new StdevPTableFunction();
            Substitute = new SubstituteFunction();
            SubstituteT = new SubstituteTFunction();
            Sum = new SumFunction();
            SumT = new SumTableFunction();
            Switch = new SwitchFunction();
            Table = new TableFunction();
            Table_UO = new TableFunction_UO();
            Tan = new TanFunction();
            TanT = new TanTableFunction();
            Text = new TextFunction();
            Text_UO = new TextFunction_UO();
            Time = new TimeFunction();
            TimeValue = new TimeValueFunction();
            TimeValue_UO = new TimeValueFunction_UO();
            TimeZoneOffset = new TimeZoneOffsetFunction();
            Today = new TodayFunction();
            Trace = new TraceFunction();
            Trim = new TrimFunction();
            TrimEnds = new TrimEndsFunction();
            TrimEndsT = new TrimEndsTFunction();
            TrimT = new TrimTFunction();
            Trunc = new TruncFunction();
            TruncT = new TruncTableFunction();
            UniChar = new UniCharFunction();
            UniCharT = new UniCharTFunction();
            Upper = new LowerUpperFunction(isLower: false);
            UpperT = new LowerUpperTFunction(isLower: false);
            Value = new ValueFunction();
            Value_UO = new ValueFunction_UO();
            VarP = new VarPFunction();
            VarPT = new VarPTableFunction();
            Weekday = new WeekdayFunction();
            WeekdaysLong = new WeekdaysLongFunction();
            WeekdaysShort = new WeekdaysShortFunction();
            WeekNum = new WeekNumFunction();
            With = new WithFunction();
            Year = new YearFunction();
            
            _library = new TexlFunctionSet(new[]
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
                EOMonth,
                EncodeHTML,
                EncodeUrl,
                EndsWith,
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

            Decimal = new DecimalFunction();
            Decimal_UO = new DecimalFunction_UO();
            Float = new FloatFunction();
            Float_UO = new FloatFunction_UO();
            IsUTCToday = new IsUTCTodayFunction();
            UTCNow = new UTCNowFunction();
            UTCToday = new UTCTodayFunction();
            BooleanL = new BooleanLFunction();
            BooleanL_T = new BooleanLFunction_T();
            Summarize = new SummarizeFunction();

            _featureGateFunctions = new TexlFunctionSet(new[]
            {
                Decimal,
                Decimal_UO,
                Float,
                Float_UO,
                IsUTCToday,
                UTCNow,
                UTCToday,
                BooleanL,
                BooleanL_T,
                Summarize
            });            

            // Slow API, only use for backward compatibility
#pragma warning disable CS0618 // Type or member is obsolete
            _testOnlyLibrary = new TexlFunctionSet(_library.Functions).Add(_featureGateFunctions);
#pragma warning restore CS0618 // Type or member is obsolete
        }

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

        private static readonly TexlFunctionSet _testOnlyLibrary;

        // Slow API, only use for backward compatibility
#pragma warning disable CS0618 // Type or member is obsolete        
        public static IEnumerable<TexlFunction> BuiltinFunctionsLibrary => _library.Functions;

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
