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
            Dictionary<Library, TexlFunction> functionDictionary = new Dictionary<Library, TexlFunction>()
            {
                { Library.Abs, new AbsFunction() },
                { Library.AbsT, new AbsTableFunction() },
                { Library.Acos, new AcosFunction() },
                { Library.AcosT, new AcosTableFunction() },
                { Library.Acot, new AcotFunction() },
                { Library.AcotT, new AcotTableFunction() },
                { Library.AddColumns, new AddColumnsFunction() },
                { Library.AmPm, new AmPmFunction() },
                { Library.AmPmShort, new AmPmShortFunction() },
                { Library.And, new VariadicLogicalFunction(isAnd: true) },
                { Library.Asin, new AsinFunction() },
                { Library.AsinT, new AsinTableFunction() },
                { Library.AsType, new AsTypeFunction() },
                { Library.Atan, new AtanFunction() },
                { Library.Atan2, new Atan2Function() },
                { Library.AtanT, new AtanTableFunction() },
                { Library.Average, new AverageFunction() },
                { Library.AverageT, new AverageTableFunction() },
                { Library.Blank, new BlankFunction() },
                { Library.Boolean, new BooleanFunction() },
                { Library.Boolean_T, new BooleanFunction_T() },
                { Library.Boolean_UO, new BooleanFunction_UO() },
                { Library.BooleanB, new BooleanBFunction() },
                { Library.BooleanB_T, new BooleanBFunction_T() },
                { Library.BooleanN, new BooleanNFunction() },
                { Library.BooleanN_T, new BooleanNFunction_T() },
                { Library.BooleanW, new BooleanWFunction() },
                { Library.BooleanW_T, new BooleanWFunction_T() },
                { Library.Char, new CharFunction() },
                { Library.CharT, new CharTFunction() },
                { Library.Clock24, new IsClock24Function() },
                { Library.Coalesce, new CoalesceFunction() },
                { Library.ColorFade, new ColorFadeFunction() },
                { Library.ColorFadeT, new ColorFadeTFunction() },
                { Library.ColorValue, new ColorValueFunction() },
                { Library.ColorValue_UO, new ColorValueFunction_UO() },
                { Library.Column_UO, new ColumnFunction_UO() },
                { Library.ColumnNames_UO, new ColumnNamesFunction_UO() },
                { Library.Concat, new ConcatFunction() },
                { Library.Concatenate, new ConcatenateFunction() },
                { Library.ConcatenateT, new ConcatenateTableFunction() },
                { Library.Cos, new CosFunction() },
                { Library.CosT, new CosTableFunction() },
                { Library.Cot, new CotFunction() },
                { Library.CotT, new CotTableFunction() },
                { Library.Count, new CountFunction() },
                { Library.CountA, new CountAFunction() },
                { Library.CountIf, new CountIfFunction() },
                { Library.CountRows, new CountRowsFunction() },
                { Library.CountRows_UO, new CountRowsFunction_UO() },
                { Library.Date, new DateFunction() },
                { Library.DateAdd, new DateAddFunction() },
                { Library.DateAddT, new DateAddTFunction() },
                { Library.DateDiff, new DateDiffFunction() },
                { Library.DateDiffT, new DateDiffTFunction() },
                { Library.DateTime, new DateTimeFunction() },
                { Library.DateTimeValue, new DateTimeValueFunction() },
                { Library.DateTimeValue_UO, new DateTimeValueFunction_UO() },
                { Library.DateValue, new DateValueFunction() },
                { Library.DateValue_UO, new DateValueFunction_UO() },
                { Library.Day, new DayFunction() },
                { Library.Dec2Hex, new Dec2HexFunction() },
                { Library.Dec2HexT, new Dec2HexTFunction() },
                { Library.Degrees, new DegreesFunction() },
                { Library.DegreesT, new DegreesTableFunction() },
                { Library.DropColumns, new DropColumnsFunction() },
                { Library.EDate, new EDateFunction() },
                { Library.EOMonth, new EOMonthFunction() },
                { Library.EncodeHTML, new EncodeHTMLFunction() },
                { Library.EncodeUrl, new EncodeUrlFunction() },
                { Library.EndsWith, new EndsWithFunction() },
                { Library.Error, new ErrorFunction() },
                { Library.Exp, new ExpFunction() },
                { Library.ExpT, new ExpTableFunction() },
                { Library.Filter, new FilterFunction() },
                { Library.Find, new FindFunction() },
                { Library.FindT, new FindTFunction() },
                { Library.First, new FirstLastFunction(isFirst: true) },
                { Library.First_UO, new FirstLastFunction_UO(isFirst: true) },
                { Library.FirstN, new FirstLastNFunction(isFirst: true) },
                { Library.FirstN_UO, new FirstLastNFunction_UO(isFirst: true) },
                { Library.ForAll, new ForAllFunction() },
                { Library.ForAll_UO, new ForAllFunction_UO() },
                { Library.GUID_UO, new GUIDPureFunction_UO() },
                { Library.GUIDNoArg, new GUIDNoArgFunction() },
                { Library.GUIDPure, new GUIDPureFunction() },
                { Library.Hex2Dec, new Hex2DecFunction() },
                { Library.Hex2DecT, new Hex2DecTFunction() },
                { Library.Hour, new HourFunction() },
                { Library.If, new IfFunction() },
                { Library.IfError, new IfErrorFunction() },
                { Library.Index, new IndexFunction() },
                { Library.Index_UO, new IndexFunction_UO() },
                { Library.Int, new IntFunction() },
                { Library.IntT, new IntTableFunction() },
                { Library.IsBlank, new IsBlankFunction() },
                { Library.IsBlankOptionSetValue, new IsBlankOptionSetValueFunction() },
                { Library.IsBlankOrError, new IsBlankOrErrorFunction() },
                { Library.IsBlankOrErrorOptionSetValue, new IsBlankOrErrorOptionSetValueFunction() },
                { Library.IsEmpty, new IsEmptyFunction() },
                { Library.IsError, new IsErrorFunction() },
                { Library.IsNumeric, new IsNumericFunction() },
                { Library.ISOWeekNum, new ISOWeekNumFunction() },
                { Library.IsToday, new IsTodayFunction() },
                { Library.Language, new LanguageFunction() },
                { Library.Last, new FirstLastFunction(isFirst: false) },
                { Library.Last_UO, new FirstLastFunction_UO(isFirst: false) },
                { Library.LastN, new FirstLastNFunction(isFirst: false) },
                { Library.LastN_UO, new FirstLastNFunction_UO(isFirst: false) },
                { Library.Left, new LeftRightScalarFunction(isLeft: true) },
                { Library.LeftST, new LeftRightScalarTableFunction(isLeft: true) },
                { Library.LeftTS, new LeftRightTableScalarFunction(isLeft: true) },
                { Library.LeftTT, new LeftRightTableTableFunction(isLeft: true) },
                { Library.Len, new LenFunction() },
                { Library.LenT, new LenTFunction() },
                { Library.Ln, new LnFunction() },
                { Library.LnT, new LnTableFunction() },
                { Library.Log, new LogFunction() },
                { Library.LogT, new LogTFunction() },
                { Library.LookUp, new LookUpFunction() },
                { Library.Lower, new LowerUpperFunction(isLower: true) },
                { Library.LowerT, new LowerUpperTFunction(isLower: true) },
                { Library.Max, new MinMaxFunction(isMin: false) },
                { Library.MaxT, new MinMaxTableFunction(isMin: false) },
                { Library.Mid, new MidFunction() },
                { Library.MidT, new MidTFunction() },
                { Library.Min, new MinMaxFunction(isMin: true) },
                { Library.MinT, new MinMaxTableFunction(isMin: true) },
                { Library.Minute, new MinuteFunction() },
                { Library.Mod, new ModFunction() },
                { Library.ModT, new ModTFunction() },
                { Library.Month, new MonthFunction() },
                { Library.MonthsLong, new MonthsLongFunction() },
                { Library.MonthsShort, new MonthsShortFunction() },
                { Library.Not, new NotFunction() },
                { Library.Now, new NowFunction() },
                { Library.Or, new VariadicLogicalFunction(isAnd: false) },
                { Library.ParseJSON, new ParseJSONFunction() },
                { Library.PatchRecord, new PatchRecordFunction() },
                { Library.Pi, new PiFunction() },
                { Library.PlainText, new PlainTextFunction() },
                { Library.Power, new PowerFunction() },
                { Library.PowerT, new PowerTFunction() },
                { Library.Proper, new ProperFunction() },
                { Library.ProperT, new ProperTFunction() },
                { Library.Radians, new RadiansFunction() },
                { Library.RadiansT, new RadiansTableFunction() },
                { Library.Rand, new RandFunction() },
                { Library.RandBetween, new RandBetweenFunction() },
                { Library.Refresh, new RefreshFunction() },
                { Library.RenameColumns, new RenameColumnsFunction() },
                { Library.Replace, new ReplaceFunction() },
                { Library.ReplaceT, new ReplaceTFunction() },
                { Library.RGBA, new RGBAFunction() },
                { Library.Right, new LeftRightScalarFunction(isLeft: false) },
                { Library.RightST, new LeftRightScalarTableFunction(isLeft: false) },
                { Library.RightTS, new LeftRightTableScalarFunction(isLeft: false) },
                { Library.RightTT, new LeftRightTableTableFunction(isLeft: false) },
                { Library.Round, new RoundScalarFunction() },
                { Library.RoundDown, new RoundDownScalarFunction() },
                { Library.RoundDownT, new RoundDownTableFunction() },
                { Library.RoundT, new RoundTableFunction() },
                { Library.RoundUp, new RoundUpScalarFunction() },
                { Library.RoundUpT, new RoundUpTableFunction() },
                { Library.Search, new SearchFunction() },
                { Library.Second, new SecondFunction() },
                { Library.Sequence, new SequenceFunction() },
                { Library.ShowColumns, new ShowColumnsFunction() },
                { Library.Shuffle, new ShuffleFunction() },
                { Library.Sin, new SinFunction() },
                { Library.SinT, new SinTableFunction() },
                { Library.Sort, new SortFunction() },
                { Library.SortByColumns, new SortByColumnsFunction() },
                { Library.Split, new SplitFunction() },
                { Library.Sqrt, new SqrtFunction() },
                { Library.SqrtT, new SqrtTableFunction() },
                { Library.StartsWith, new StartsWithFunction() },
                { Library.StdevP, new StdevPFunction() },
                { Library.StdevPT, new StdevPTableFunction() },
                { Library.Substitute, new SubstituteFunction() },
                { Library.SubstituteT, new SubstituteTFunction() },
                { Library.Sum, new SumFunction() },
                { Library.SumT, new SumTableFunction() },
                { Library.Switch, new SwitchFunction() },
                { Library.Table, new TableFunction() },
                { Library.Table_UO, new TableFunction_UO() },
                { Library.Tan, new TanFunction() },
                { Library.TanT, new TanTableFunction() },
                { Library.Text, new TextFunction() },
                { Library.Text_UO, new TextFunction_UO() },
                { Library.Time, new TimeFunction() },
                { Library.TimeValue, new TimeValueFunction() },
                { Library.TimeValue_UO, new TimeValueFunction_UO() },
                { Library.TimeZoneOffset, new TimeZoneOffsetFunction() },
                { Library.Today, new TodayFunction() },
                { Library.Trace, new TraceFunction() },
                { Library.Trim, new TrimFunction() },
                { Library.TrimEnds, new TrimEndsFunction() },
                { Library.TrimEndsT, new TrimEndsTFunction() },
                { Library.TrimT, new TrimTFunction() },
                { Library.Trunc, new TruncFunction() },
                { Library.TruncT, new TruncTableFunction() },
                { Library.UniChar, new UniCharFunction() },
                { Library.UniCharT, new UniCharTFunction() },
                { Library.Upper, new LowerUpperFunction(isLower: false) },
                { Library.UpperT, new LowerUpperTFunction(isLower: false) },
                { Library.Value, new ValueFunction() },
                { Library.Value_UO, new ValueFunction_UO() },
                { Library.VarP, new VarPFunction() },
                { Library.VarPT, new VarPTableFunction() },
                { Library.Weekday, new WeekdayFunction() },
                { Library.WeekdaysLong, new WeekdaysLongFunction() },
                { Library.WeekdaysShort, new WeekdaysShortFunction() },
                { Library.WeekNum, new WeekNumFunction() },
                { Library.With, new WithFunction() },
                { Library.Year, new YearFunction() }
            };

            _library = new TexlFunctionSet(functionDictionary.Values);

            Abs = functionDictionary[Library.Abs];
            AbsT = functionDictionary[Library.AbsT];
            Acos = functionDictionary[Library.Acos];
            AcosT = functionDictionary[Library.AcosT];
            Acot = functionDictionary[Library.Acot];
            AcotT = functionDictionary[Library.AcotT];
            AddColumns = functionDictionary[Library.AddColumns];
            AmPm = functionDictionary[Library.AmPm];
            AmPmShort = functionDictionary[Library.AmPmShort];
            And = functionDictionary[Library.And];
            Asin = functionDictionary[Library.Asin];
            AsinT = functionDictionary[Library.AsinT];
            AsType = functionDictionary[Library.AsType];
            Atan = functionDictionary[Library.Atan];
            Atan2 = functionDictionary[Library.Atan2];
            AtanT = functionDictionary[Library.AtanT];
            Average = functionDictionary[Library.Average];
            AverageT = functionDictionary[Library.AverageT];
            Blank = functionDictionary[Library.Blank];
            Boolean = functionDictionary[Library.Boolean];
            Boolean_T = functionDictionary[Library.Boolean_T];
            Boolean_UO = functionDictionary[Library.Boolean_UO];
            BooleanB = functionDictionary[Library.BooleanB];
            BooleanB_T = functionDictionary[Library.BooleanB_T];
            BooleanN = functionDictionary[Library.BooleanN];
            BooleanN_T = functionDictionary[Library.BooleanN_T];
            BooleanW = functionDictionary[Library.BooleanW];
            BooleanW_T = functionDictionary[Library.BooleanW_T];
            Char = functionDictionary[Library.Char];
            CharT = functionDictionary[Library.CharT];
            Clock24 = functionDictionary[Library.Clock24];
            Coalesce = functionDictionary[Library.Coalesce];
            ColorFade = functionDictionary[Library.ColorFade];
            ColorFadeT = functionDictionary[Library.ColorFadeT];
            ColorValue = functionDictionary[Library.ColorValue];
            ColorValue_UO = functionDictionary[Library.ColorValue_UO];
            Column_UO = functionDictionary[Library.Column_UO];
            ColumnNames_UO = functionDictionary[Library.ColumnNames_UO];
            Concat = functionDictionary[Library.Concat];
            Concatenate = functionDictionary[Library.Concatenate];
            ConcatenateT = functionDictionary[Library.ConcatenateT];
            Cos = functionDictionary[Library.Cos];
            CosT = functionDictionary[Library.CosT];
            Cot = functionDictionary[Library.Cot];
            CotT = functionDictionary[Library.CotT];
            Count = functionDictionary[Library.Count];
            CountA = functionDictionary[Library.CountA];
            CountIf = functionDictionary[Library.CountIf];
            CountRows = functionDictionary[Library.CountRows];
            CountRows_UO = functionDictionary[Library.CountRows_UO];
            Date = functionDictionary[Library.Date];
            DateAdd = functionDictionary[Library.DateAdd];
            DateAddT = functionDictionary[Library.DateAddT];
            DateDiff = functionDictionary[Library.DateDiff];
            DateDiffT = functionDictionary[Library.DateDiffT];
            DateTime = functionDictionary[Library.DateTime];
            DateTimeValue = functionDictionary[Library.DateTimeValue];
            DateTimeValue_UO = functionDictionary[Library.DateTimeValue_UO];
            DateValue = functionDictionary[Library.DateValue];
            DateValue_UO = functionDictionary[Library.DateValue_UO];
            Day = functionDictionary[Library.Day];
            Dec2Hex = functionDictionary[Library.Dec2Hex];
            Dec2HexT = functionDictionary[Library.Dec2HexT];
            Degrees = functionDictionary[Library.Degrees];
            DegreesT = functionDictionary[Library.DegreesT];
            DropColumns = functionDictionary[Library.DropColumns];
            EDate = functionDictionary[Library.EDate];
            EOMonth = functionDictionary[Library.EOMonth];
            EncodeHTML = functionDictionary[Library.EncodeHTML];
            EncodeUrl = functionDictionary[Library.EncodeUrl];
            EndsWith = functionDictionary[Library.EndsWith];
            Error = functionDictionary[Library.Error];
            Exp = functionDictionary[Library.Exp];
            ExpT = functionDictionary[Library.ExpT];
            Filter = functionDictionary[Library.Filter];
            Find = functionDictionary[Library.Find];
            FindT = functionDictionary[Library.FindT];
            First = functionDictionary[Library.First];
            First_UO = functionDictionary[Library.First_UO];
            FirstN = functionDictionary[Library.FirstN];
            FirstN_UO = functionDictionary[Library.FirstN_UO];
            ForAll = functionDictionary[Library.ForAll];
            ForAll_UO = functionDictionary[Library.ForAll_UO];
            GUID_UO = functionDictionary[Library.GUID_UO];
            GUIDNoArg = functionDictionary[Library.GUIDNoArg];
            GUIDPure = functionDictionary[Library.GUIDPure];
            Hex2Dec = functionDictionary[Library.Hex2Dec];
            Hex2DecT = functionDictionary[Library.Hex2DecT];
            Hour = functionDictionary[Library.Hour];
            If = functionDictionary[Library.If];
            IfError = functionDictionary[Library.IfError];
            Index = functionDictionary[Library.Index];
            Index_UO = functionDictionary[Library.Index_UO];
            Int = functionDictionary[Library.Int];
            IntT = functionDictionary[Library.IntT];
            IsBlank = functionDictionary[Library.IsBlank];
            IsBlankOptionSetValue = functionDictionary[Library.IsBlankOptionSetValue];
            IsBlankOrError = functionDictionary[Library.IsBlankOrError];
            IsBlankOrErrorOptionSetValue = functionDictionary[Library.IsBlankOrErrorOptionSetValue];
            IsEmpty = functionDictionary[Library.IsEmpty];
            IsError = functionDictionary[Library.IsError];
            IsNumeric = functionDictionary[Library.IsNumeric];
            ISOWeekNum = functionDictionary[Library.ISOWeekNum];
            IsToday = functionDictionary[Library.IsToday];
            Language = functionDictionary[Library.Language];
            Last = functionDictionary[Library.Last];
            Last_UO = functionDictionary[Library.Last_UO];
            LastN = functionDictionary[Library.LastN];
            LastN_UO = functionDictionary[Library.LastN_UO];
            Left = functionDictionary[Library.Left];
            LeftST = functionDictionary[Library.LeftST];
            LeftTS = functionDictionary[Library.LeftTS];
            LeftTT = functionDictionary[Library.LeftTT];
            Len = functionDictionary[Library.Len];
            LenT = functionDictionary[Library.LenT];
            Ln = functionDictionary[Library.Ln];
            LnT = functionDictionary[Library.LnT];
            Log = functionDictionary[Library.Log];
            LogT = functionDictionary[Library.LogT];
            LookUp = functionDictionary[Library.LookUp];
            Lower = functionDictionary[Library.Lower];
            LowerT = functionDictionary[Library.LowerT];
            Max = functionDictionary[Library.Max];
            MaxT = functionDictionary[Library.MaxT];
            Mid = functionDictionary[Library.Mid];
            MidT = functionDictionary[Library.MidT];
            Min = functionDictionary[Library.Min];
            MinT = functionDictionary[Library.MinT];
            Minute = functionDictionary[Library.Minute];
            Mod = functionDictionary[Library.Mod];
            ModT = functionDictionary[Library.ModT];
            Month = functionDictionary[Library.Month];
            MonthsLong = functionDictionary[Library.MonthsLong];
            MonthsShort = functionDictionary[Library.MonthsShort];
            Not = functionDictionary[Library.Not];
            Now = functionDictionary[Library.Now];
            Or = functionDictionary[Library.Or];
            ParseJSON = functionDictionary[Library.ParseJSON];
            PatchRecord = functionDictionary[Library.PatchRecord];
            Pi = functionDictionary[Library.Pi];
            PlainText = functionDictionary[Library.PlainText];
            Power = functionDictionary[Library.Power];
            PowerT = functionDictionary[Library.PowerT];
            Proper = functionDictionary[Library.Proper];
            ProperT = functionDictionary[Library.ProperT];
            Radians = functionDictionary[Library.Radians];
            RadiansT = functionDictionary[Library.RadiansT];
            Rand = functionDictionary[Library.Rand];
            RandBetween = functionDictionary[Library.RandBetween];
            Refresh = functionDictionary[Library.Refresh];
            RenameColumns = functionDictionary[Library.RenameColumns];
            Replace = functionDictionary[Library.Replace];
            ReplaceT = functionDictionary[Library.ReplaceT];
            RGBA = functionDictionary[Library.RGBA];
            Right = functionDictionary[Library.Right];
            RightST = functionDictionary[Library.RightST];
            RightTS = functionDictionary[Library.RightTS];
            RightTT = functionDictionary[Library.RightTT];
            Round = functionDictionary[Library.Round];
            RoundDown = functionDictionary[Library.RoundDown];
            RoundDownT = functionDictionary[Library.RoundDownT];
            RoundT = functionDictionary[Library.RoundT];
            RoundUp = functionDictionary[Library.RoundUp];
            RoundUpT = functionDictionary[Library.RoundUpT];
            Search = functionDictionary[Library.Search];
            Second = functionDictionary[Library.Second];
            Sequence = functionDictionary[Library.Sequence];
            ShowColumns = functionDictionary[Library.ShowColumns];
            Shuffle = functionDictionary[Library.Shuffle];
            Sin = functionDictionary[Library.Sin];
            SinT = functionDictionary[Library.SinT];
            Sort = functionDictionary[Library.Sort];
            SortByColumns = functionDictionary[Library.SortByColumns];
            Split = functionDictionary[Library.Split];
            Sqrt = functionDictionary[Library.Sqrt];
            SqrtT = functionDictionary[Library.SqrtT];
            StartsWith = functionDictionary[Library.StartsWith];
            StdevP = functionDictionary[Library.StdevP];
            StdevPT = functionDictionary[Library.StdevPT];
            Substitute = functionDictionary[Library.Substitute];
            SubstituteT = functionDictionary[Library.SubstituteT];
            Sum = functionDictionary[Library.Sum];
            SumT = functionDictionary[Library.SumT];
            Switch = functionDictionary[Library.Switch];
            Table = functionDictionary[Library.Table];
            Table_UO = functionDictionary[Library.Table_UO];
            Tan = functionDictionary[Library.Tan];
            TanT = functionDictionary[Library.TanT];
            Text = functionDictionary[Library.Text];
            Text_UO = functionDictionary[Library.Text_UO];
            Time = functionDictionary[Library.Time];
            TimeValue = functionDictionary[Library.TimeValue];
            TimeValue_UO = functionDictionary[Library.TimeValue_UO];
            TimeZoneOffset = functionDictionary[Library.TimeZoneOffset];
            Today = functionDictionary[Library.Today];
            Trace = functionDictionary[Library.Trace];
            Trim = functionDictionary[Library.Trim];
            TrimEnds = functionDictionary[Library.TrimEnds];
            TrimEndsT = functionDictionary[Library.TrimEndsT];
            TrimT = functionDictionary[Library.TrimT];
            Trunc = functionDictionary[Library.Trunc];
            TruncT = functionDictionary[Library.TruncT];
            UniChar = functionDictionary[Library.UniChar];
            UniCharT = functionDictionary[Library.UniCharT];
            Upper = functionDictionary[Library.Upper];
            UpperT = functionDictionary[Library.UpperT];
            Value = functionDictionary[Library.Value];
            Value_UO = functionDictionary[Library.Value_UO];
            VarP = functionDictionary[Library.VarP];
            VarPT = functionDictionary[Library.VarPT];
            Weekday = functionDictionary[Library.Weekday];
            WeekdaysLong = functionDictionary[Library.WeekdaysLong];
            WeekdaysShort = functionDictionary[Library.WeekdaysShort];
            WeekNum = functionDictionary[Library.WeekNum];
            With = functionDictionary[Library.With];
            Year = functionDictionary[Library.Year];

            Dictionary<FeatureGateFunctions, TexlFunction> featureGateFunctionDictionary = new Dictionary<FeatureGateFunctions, TexlFunction>()
            {
                { FeatureGateFunctions.Decimal, new DecimalFunction() },
                { FeatureGateFunctions.Decimal_UO, new DecimalFunction_UO() },
                { FeatureGateFunctions.Float, new FloatFunction() },
                { FeatureGateFunctions.Float_UO, new FloatFunction_UO() },
                { FeatureGateFunctions.IsUTCToday, new IsUTCTodayFunction() },
                { FeatureGateFunctions.UTCNow, new UTCNowFunction() },
                { FeatureGateFunctions.UTCToday, new UTCTodayFunction() },
                { FeatureGateFunctions.BooleanL, new BooleanLFunction() },
                { FeatureGateFunctions.BooleanL_T, new BooleanLFunction_T() },
                { FeatureGateFunctions.Summarize, new SummarizeFunction() }
            };

            _featureGateFunctions = new TexlFunctionSet(featureGateFunctionDictionary.Values);

            Decimal = featureGateFunctionDictionary[FeatureGateFunctions.Decimal];
            Decimal_UO = featureGateFunctionDictionary[FeatureGateFunctions.Decimal_UO];
            Float = featureGateFunctionDictionary[FeatureGateFunctions.Float];
            Float_UO = featureGateFunctionDictionary[FeatureGateFunctions.Float_UO];
            IsUTCToday = featureGateFunctionDictionary[FeatureGateFunctions.IsUTCToday];
            UTCNow = featureGateFunctionDictionary[FeatureGateFunctions.UTCNow];
            UTCToday = featureGateFunctionDictionary[FeatureGateFunctions.UTCToday];
            BooleanL = featureGateFunctionDictionary[FeatureGateFunctions.BooleanL];
            BooleanL_T = featureGateFunctionDictionary[FeatureGateFunctions.BooleanL_T];
            Summarize = featureGateFunctionDictionary[FeatureGateFunctions.Summarize];

            // Slow API, only use for backward compatibility
#pragma warning disable CS0618 // Type or member is obsolete
            _testOnlyLibrary = new TexlFunctionSet(_library.Functions).Add(_featureGateFunctions);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        // Functions in this list are shared and may show up in other hosts by default.
        internal static readonly TexlFunctionSet _library;

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

        private enum FeatureGateFunctions : int
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
