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

        private static readonly TexlFunction[] _functionArray = new TexlFunction[]
        {
             new AbsFunction(),
             new AbsTableFunction(),
             new AcosFunction(),
             new AcosTableFunction(),
             new AcotFunction(),
             new AcotTableFunction(),
             new AddColumnsFunction(),
             new AmPmFunction(),
             new AmPmShortFunction(),
             new VariadicLogicalFunction(isAnd: true),
             new AsinFunction(),
             new AsinTableFunction(),
             new AsTypeFunction(),
             new AtanFunction(),
             new Atan2Function(),
             new AtanTableFunction(),
             new AverageFunction(),
             new AverageTableFunction(),
             new BlankFunction(),
             new BooleanFunction(),
             new BooleanFunction_T(),
             new BooleanFunction_UO(),
             new BooleanBFunction(),
             new BooleanBFunction_T(),
             new BooleanNFunction(),
             new BooleanNFunction_T(),
             new BooleanWFunction(),
             new BooleanWFunction_T(),
             new CharFunction(),
             new CharTFunction(),
             new IsClock24Function(),
             new CoalesceFunction(),
             new ColorFadeFunction(),
             new ColorFadeTFunction(),
             new ColorValueFunction(),
             new ColorValueFunction_UO(),
             new ColumnFunction_UO(),
             new ColumnNamesFunction_UO(),
             new ConcatFunction(),
             new ConcatenateFunction(),
             new ConcatenateTableFunction(),
             new CosFunction(),
             new CosTableFunction(),
             new CotFunction(),
             new CotTableFunction(),
             new CountFunction(),
             new CountAFunction(),
             new CountIfFunction(),
             new CountRowsFunction(),
             new CountRowsFunction_UO(),
             new DateFunction(),
             new DateAddFunction(),
             new DateAddTFunction(),
             new DateDiffFunction(),
             new DateDiffTFunction(),
             new DateTimeFunction(),
             new DateTimeValueFunction(),
             new DateTimeValueFunction_UO(),
             new DateValueFunction(),
             new DateValueFunction_UO(),
             new DayFunction(),
             new Dec2HexFunction(),
             new Dec2HexTFunction(),
             new DegreesFunction(),
             new DegreesTableFunction(),
             new DropColumnsFunction(),
             new EDateFunction(),
             new EOMonthFunction(),
             new EncodeHTMLFunction(),
             new EncodeUrlFunction(),
             new EndsWithFunction(),
             new ErrorFunction(),
             new ExpFunction(),
             new ExpTableFunction(),
             new FilterFunction(),
             new FindFunction(),
             new FindTFunction(),
             new FirstLastFunction(isFirst: true),
             new FirstLastFunction_UO(isFirst: true),
             new FirstLastNFunction(isFirst: true),
             new FirstLastNFunction_UO(isFirst: true),
             new ForAllFunction(),
             new ForAllFunction_UO(),
             new GUIDPureFunction_UO(),
             new GUIDNoArgFunction(),
             new GUIDPureFunction(),
             new Hex2DecFunction(),
             new Hex2DecTFunction(),
             new HourFunction(),
             new IfFunction(),
             new IfErrorFunction(),
             new IndexFunction(),
             new IndexFunction_UO(),
             new IntFunction(),
             new IntTableFunction(),
             new IsBlankFunction(),
             new IsBlankOptionSetValueFunction(),
             new IsBlankOrErrorFunction(),
             new IsBlankOrErrorOptionSetValueFunction(),
             new IsEmptyFunction(),
             new IsErrorFunction(),
             new IsNumericFunction(),
             new ISOWeekNumFunction(),
             new IsTodayFunction(),
             new LanguageFunction(),
             new FirstLastFunction(isFirst: false),
             new FirstLastFunction_UO(isFirst: false),
             new FirstLastNFunction(isFirst: false),
             new FirstLastNFunction_UO(isFirst: false),
             new LeftRightScalarFunction(isLeft: true),
             new LeftRightScalarTableFunction(isLeft: true),
             new LeftRightTableScalarFunction(isLeft: true),
             new LeftRightTableTableFunction(isLeft: true),
             new LenFunction(),
             new LenTFunction(),
             new LnFunction(),
             new LnTableFunction(),
             new LogFunction(),
             new LogTFunction(),
             new LookUpFunction(),
             new LowerUpperFunction(isLower: true),
             new LowerUpperTFunction(isLower: true),
             new MinMaxFunction(isMin: false),
             new MinMaxTableFunction(isMin: false),
             new MidFunction(),
             new MidTFunction(),
             new MinMaxFunction(isMin: true),
             new MinMaxTableFunction(isMin: true),
             new MinuteFunction(),
             new ModFunction(),
             new ModTFunction(),
             new MonthFunction(),
             new MonthsLongFunction(),
             new MonthsShortFunction(),
             new NotFunction(),
             new NowFunction(),
             new VariadicLogicalFunction(isAnd: false),
             new ParseJSONFunction(),
             new PatchRecordFunction(),
             new PiFunction(),
             new PlainTextFunction(),
             new PowerFunction(),
             new PowerTFunction(),
             new ProperFunction(),
             new ProperTFunction(),
             new RadiansFunction(),
             new RadiansTableFunction(),
             new RandFunction(),
             new RandBetweenFunction(),
             new RefreshFunction(),
             new RenameColumnsFunction(),
             new ReplaceFunction(),
             new ReplaceTFunction(),
             new RGBAFunction(),
             new LeftRightScalarFunction(isLeft: false),
             new LeftRightScalarTableFunction(isLeft: false),
             new LeftRightTableScalarFunction(isLeft: false),
             new LeftRightTableTableFunction(isLeft: false),
             new RoundScalarFunction(),
             new RoundDownScalarFunction(),
             new RoundDownTableFunction(),
             new RoundTableFunction(),
             new RoundUpScalarFunction(),
             new RoundUpTableFunction(),
             new SearchFunction(),
             new SecondFunction(),
             new SequenceFunction(),
             new ShowColumnsFunction(),
             new ShuffleFunction(),
             new SinFunction(),
             new SinTableFunction(),
             new SortFunction(),
             new SortByColumnsFunction(),
             new SplitFunction(),
             new SqrtFunction(),
             new SqrtTableFunction(),
             new StartsWithFunction(),
             new StdevPFunction(),
             new StdevPTableFunction(),
             new SubstituteFunction(),
             new SubstituteTFunction(),
             new SumFunction(),
             new SumTableFunction(),
             new SwitchFunction(),
             new TableFunction(),
             new TableFunction_UO(),
             new TanFunction(),
             new TanTableFunction(),
             new TextFunction(),
             new TextFunction_UO(),
             new TimeFunction(),
             new TimeValueFunction(),
             new TimeValueFunction_UO(),
             new TimeZoneOffsetFunction(),
             new TodayFunction(),
             new TraceFunction(),
             new TrimFunction(),
             new TrimEndsFunction(),
             new TrimEndsTFunction(),
             new TrimTFunction(),
             new TruncFunction(),
             new TruncTableFunction(),
             new UniCharFunction(),
             new UniCharTFunction(),
             new LowerUpperFunction(isLower: false),
             new LowerUpperTFunction(isLower: false),
             new ValueFunction(),
             new ValueFunction_UO(),
             new VarPFunction(),
             new VarPTableFunction(),
             new WeekdayFunction(),
             new WeekdaysLongFunction(),
             new WeekdaysShortFunction(),
             new WeekNumFunction(),
             new WithFunction(),
             new YearFunction(),
        };

        // Functions in this list are shared and may show up in other hosts by default.
        internal static readonly TexlFunctionSet _library = new TexlFunctionSet(_functionArray);

        private enum Library : int
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
        }

        public static readonly TexlFunction Abs = _functionArray[(int)Library.Abs];
        public static readonly TexlFunction AbsT = _functionArray[(int)Library.AbsT];
        public static readonly TexlFunction Acos = _functionArray[(int)Library.Acos];
        public static readonly TexlFunction AcosT = _functionArray[(int)Library.AcosT];
        public static readonly TexlFunction Acot = _functionArray[(int)Library.Acot];
        public static readonly TexlFunction AcotT = _functionArray[(int)Library.AcotT];
        public static readonly TexlFunction AddColumns = _functionArray[(int)Library.AddColumns];
        public static readonly TexlFunction AmPm = _functionArray[(int)Library.AmPm];
        public static readonly TexlFunction AmPmShort = _functionArray[(int)Library.AmPmShort];
        public static readonly TexlFunction And = _functionArray[(int)Library.And];
        public static readonly TexlFunction Asin = _functionArray[(int)Library.Asin];
        public static readonly TexlFunction AsinT = _functionArray[(int)Library.AsinT];
        public static readonly TexlFunction AsType = _functionArray[(int)Library.AsType];
        public static readonly TexlFunction Atan = _functionArray[(int)Library.Atan];
        public static readonly TexlFunction Atan2 = _functionArray[(int)Library.Atan2];
        public static readonly TexlFunction AtanT = _functionArray[(int)Library.AtanT];
        public static readonly TexlFunction Average = _functionArray[(int)Library.Average];
        public static readonly TexlFunction AverageT = _functionArray[(int)Library.AverageT];
        public static readonly TexlFunction Blank = _functionArray[(int)Library.Blank];
        public static readonly TexlFunction Boolean = _functionArray[(int)Library.Boolean];
        public static readonly TexlFunction Boolean_T = _functionArray[(int)Library.Boolean_T];
        public static readonly TexlFunction Boolean_UO = _functionArray[(int)Library.Boolean_UO];
        public static readonly TexlFunction BooleanB = _functionArray[(int)Library.BooleanB];
        public static readonly TexlFunction BooleanB_T = _functionArray[(int)Library.BooleanB_T];
        public static readonly TexlFunction BooleanN = _functionArray[(int)Library.BooleanN];
        public static readonly TexlFunction BooleanN_T = _functionArray[(int)Library.BooleanN_T];
        public static readonly TexlFunction BooleanW = _functionArray[(int)Library.BooleanW];
        public static readonly TexlFunction BooleanW_T = _functionArray[(int)Library.BooleanW_T];
        public static readonly TexlFunction Char = _functionArray[(int)Library.Char];
        public static readonly TexlFunction CharT = _functionArray[(int)Library.CharT];
        public static readonly TexlFunction Clock24 = _functionArray[(int)Library.Clock24];
        public static readonly TexlFunction Coalesce = _functionArray[(int)Library.Coalesce];
        public static readonly TexlFunction ColorFade = _functionArray[(int)Library.ColorFade];
        public static readonly TexlFunction ColorFadeT = _functionArray[(int)Library.ColorFadeT];
        public static readonly TexlFunction ColorValue = _functionArray[(int)Library.ColorValue];
        public static readonly TexlFunction ColorValue_UO = _functionArray[(int)Library.ColorValue_UO];
        public static readonly TexlFunction Column_UO = _functionArray[(int)Library.Column_UO];
        public static readonly TexlFunction ColumnNames_UO = _functionArray[(int)Library.ColumnNames_UO];
        public static readonly TexlFunction Concat = _functionArray[(int)Library.Concat];
        public static readonly TexlFunction Concatenate = _functionArray[(int)Library.Concatenate];
        public static readonly TexlFunction ConcatenateT = _functionArray[(int)Library.ConcatenateT];
        public static readonly TexlFunction Cos = _functionArray[(int)Library.Cos];
        public static readonly TexlFunction CosT = _functionArray[(int)Library.CosT];
        public static readonly TexlFunction Cot = _functionArray[(int)Library.Cot];
        public static readonly TexlFunction CotT = _functionArray[(int)Library.CotT];
        public static readonly TexlFunction Count = _functionArray[(int)Library.Count];
        public static readonly TexlFunction CountA = _functionArray[(int)Library.CountA];
        public static readonly TexlFunction CountIf = _functionArray[(int)Library.CountIf];
        public static readonly TexlFunction CountRows = _functionArray[(int)Library.CountRows];
        public static readonly TexlFunction CountRows_UO = _functionArray[(int)Library.CountRows_UO];
        public static readonly TexlFunction Date = _functionArray[(int)Library.Date];
        public static readonly TexlFunction DateAdd = _functionArray[(int)Library.DateAdd];
        public static readonly TexlFunction DateAddT = _functionArray[(int)Library.DateAddT];
        public static readonly TexlFunction DateDiff = _functionArray[(int)Library.DateDiff];
        public static readonly TexlFunction DateDiffT = _functionArray[(int)Library.DateDiffT];
        public static readonly TexlFunction DateTime = _functionArray[(int)Library.DateTime];
        public static readonly TexlFunction DateTimeValue = _functionArray[(int)Library.DateTimeValue];
        public static readonly TexlFunction DateTimeValue_UO = _functionArray[(int)Library.DateTimeValue_UO];
        public static readonly TexlFunction DateValue = _functionArray[(int)Library.DateValue];
        public static readonly TexlFunction DateValue_UO = _functionArray[(int)Library.DateValue_UO];
        public static readonly TexlFunction Day = _functionArray[(int)Library.Day];
        public static readonly TexlFunction Dec2Hex = _functionArray[(int)Library.Dec2Hex];
        public static readonly TexlFunction Dec2HexT = _functionArray[(int)Library.Dec2HexT];
        public static readonly TexlFunction Degrees = _functionArray[(int)Library.Degrees];
        public static readonly TexlFunction DegreesT = _functionArray[(int)Library.DegreesT];
        public static readonly TexlFunction DropColumns = _functionArray[(int)Library.DropColumns];
        public static readonly TexlFunction EDate = _functionArray[(int)Library.EDate];
        public static readonly TexlFunction EOMonth = _functionArray[(int)Library.EOMonth];
        public static readonly TexlFunction EncodeHTML = _functionArray[(int)Library.EncodeHTML];
        public static readonly TexlFunction EncodeUrl = _functionArray[(int)Library.EncodeUrl];
        public static readonly TexlFunction EndsWith = _functionArray[(int)Library.EndsWith];
        public static readonly TexlFunction Error = _functionArray[(int)Library.Error];
        public static readonly TexlFunction Exp = _functionArray[(int)Library.Exp];
        public static readonly TexlFunction ExpT = _functionArray[(int)Library.ExpT];
        public static readonly TexlFunction Filter = _functionArray[(int)Library.Filter];
        public static readonly TexlFunction Find = _functionArray[(int)Library.Find];
        public static readonly TexlFunction FindT = _functionArray[(int)Library.FindT];
        public static readonly TexlFunction First = _functionArray[(int)Library.First];
        public static readonly TexlFunction First_UO = _functionArray[(int)Library.First_UO];
        public static readonly TexlFunction FirstN = _functionArray[(int)Library.FirstN];
        public static readonly TexlFunction FirstN_UO = _functionArray[(int)Library.FirstN_UO];
        public static readonly TexlFunction ForAll = _functionArray[(int)Library.ForAll];
        public static readonly TexlFunction ForAll_UO = _functionArray[(int)Library.ForAll_UO];
        public static readonly TexlFunction GUID_UO = _functionArray[(int)Library.GUID_UO];
        public static readonly TexlFunction GUIDNoArg = _functionArray[(int)Library.GUIDNoArg];
        public static readonly TexlFunction GUIDPure = _functionArray[(int)Library.GUIDPure];
        public static readonly TexlFunction Hex2Dec = _functionArray[(int)Library.Hex2Dec];
        public static readonly TexlFunction Hex2DecT = _functionArray[(int)Library.Hex2DecT];
        public static readonly TexlFunction Hour = _functionArray[(int)Library.Hour];
        public static readonly TexlFunction If = _functionArray[(int)Library.If];
        public static readonly TexlFunction IfError = _functionArray[(int)Library.IfError];
        public static readonly TexlFunction Index = _functionArray[(int)Library.Index];
        public static readonly TexlFunction Index_UO = _functionArray[(int)Library.Index_UO];
        public static readonly TexlFunction Int = _functionArray[(int)Library.Int];
        public static readonly TexlFunction IntT = _functionArray[(int)Library.IntT];
        public static readonly TexlFunction IsBlank = _functionArray[(int)Library.IsBlank];
        public static readonly TexlFunction IsBlankOptionSetValue = _functionArray[(int)Library.IsBlankOptionSetValue];
        public static readonly TexlFunction IsBlankOrError = _functionArray[(int)Library.IsBlankOrError];
        public static readonly TexlFunction IsBlankOrErrorOptionSetValue = _functionArray[(int)Library.IsBlankOrErrorOptionSetValue];
        public static readonly TexlFunction IsEmpty = _functionArray[(int)Library.IsEmpty];
        public static readonly TexlFunction IsError = _functionArray[(int)Library.IsError];
        public static readonly TexlFunction IsNumeric = _functionArray[(int)Library.IsNumeric];
        public static readonly TexlFunction ISOWeekNum = _functionArray[(int)Library.ISOWeekNum];
        public static readonly TexlFunction IsToday = _functionArray[(int)Library.IsToday];
        public static readonly TexlFunction Language = _functionArray[(int)Library.Language];
        public static readonly TexlFunction Last = _functionArray[(int)Library.Last];
        public static readonly TexlFunction Last_UO = _functionArray[(int)Library.Last_UO];
        public static readonly TexlFunction LastN = _functionArray[(int)Library.LastN];
        public static readonly TexlFunction LastN_UO = _functionArray[(int)Library.LastN_UO];
        public static readonly TexlFunction Left = _functionArray[(int)Library.Left];
        public static readonly TexlFunction LeftST = _functionArray[(int)Library.LeftST];
        public static readonly TexlFunction LeftTS = _functionArray[(int)Library.LeftTS];
        public static readonly TexlFunction LeftTT = _functionArray[(int)Library.LeftTT];
        public static readonly TexlFunction Len = _functionArray[(int)Library.Len];
        public static readonly TexlFunction LenT = _functionArray[(int)Library.LenT];
        public static readonly TexlFunction Ln = _functionArray[(int)Library.Ln];
        public static readonly TexlFunction LnT = _functionArray[(int)Library.LnT];
        public static readonly TexlFunction Log = _functionArray[(int)Library.Log];
        public static readonly TexlFunction LogT = _functionArray[(int)Library.LogT];
        public static readonly TexlFunction LookUp = _functionArray[(int)Library.LookUp];
        public static readonly TexlFunction Lower = _functionArray[(int)Library.Lower];
        public static readonly TexlFunction LowerT = _functionArray[(int)Library.LowerT];
        public static readonly TexlFunction Max = _functionArray[(int)Library.Max];
        public static readonly TexlFunction MaxT = _functionArray[(int)Library.MaxT];
        public static readonly TexlFunction Mid = _functionArray[(int)Library.Mid];
        public static readonly TexlFunction MidT = _functionArray[(int)Library.MidT];
        public static readonly TexlFunction Min = _functionArray[(int)Library.Min];
        public static readonly TexlFunction MinT = _functionArray[(int)Library.MinT];
        public static readonly TexlFunction Minute = _functionArray[(int)Library.Minute];
        public static readonly TexlFunction Mod = _functionArray[(int)Library.Mod];
        public static readonly TexlFunction ModT = _functionArray[(int)Library.ModT];
        public static readonly TexlFunction Month = _functionArray[(int)Library.Month];
        public static readonly TexlFunction MonthsLong = _functionArray[(int)Library.MonthsLong];
        public static readonly TexlFunction MonthsShort = _functionArray[(int)Library.MonthsShort];
        public static readonly TexlFunction Not = _functionArray[(int)Library.Not];
        public static readonly TexlFunction Now = _functionArray[(int)Library.Now];
        public static readonly TexlFunction Or = _functionArray[(int)Library.Or];
        public static readonly TexlFunction ParseJSON = _functionArray[(int)Library.ParseJSON];
        public static readonly TexlFunction PatchRecord = _functionArray[(int)Library.PatchRecord];
        public static readonly TexlFunction Pi = _functionArray[(int)Library.Pi];
        public static readonly TexlFunction PlainText = _functionArray[(int)Library.PlainText];
        public static readonly TexlFunction Power = _functionArray[(int)Library.Power];
        public static readonly TexlFunction PowerT = _functionArray[(int)Library.PowerT];
        public static readonly TexlFunction Proper = _functionArray[(int)Library.Proper];
        public static readonly TexlFunction ProperT = _functionArray[(int)Library.ProperT];
        public static readonly TexlFunction Radians = _functionArray[(int)Library.Radians];
        public static readonly TexlFunction RadiansT = _functionArray[(int)Library.RadiansT];
        public static readonly TexlFunction Rand = _functionArray[(int)Library.Rand];
        public static readonly TexlFunction RandBetween = _functionArray[(int)Library.RandBetween];
        public static readonly TexlFunction Refresh = _functionArray[(int)Library.Refresh];
        public static readonly TexlFunction RenameColumns = _functionArray[(int)Library.RenameColumns];
        public static readonly TexlFunction Replace = _functionArray[(int)Library.Replace];
        public static readonly TexlFunction ReplaceT = _functionArray[(int)Library.ReplaceT];
        public static readonly TexlFunction RGBA = _functionArray[(int)Library.RGBA];
        public static readonly TexlFunction Right = _functionArray[(int)Library.Right];
        public static readonly TexlFunction RightST = _functionArray[(int)Library.RightST];
        public static readonly TexlFunction RightTS = _functionArray[(int)Library.RightTS];
        public static readonly TexlFunction RightTT = _functionArray[(int)Library.RightTT];
        public static readonly TexlFunction Round = _functionArray[(int)Library.Round];
        public static readonly TexlFunction RoundDown = _functionArray[(int)Library.RoundDown];
        public static readonly TexlFunction RoundDownT = _functionArray[(int)Library.RoundDownT];
        public static readonly TexlFunction RoundT = _functionArray[(int)Library.RoundT];
        public static readonly TexlFunction RoundUp = _functionArray[(int)Library.RoundUp];
        public static readonly TexlFunction RoundUpT = _functionArray[(int)Library.RoundUpT];
        public static readonly TexlFunction Search = _functionArray[(int)Library.Search];
        public static readonly TexlFunction Second = _functionArray[(int)Library.Second];
        public static readonly TexlFunction Sequence = _functionArray[(int)Library.Sequence];
        public static readonly TexlFunction ShowColumns = _functionArray[(int)Library.ShowColumns];
        public static readonly TexlFunction Shuffle = _functionArray[(int)Library.Shuffle];
        public static readonly TexlFunction Sin = _functionArray[(int)Library.Sin];
        public static readonly TexlFunction SinT = _functionArray[(int)Library.SinT];
        public static readonly TexlFunction Sort = _functionArray[(int)Library.Sort];
        public static readonly TexlFunction SortByColumns = _functionArray[(int)Library.SortByColumns];
        public static readonly TexlFunction Split = _functionArray[(int)Library.Split];
        public static readonly TexlFunction Sqrt = _functionArray[(int)Library.Sqrt];
        public static readonly TexlFunction SqrtT = _functionArray[(int)Library.SqrtT];
        public static readonly TexlFunction StartsWith = _functionArray[(int)Library.StartsWith];
        public static readonly TexlFunction StdevP = _functionArray[(int)Library.StdevP];
        public static readonly TexlFunction StdevPT = _functionArray[(int)Library.StdevPT];
        public static readonly TexlFunction Substitute = _functionArray[(int)Library.Substitute];
        public static readonly TexlFunction SubstituteT = _functionArray[(int)Library.SubstituteT];
        public static readonly TexlFunction Sum = _functionArray[(int)Library.Sum];
        public static readonly TexlFunction SumT = _functionArray[(int)Library.SumT];
        public static readonly TexlFunction Switch = _functionArray[(int)Library.Switch];
        public static readonly TexlFunction Table = _functionArray[(int)Library.Table];
        public static readonly TexlFunction Table_UO = _functionArray[(int)Library.Table_UO];
        public static readonly TexlFunction Tan = _functionArray[(int)Library.Tan];
        public static readonly TexlFunction TanT = _functionArray[(int)Library.TanT];
        public static readonly TexlFunction Text = _functionArray[(int)Library.Text];
        public static readonly TexlFunction Text_UO = _functionArray[(int)Library.Text_UO];
        public static readonly TexlFunction Time = _functionArray[(int)Library.Time];
        public static readonly TexlFunction TimeValue = _functionArray[(int)Library.TimeValue];
        public static readonly TexlFunction TimeValue_UO = _functionArray[(int)Library.TimeValue_UO];
        public static readonly TexlFunction TimeZoneOffset = _functionArray[(int)Library.TimeZoneOffset];
        public static readonly TexlFunction Today = _functionArray[(int)Library.Today];
        public static readonly TexlFunction Trace = _functionArray[(int)Library.Trace];
        public static readonly TexlFunction Trim = _functionArray[(int)Library.Trim];
        public static readonly TexlFunction TrimEnds = _functionArray[(int)Library.TrimEnds];
        public static readonly TexlFunction TrimEndsT = _functionArray[(int)Library.TrimEndsT];
        public static readonly TexlFunction TrimT = _functionArray[(int)Library.TrimT];
        public static readonly TexlFunction Trunc = _functionArray[(int)Library.Trunc];
        public static readonly TexlFunction TruncT = _functionArray[(int)Library.TruncT];
        public static readonly TexlFunction UniChar = _functionArray[(int)Library.UniChar];
        public static readonly TexlFunction UniCharT = _functionArray[(int)Library.UniCharT];
        public static readonly TexlFunction Upper = _functionArray[(int)Library.Upper];
        public static readonly TexlFunction UpperT = _functionArray[(int)Library.UpperT];
        public static readonly TexlFunction Value = _functionArray[(int)Library.Value];
        public static readonly TexlFunction Value_UO = _functionArray[(int)Library.Value_UO];
        public static readonly TexlFunction VarP = _functionArray[(int)Library.VarP];
        public static readonly TexlFunction VarPT = _functionArray[(int)Library.VarPT];
        public static readonly TexlFunction Weekday = _functionArray[(int)Library.Weekday];
        public static readonly TexlFunction WeekdaysLong = _functionArray[(int)Library.WeekdaysLong];
        public static readonly TexlFunction WeekdaysShort = _functionArray[(int)Library.WeekdaysShort];
        public static readonly TexlFunction WeekNum = _functionArray[(int)Library.WeekNum];
        public static readonly TexlFunction With = _functionArray[(int)Library.With];
        public static readonly TexlFunction Year = _functionArray[(int)Library.Year];

        private static readonly TexlFunction[] _featureGateFunctionArray = new TexlFunction[]
        {
            new DecimalFunction(),
            new DecimalFunction_UO(),
            new FloatFunction(),
            new FloatFunction_UO(),
            new IsUTCTodayFunction(),
            new UTCNowFunction(),
            new UTCTodayFunction(),
            new BooleanLFunction(),
            new BooleanLFunction_T(),
            new SummarizeFunction(),
        };

        // _featureGateFunctions functions, not present in all platforms
        internal static readonly TexlFunctionSet _featureGateFunctions = new TexlFunctionSet(_featureGateFunctionArray);

        private enum FeaatureGateFunctions
        {
            Decimal = 0,
            Decimal_UO,
            Float,
            Float_UO,
            IsUTCToday,
            UTCNow,
            UTCToday,
            BooleanL,
            BooleanL_T,
            Summarize
        }

        public static readonly TexlFunction Decimal = _featureGateFunctionArray[(int)FeaatureGateFunctions.Decimal];
        public static readonly TexlFunction Decimal_UO = _featureGateFunctionArray[(int)FeaatureGateFunctions.Decimal_UO];
        public static readonly TexlFunction Float = _featureGateFunctionArray[(int)FeaatureGateFunctions.Float];
        public static readonly TexlFunction Float_UO = _featureGateFunctionArray[(int)FeaatureGateFunctions.Float_UO];
        public static readonly TexlFunction IsUTCToday = _featureGateFunctionArray[(int)FeaatureGateFunctions.IsUTCToday];
        public static readonly TexlFunction UTCNow = _featureGateFunctionArray[(int)FeaatureGateFunctions.UTCNow];
        public static readonly TexlFunction UTCToday = _featureGateFunctionArray[(int)FeaatureGateFunctions.UTCToday];
        public static readonly TexlFunction BooleanL = _featureGateFunctionArray[(int)FeaatureGateFunctions.BooleanL];
        public static readonly TexlFunction BooleanL_T = _featureGateFunctionArray[(int)FeaatureGateFunctions.BooleanL_T];
        public static readonly TexlFunction Summarize = _featureGateFunctionArray[(int)FeaatureGateFunctions.Summarize];

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
