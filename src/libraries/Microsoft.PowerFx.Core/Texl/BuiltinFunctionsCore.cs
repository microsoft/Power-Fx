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

        internal static readonly TexlFunction[] __library = new TexlFunction[]
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
        internal static readonly TexlFunctionSet _library = new TexlFunctionSet(__library);

        public static readonly TexlFunction Abs = __library[0];
        public static readonly TexlFunction AbsT = __library[1];
        public static readonly TexlFunction Acos = __library[2];
        public static readonly TexlFunction AcosT = __library[3];
        public static readonly TexlFunction Acot = __library[4];
        public static readonly TexlFunction AcotT = __library[5];
        public static readonly TexlFunction AddColumns = __library[6];
        public static readonly TexlFunction AmPm = __library[7];
        public static readonly TexlFunction AmPmShort = __library[8];
        public static readonly TexlFunction And = __library[9];
        public static readonly TexlFunction Asin = __library[10];
        public static readonly TexlFunction AsinT = __library[11];
        public static readonly TexlFunction AsType = __library[12];
        public static readonly TexlFunction Atan = __library[13];
        public static readonly TexlFunction Atan2 = __library[14];
        public static readonly TexlFunction AtanT = __library[15];
        public static readonly TexlFunction Average = __library[16];
        public static readonly TexlFunction AverageT = __library[17];
        public static readonly TexlFunction Blank = __library[18];
        public static readonly TexlFunction Boolean = __library[19];
        public static readonly TexlFunction Boolean_T = __library[20];
        public static readonly TexlFunction Boolean_UO = __library[21];
        public static readonly TexlFunction BooleanB = __library[22];
        public static readonly TexlFunction BooleanB_T = __library[23];
        public static readonly TexlFunction BooleanN = __library[24];
        public static readonly TexlFunction BooleanN_T = __library[25];
        public static readonly TexlFunction BooleanW = __library[26];
        public static readonly TexlFunction BooleanW_T = __library[27];
        public static readonly TexlFunction Char = __library[28];
        public static readonly TexlFunction CharT = __library[29];
        public static readonly TexlFunction Clock24 = __library[30];
        public static readonly TexlFunction Coalesce = __library[31];
        public static readonly TexlFunction ColorFade = __library[32];
        public static readonly TexlFunction ColorFadeT = __library[33];
        public static readonly TexlFunction ColorValue = __library[34];
        public static readonly TexlFunction ColorValue_UO = __library[35];
        public static readonly TexlFunction Column_UO = __library[36];
        public static readonly TexlFunction ColumnNames_UO = __library[37];
        public static readonly TexlFunction Concat = __library[38];
        public static readonly TexlFunction Concatenate = __library[39];
        public static readonly TexlFunction ConcatenateT = __library[40];
        public static readonly TexlFunction Cos = __library[41];
        public static readonly TexlFunction CosT = __library[42];
        public static readonly TexlFunction Cot = __library[43];
        public static readonly TexlFunction CotT = __library[44];
        public static readonly TexlFunction Count = __library[45];
        public static readonly TexlFunction CountA = __library[46];
        public static readonly TexlFunction CountIf = __library[47];
        public static readonly TexlFunction CountRows = __library[48];
        public static readonly TexlFunction CountRows_UO = __library[49];
        public static readonly TexlFunction Date = __library[50];
        public static readonly TexlFunction DateAdd = __library[51];
        public static readonly TexlFunction DateAddT = __library[52];
        public static readonly TexlFunction DateDiff = __library[53];
        public static readonly TexlFunction DateDiffT = __library[54];
        public static readonly TexlFunction DateTime = __library[55];
        public static readonly TexlFunction DateTimeValue = __library[56];
        public static readonly TexlFunction DateTimeValue_UO = __library[57];
        public static readonly TexlFunction DateValue = __library[58];
        public static readonly TexlFunction DateValue_UO = __library[59];
        public static readonly TexlFunction Day = __library[60];
        public static readonly TexlFunction Dec2Hex = __library[61];
        public static readonly TexlFunction Dec2HexT = __library[62];
        public static readonly TexlFunction Degrees = __library[63];
        public static readonly TexlFunction DegreesT = __library[64];
        public static readonly TexlFunction DropColumns = __library[65];
        public static readonly TexlFunction EDate = __library[66];
        public static readonly TexlFunction EOMonth = __library[67];
        public static readonly TexlFunction EncodeHTML = __library[68];
        public static readonly TexlFunction EncodeUrl = __library[69];
        public static readonly TexlFunction EndsWith = __library[70];
        public static readonly TexlFunction Error = __library[71];
        public static readonly TexlFunction Exp = __library[72];
        public static readonly TexlFunction ExpT = __library[73];
        public static readonly TexlFunction Filter = __library[74];
        public static readonly TexlFunction Find = __library[75];
        public static readonly TexlFunction FindT = __library[76];
        public static readonly TexlFunction First = __library[77];
        public static readonly TexlFunction First_UO = __library[78];
        public static readonly TexlFunction FirstN = __library[79];
        public static readonly TexlFunction FirstN_UO = __library[80];
        public static readonly TexlFunction ForAll = __library[81];
        public static readonly TexlFunction ForAll_UO = __library[82];
        public static readonly TexlFunction GUID_UO = __library[83];
        public static readonly TexlFunction GUIDNoArg = __library[84];
        public static readonly TexlFunction GUIDPure = __library[85];
        public static readonly TexlFunction Hex2Dec = __library[86];
        public static readonly TexlFunction Hex2DecT = __library[87];
        public static readonly TexlFunction Hour = __library[88];
        public static readonly TexlFunction If = __library[89];
        public static readonly TexlFunction IfError = __library[90];
        public static readonly TexlFunction Index = __library[91];
        public static readonly TexlFunction Index_UO = __library[92];
        public static readonly TexlFunction Int = __library[93];
        public static readonly TexlFunction IntT = __library[94];
        public static readonly TexlFunction IsBlank = __library[95];
        public static readonly TexlFunction IsBlankOptionSetValue = __library[96];
        public static readonly TexlFunction IsBlankOrError = __library[97];
        public static readonly TexlFunction IsBlankOrErrorOptionSetValue = __library[98];
        public static readonly TexlFunction IsEmpty = __library[99];
        public static readonly TexlFunction IsError = __library[100];
        public static readonly TexlFunction IsNumeric = __library[101];
        public static readonly TexlFunction ISOWeekNum = __library[102];
        public static readonly TexlFunction IsToday = __library[103];
        public static readonly TexlFunction Language = __library[104];
        public static readonly TexlFunction Last = __library[105];
        public static readonly TexlFunction Last_UO = __library[106];
        public static readonly TexlFunction LastN = __library[107];
        public static readonly TexlFunction LastN_UO = __library[108];
        public static readonly TexlFunction Left = __library[109];
        public static readonly TexlFunction LeftST = __library[110];
        public static readonly TexlFunction LeftTS = __library[111];
        public static readonly TexlFunction LeftTT = __library[112];
        public static readonly TexlFunction Len = __library[113];
        public static readonly TexlFunction LenT = __library[114];
        public static readonly TexlFunction Ln = __library[115];
        public static readonly TexlFunction LnT = __library[116];
        public static readonly TexlFunction Log = __library[117];
        public static readonly TexlFunction LogT = __library[118];
        public static readonly TexlFunction LookUp = __library[119];
        public static readonly TexlFunction Lower = __library[120];
        public static readonly TexlFunction LowerT = __library[121];
        public static readonly TexlFunction Max = __library[122];
        public static readonly TexlFunction MaxT = __library[123];
        public static readonly TexlFunction Mid = __library[124];
        public static readonly TexlFunction MidT = __library[125];
        public static readonly TexlFunction Min = __library[126];
        public static readonly TexlFunction MinT = __library[127];
        public static readonly TexlFunction Minute = __library[128];
        public static readonly TexlFunction Mod = __library[129];
        public static readonly TexlFunction ModT = __library[130];
        public static readonly TexlFunction Month = __library[131];
        public static readonly TexlFunction MonthsLong = __library[132];
        public static readonly TexlFunction MonthsShort = __library[133];
        public static readonly TexlFunction Not = __library[134];
        public static readonly TexlFunction Now = __library[135];
        public static readonly TexlFunction Or = __library[136];
        public static readonly TexlFunction ParseJSON = __library[137];
        public static readonly TexlFunction PatchRecord = __library[138];
        public static readonly TexlFunction Pi = __library[139];
        public static readonly TexlFunction PlainText = __library[140];
        public static readonly TexlFunction Power = __library[141];
        public static readonly TexlFunction PowerT = __library[142];
        public static readonly TexlFunction Proper = __library[143];
        public static readonly TexlFunction ProperT = __library[144];
        public static readonly TexlFunction Radians = __library[145];
        public static readonly TexlFunction RadiansT = __library[146];
        public static readonly TexlFunction Rand = __library[147];
        public static readonly TexlFunction RandBetween = __library[148];
        public static readonly TexlFunction Refresh = __library[149];
        public static readonly TexlFunction RenameColumns = __library[150];
        public static readonly TexlFunction Replace = __library[151];
        public static readonly TexlFunction ReplaceT = __library[152];
        public static readonly TexlFunction RGBA = __library[153];
        public static readonly TexlFunction Right = __library[154];
        public static readonly TexlFunction RightST = __library[155];
        public static readonly TexlFunction RightTS = __library[156];
        public static readonly TexlFunction RightTT = __library[157];
        public static readonly TexlFunction Round = __library[158];
        public static readonly TexlFunction RoundDown = __library[159];
        public static readonly TexlFunction RoundDownT = __library[160];
        public static readonly TexlFunction RoundT = __library[161];
        public static readonly TexlFunction RoundUp = __library[162];
        public static readonly TexlFunction RoundUpT = __library[163];
        public static readonly TexlFunction Search = __library[164];
        public static readonly TexlFunction Second = __library[165];
        public static readonly TexlFunction Sequence = __library[166];
        public static readonly TexlFunction ShowColumns = __library[167];
        public static readonly TexlFunction Shuffle = __library[168];
        public static readonly TexlFunction Sin = __library[169];
        public static readonly TexlFunction SinT = __library[170];
        public static readonly TexlFunction Sort = __library[171];
        public static readonly TexlFunction SortByColumns = __library[172];
        public static readonly TexlFunction Split = __library[173];
        public static readonly TexlFunction Sqrt = __library[174];
        public static readonly TexlFunction SqrtT = __library[175];
        public static readonly TexlFunction StartsWith = __library[176];
        public static readonly TexlFunction StdevP = __library[177];
        public static readonly TexlFunction StdevPT = __library[178];
        public static readonly TexlFunction Substitute = __library[179];
        public static readonly TexlFunction SubstituteT = __library[180];
        public static readonly TexlFunction Sum = __library[181];
        public static readonly TexlFunction SumT = __library[182];
        public static readonly TexlFunction Switch = __library[183];
        public static readonly TexlFunction Table = __library[184];
        public static readonly TexlFunction Table_UO = __library[185];
        public static readonly TexlFunction Tan = __library[186];
        public static readonly TexlFunction TanT = __library[187];
        public static readonly TexlFunction Text = __library[188];
        public static readonly TexlFunction Text_UO = __library[189];
        public static readonly TexlFunction Time = __library[190];
        public static readonly TexlFunction TimeValue = __library[191];
        public static readonly TexlFunction TimeValue_UO = __library[192];
        public static readonly TexlFunction TimeZoneOffset = __library[193];
        public static readonly TexlFunction Today = __library[194];
        public static readonly TexlFunction Trace = __library[195];
        public static readonly TexlFunction Trim = __library[196];
        public static readonly TexlFunction TrimEnds = __library[197];
        public static readonly TexlFunction TrimEndsT = __library[198];
        public static readonly TexlFunction TrimT = __library[199];
        public static readonly TexlFunction Trunc = __library[200];
        public static readonly TexlFunction TruncT = __library[201];
        public static readonly TexlFunction UniChar = __library[202];
        public static readonly TexlFunction UniCharT = __library[203];
        public static readonly TexlFunction Upper = __library[204];
        public static readonly TexlFunction UpperT = __library[205];
        public static readonly TexlFunction Value = __library[206];
        public static readonly TexlFunction Value_UO = __library[207];
        public static readonly TexlFunction VarP = __library[208];
        public static readonly TexlFunction VarPT = __library[209];
        public static readonly TexlFunction Weekday = __library[210];
        public static readonly TexlFunction WeekdaysLong = __library[211];
        public static readonly TexlFunction WeekdaysShort = __library[212];
        public static readonly TexlFunction WeekNum = __library[213];
        public static readonly TexlFunction With = __library[214];
        public static readonly TexlFunction Year = __library[215];

        internal static readonly TexlFunction[] __featureGateFunctions = new TexlFunction[]
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
        internal static readonly TexlFunctionSet _featureGateFunctions = new TexlFunctionSet(__featureGateFunctions);

        public static readonly TexlFunction Decimal = __featureGateFunctions[0];
        public static readonly TexlFunction Decimal_UO = __featureGateFunctions[1];
        public static readonly TexlFunction Float = __featureGateFunctions[2];
        public static readonly TexlFunction Float_UO = __featureGateFunctions[3];
        public static readonly TexlFunction IsUTCToday = __featureGateFunctions[4];
        public static readonly TexlFunction UTCNow = __featureGateFunctions[5];
        public static readonly TexlFunction UTCToday = __featureGateFunctions[6];
        public static readonly TexlFunction BooleanL = __featureGateFunctions[7];
        public static readonly TexlFunction BooleanL_T = __featureGateFunctions[8];
        public static readonly TexlFunction Summarize = __featureGateFunctions[9];

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
