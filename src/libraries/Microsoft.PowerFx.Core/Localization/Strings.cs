// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Localization
{
    internal static class TexlStrings
    {
        public delegate string StringGetter(string locale = null);

        public static StringGetter AboutChar = (b) => StringResources.Get("AboutChar", b);
        public static StringGetter CharArg1 = (b) => StringResources.Get("CharArg1", b);
        public static StringGetter AboutCharT = (b) => StringResources.Get("AboutCharT", b);
        public static StringGetter CharTArg1 = (b) => StringResources.Get("CharTArg1", b);

        public static StringGetter AboutIf = (b) => StringResources.Get("AboutIf", b);
        public static StringGetter IfArgCond = (b) => StringResources.Get("IfArgCond", b);
        public static StringGetter IfArgTrueValue = (b) => StringResources.Get("IfArgTrueValue", b);
        public static StringGetter IfArgElseValue = (b) => StringResources.Get("IfArgElseValue", b);

        public static StringGetter AboutSwitch = (b) => StringResources.Get("AboutSwitch", b);
        public static StringGetter SwitchExpression = (b) => StringResources.Get("SwitchExpression", b);
        public static StringGetter SwitchDefaultReturn = (b) => StringResources.Get("SwitchDefaultReturn", b);
        public static StringGetter SwitchCaseExpr = (b) => StringResources.Get("SwitchCaseExpr", b);
        public static StringGetter SwitchCaseArg = (b) => StringResources.Get("SwitchCaseArg", b);

        public static StringGetter AboutAnd = (b) => StringResources.Get("AboutAnd", b);
        public static StringGetter AboutOr = (b) => StringResources.Get("AboutOr", b);
        public static StringGetter AboutNot = (b) => StringResources.Get("AboutNot", b);
        public static StringGetter LogicalFuncParam = (b) => StringResources.Get("LogicalFuncParam", b);

        public static StringGetter AboutCount = (b) => StringResources.Get("AboutCount", b);
        public static StringGetter AboutCountA = (b) => StringResources.Get("AboutCountA", b);
        public static StringGetter AboutCountRows = (b) => StringResources.Get("AboutCountRows", b);
        public static StringGetter CountArg1 = (b) => StringResources.Get("CountArg1", b);

        public static StringGetter AboutCountIf = (b) => StringResources.Get("AboutCountIf", b);
        public static StringGetter CountIfArg1 = (b) => StringResources.Get("CountIfArg1", b);
        public static StringGetter CountIfArg2 = (b) => StringResources.Get("CountIfArg2", b);

        public static StringGetter AboutSet = (b) => StringResources.Get("AboutSet", b);
        public static StringGetter SetArg1 = (b) => StringResources.Get("SetArg1", b);
        public static StringGetter SetArg2 = (b) => StringResources.Get("SetArg2", b);

        public static StringGetter AboutSumT = (b) => StringResources.Get("AboutSumT", b);
        public static StringGetter AboutMaxT = (b) => StringResources.Get("AboutMaxT", b);
        public static StringGetter AboutMinT = (b) => StringResources.Get("AboutMinT", b);
        public static StringGetter AboutAverageT = (b) => StringResources.Get("AboutAverageT", b);
        public static StringGetter StatisticalTArg1 = (b) => StringResources.Get("StatisticalTArg1", b);
        public static StringGetter StatisticalTArg2 = (b) => StringResources.Get("StatisticalTArg2", b);

        public static StringGetter AboutSum = (b) => StringResources.Get("AboutSum", b);
        public static StringGetter AboutMax = (b) => StringResources.Get("AboutMax", b);
        public static StringGetter AboutMin = (b) => StringResources.Get("AboutMin", b);
        public static StringGetter AboutAverage = (b) => StringResources.Get("AboutAverage", b);
        public static StringGetter StatisticalArg = (b) => StringResources.Get("StatisticalArg", b);

        public static StringGetter AboutAddColumns = (b) => StringResources.Get("AboutAddColumns", b);
        public static StringGetter AddColumnsArg1 = (b) => StringResources.Get("AddColumnsArg1", b);
        public static StringGetter AddColumnsArg2 = (b) => StringResources.Get("AddColumnsArg2", b);
        public static StringGetter AddColumnsArg3 = (b) => StringResources.Get("AddColumnsArg3", b);

        public static StringGetter AboutDropColumns = (b) => StringResources.Get("AboutDropColumns", b);
        public static StringGetter DropColumnsArg1 = (b) => StringResources.Get("DropColumnsArg1", b);
        public static StringGetter DropColumnsArg2 = (b) => StringResources.Get("DropColumnsArg2", b);

        public static StringGetter AboutShowColumns = (b) => StringResources.Get("AboutShowColumns", b);
        public static StringGetter ShowColumnsArg1 = (b) => StringResources.Get("ShowColumnsArg1", b);
        public static StringGetter ShowColumnsArg2 = (b) => StringResources.Get("ShowColumnsArg2", b);

        public static StringGetter AboutFilter = (b) => StringResources.Get("AboutFilter", b);
        public static StringGetter FilterArg1 = (b) => StringResources.Get("FilterArg1", b);
        public static StringGetter FilterArg2 = (b) => StringResources.Get("FilterArg2", b);

        public static StringGetter AboutFirst = (b) => StringResources.Get("AboutFirst", b);
        public static StringGetter AboutLast = (b) => StringResources.Get("AboutLast", b);
        public static StringGetter FirstLastArg1 = (b) => StringResources.Get("FirstLastArg1", b);

        public static StringGetter AboutFirstN = (b) => StringResources.Get("AboutFirstN", b);
        public static StringGetter AboutLastN = (b) => StringResources.Get("AboutLastN", b);
        public static StringGetter AboutFirstNW = (b) => StringResources.Get("AboutFirstNW", b);
        public static StringGetter AboutLastNW = (b) => StringResources.Get("AboutLastNW", b);
        public static StringGetter FirstLastNArg1 = (b) => StringResources.Get("FirstLastNArg1", b);
        public static StringGetter FirstLastNArg2 = (b) => StringResources.Get("FirstLastNArg2", b);

        public static StringGetter AboutText = (b) => StringResources.Get("AboutText", b);
        public static StringGetter TextArg1 = (b) => StringResources.Get("TextArg1", b);
        public static StringGetter TextArg2 = (b) => StringResources.Get("TextArg2", b);
        public static StringGetter TextArg3 = (b) => StringResources.Get("TextArg3", b);

        public static StringGetter AboutValue = (b) => StringResources.Get("AboutValue", b);
        public static StringGetter AboutFloat = (b) => StringResources.Get("AboutFloat", b);
        public static StringGetter AboutDecimal = (b) => StringResources.Get("AboutDecimal", b);
        public static StringGetter ValueArg1 = (b) => StringResources.Get("ValueArg1", b);
        public static StringGetter ValueArg2 = (b) => StringResources.Get("ValueArg2", b);

        public static StringGetter AboutBoolean = (b) => StringResources.Get("AboutBoolean", b);
        public static StringGetter BooleanArg1 = (b) => StringResources.Get("BooleanArg1", b);
        public static StringGetter AboutBooleanT = (b) => StringResources.Get("AboutBooleanT", b);
        public static StringGetter BooleanTArg1 = (b) => StringResources.Get("BooleanTArg1", b);

        public static StringGetter AboutBooleanN = (b) => StringResources.Get("AboutBooleanN", b);
        public static StringGetter BooleanNArg1 = (b) => StringResources.Get("BooleanNArg1", b);
        public static StringGetter AboutBooleanNT = (b) => StringResources.Get("AboutBooleanNT", b);
        public static StringGetter BooleanNTArg1 = (b) => StringResources.Get("BooleanNTArg1", b);

        public static StringGetter AboutBooleanW = (b) => StringResources.Get("AboutBooleanW", b);
        public static StringGetter BooleanWArg1 = (b) => StringResources.Get("BooleanWArg1", b);
        public static StringGetter AboutBooleanWT = (b) => StringResources.Get("AboutBooleanWT", b);
        public static StringGetter BooleanWTArg1 = (b) => StringResources.Get("BooleanWTArg1", b);

        public static StringGetter AboutBooleanB = (b) => StringResources.Get("AboutBooleanB", b);
        public static StringGetter BooleanBArg1 = (b) => StringResources.Get("BooleanBArg1", b);
        public static StringGetter AboutBooleanBT = (b) => StringResources.Get("AboutBooleanBT", b);
        public static StringGetter BooleanBTArg1 = (b) => StringResources.Get("BooleanBTArg1", b);

        public static StringGetter AboutConcatenate = (b) => StringResources.Get("AboutConcatenate", b);
        public static StringGetter ConcatenateArg1 = (b) => StringResources.Get("ConcatenateArg1", b);
        public static StringGetter AboutConcatenateT = (b) => StringResources.Get("AboutConcatenateT", b);
        public static StringGetter ConcatenateTArg1 = (b) => StringResources.Get("ConcatenateTArg1", b);

        public static StringGetter AboutCoalesce = (b) => StringResources.Get("AboutCoalesce", b);
        public static StringGetter CoalesceArg1 = (b) => StringResources.Get("CoalesceArg1", b);

        public static StringGetter AboutIfError = (b) => StringResources.Get("AboutIfError", b);
        public static StringGetter IfErrorArg1 = (b) => StringResources.Get("IfErrorArg1", b);
        public static StringGetter IfErrorArg2 = (b) => StringResources.Get("IfErrorArg2", b);

        public static StringGetter AboutError = (b) => StringResources.Get("AboutError", b);
        public static StringGetter ErrorArg1 = (b) => StringResources.Get("ErrorArg1", b);

        public static StringGetter AboutIsError = (b) => StringResources.Get("AboutIsError", b);
        public static StringGetter IsErrorArg = (b) => StringResources.Get("IsErrorArg", b);

        public static StringGetter AboutConcat = (b) => StringResources.Get("AboutConcat", b);
        public static StringGetter ConcatArg1 = (b) => StringResources.Get("ConcatArg1", b);
        public static StringGetter ConcatArg2 = (b) => StringResources.Get("ConcatArg2", b);
        public static StringGetter ConcatArg3 = (b) => StringResources.Get("ConcatArg3", b);

        public static StringGetter AboutLen = (b) => StringResources.Get("AboutLen", b);
        public static StringGetter AboutLenT = (b) => StringResources.Get("AboutLenT", b);
        public static StringGetter LenArg1 = (b) => StringResources.Get("LenArg1", b);
        public static StringGetter LenTArg1 = (b) => StringResources.Get("LenTArg1", b);

        public static StringGetter AboutUpper = (b) => StringResources.Get("AboutUpper", b);
        public static StringGetter AboutUpperT = (b) => StringResources.Get("AboutUpperT", b);
        public static StringGetter AboutLower = (b) => StringResources.Get("AboutLower", b);
        public static StringGetter AboutLowerT = (b) => StringResources.Get("AboutLowerT", b);
        public static StringGetter AboutProper = (b) => StringResources.Get("AboutProper", b);
        public static StringGetter AboutProperT = (b) => StringResources.Get("AboutProperT", b);
        public static StringGetter AboutTrim = (b) => StringResources.Get("AboutTrim", b);
        public static StringGetter AboutTrimEnds = (b) => StringResources.Get("AboutTrimEnds", b);
        public static StringGetter AboutMid = (b) => StringResources.Get("AboutMid", b);
        public static StringGetter AboutMidT = (b) => StringResources.Get("AboutMidT", b);
        public static StringGetter StringFuncArg1 = (b) => StringResources.Get("StringFuncArg1", b);
        public static StringGetter StringTFuncArg1 = (b) => StringResources.Get("StringTFuncArg1", b);
        public static StringGetter StringFuncArg2 = (b) => StringResources.Get("StringFuncArg2", b);
        public static StringGetter StringFuncArg3 = (b) => StringResources.Get("StringFuncArg3", b);

        public static StringGetter AboutReplace = (b) => StringResources.Get("AboutReplace", b);
        public static StringGetter AboutReplaceT = (b) => StringResources.Get("AboutReplaceT", b);
        public static StringGetter ReplaceFuncArg1 = (b) => StringResources.Get("ReplaceFuncArg1", b);
        public static StringGetter ReplaceFuncArg4 = (b) => StringResources.Get("ReplaceFuncArg4", b);

        public static StringGetter AboutSubstitute = (b) => StringResources.Get("AboutSubstitute", b);
        public static StringGetter AboutSubstituteT = (b) => StringResources.Get("AboutSubstituteT", b);
        public static StringGetter SubstituteFuncArg1 = (b) => StringResources.Get("SubstituteFuncArg1", b);
        public static StringGetter SubstituteTFuncArg1 = (b) => StringResources.Get("SubstituteTFuncArg1", b);
        public static StringGetter SubstituteFuncArg2 = (b) => StringResources.Get("SubstituteFuncArg2", b);
        public static StringGetter SubstituteFuncArg3 = (b) => StringResources.Get("SubstituteFuncArg3", b);
        public static StringGetter SubstituteFuncArg4 = (b) => StringResources.Get("SubstituteFuncArg4", b);

        public static StringGetter AboutSort = (b) => StringResources.Get("AboutSort", b);
        public static StringGetter SortArg1 = (b) => StringResources.Get("SortArg1", b);
        public static StringGetter SortArg2 = (b) => StringResources.Get("SortArg2", b);
        public static StringGetter SortArg3 = (b) => StringResources.Get("SortArg3", b);

        public static StringGetter AboutDistinct = (b) => StringResources.Get("AboutDistinct", b);
        public static StringGetter DistinctArg1 = (b) => StringResources.Get("DistinctArg1", b);
        public static StringGetter DistinctArg2 = (b) => StringResources.Get("DistinctArg2", b);

        public static StringGetter AboutSortByColumns = (b) => StringResources.Get("AboutSortByColumns", b);
        public static StringGetter SortByColumnsArg1 = (b) => StringResources.Get("SortByColumnsArg1", b);
        public static StringGetter SortByColumnsArg2 = (b) => StringResources.Get("SortByColumnsArg2", b);
        public static StringGetter SortByColumnsArg3 = (b) => StringResources.Get("SortByColumnsArg3", b);

        public static StringGetter AboutSortByColumnsWithOrderValues = (b) => StringResources.Get("AboutSortByColumnsWithOrderValues", b);
        public static StringGetter SortByColumnsWithOrderValuesArg1 = (b) => StringResources.Get("SortByColumnsWithOrderValuesArg1", b);
        public static StringGetter SortByColumnsWithOrderValuesArg2 = (b) => StringResources.Get("SortByColumnsWithOrderValuesArg2", b);
        public static StringGetter SortByColumnsWithOrderValuesArg3 = (b) => StringResources.Get("SortByColumnsWithOrderValuesArg3", b);

        public static StringGetter AboutRand = (b) => StringResources.Get("AboutRand", b);
        public static StringGetter AboutRandBetween = (b) => StringResources.Get("AboutRandBetween", b);
        public static StringGetter RandBetweenArg1 = (b) => StringResources.Get("RandBetweenArg1", b);
        public static StringGetter RandBetweenArg2 = (b) => StringResources.Get("RandBetweenArg2", b);

        public static StringGetter AboutNow = (b) => StringResources.Get("AboutNow", b);
        public static StringGetter AboutUTCNow = (b) => StringResources.Get("AboutUTCNow", b);
        public static StringGetter AboutToday = (b) => StringResources.Get("AboutToday", b);
        public static StringGetter AboutUTCToday = (b) => StringResources.Get("AboutUTCToday", b);
        public static StringGetter AboutGUID = (b) => StringResources.Get("AboutGUID", b);
        public static StringGetter GUIDArg = (b) => StringResources.Get("GUIDArg", b);

        public static StringGetter AboutTimeZoneOffset = (b) => StringResources.Get("AboutTimeZoneOffset", b);
        public static StringGetter TimeZoneOffsetArg1 = (b) => StringResources.Get("TimeZoneOffsetArg1", b);

        public static StringGetter AboutIsToday = (b) => StringResources.Get("AboutIsToday", b);
        public static StringGetter IsTodayFuncArg1 = (b) => StringResources.Get("IsTodayFuncArg1", b);
        public static StringGetter AboutIsUTCToday = (b) => StringResources.Get("AboutIsUTCToday", b);
        public static StringGetter IsUTCTodayFuncArg1 = (b) => StringResources.Get("IsUTCTodayFuncArg1", b);

        public static StringGetter AboutRound = (b) => StringResources.Get("AboutRound", b);
        public static StringGetter AboutRoundUp = (b) => StringResources.Get("AboutRoundUp", b);
        public static StringGetter AboutRoundDown = (b) => StringResources.Get("AboutRoundDown", b);
        public static StringGetter RoundArg1 = (b) => StringResources.Get("RoundArg1", b);
        public static StringGetter RoundArg2 = (b) => StringResources.Get("RoundArg2", b);

        public static StringGetter AboutRoundT = (b) => StringResources.Get("AboutRoundT", b);
        public static StringGetter AboutRoundUpT = (b) => StringResources.Get("AboutRoundUpT", b);
        public static StringGetter AboutRoundDownT = (b) => StringResources.Get("AboutRoundDownT", b);
        public static StringGetter RoundTArg1 = (b) => StringResources.Get("RoundTArg1", b);
        public static StringGetter RoundTArg2 = (b) => StringResources.Get("RoundTArg2", b);

        public static StringGetter AboutRGBA = (b) => StringResources.Get("AboutRGBA", b);
        public static StringGetter RGBAArg1 = (b) => StringResources.Get("RGBAArg1", b);
        public static StringGetter RGBAArg2 = (b) => StringResources.Get("RGBAArg2", b);
        public static StringGetter RGBAArg3 = (b) => StringResources.Get("RGBAArg3", b);
        public static StringGetter RGBAArg4 = (b) => StringResources.Get("RGBAArg4", b);

        public static StringGetter AboutColorFade = (b) => StringResources.Get("AboutColorFade", b);
        public static StringGetter ColorFadeArg1 = (b) => StringResources.Get("ColorFadeArg1", b);
        public static StringGetter ColorFadeArg2 = (b) => StringResources.Get("ColorFadeArg2", b);

        public static StringGetter AboutColorFadeT = (b) => StringResources.Get("AboutColorFadeT", b);
        public static StringGetter ColorFadeTArg1 = (b) => StringResources.Get("ColorFadeTArg1", b);
        public static StringGetter ColorFadeTArg2 = (b) => StringResources.Get("ColorFadeTArg2", b);

        public static StringGetter AboutAbs = (b) => StringResources.Get("AboutAbs", b);
        public static StringGetter AboutAbsT = (b) => StringResources.Get("AboutAbsT", b);
        public static StringGetter AboutSqrt = (b) => StringResources.Get("AboutSqrt", b);
        public static StringGetter AboutSqrtT = (b) => StringResources.Get("AboutSqrtT", b);
        public static StringGetter MathFuncArg1 = (b) => StringResources.Get("MathFuncArg1", b);
        public static StringGetter MathTFuncArg1 = (b) => StringResources.Get("MathTFuncArg1", b);

        public static StringGetter AboutInt = (b) => StringResources.Get("AboutInt", b);
        public static StringGetter AboutIntT = (b) => StringResources.Get("AboutIntT", b);

        public static StringGetter AboutTrunc = (b) => StringResources.Get("AboutTrunc", b);
        public static StringGetter TruncArg1 = (b) => StringResources.Get("TruncArg1", b);
        public static StringGetter TruncArg2 = (b) => StringResources.Get("TruncArg2", b);

        public static StringGetter AboutTruncT = (b) => StringResources.Get("AboutTruncT", b);
        public static StringGetter TruncTArg1 = (b) => StringResources.Get("TruncTArg1", b);
        public static StringGetter TruncTArg2 = (b) => StringResources.Get("TruncTArg2", b);

        public static StringGetter AboutLeft = (b) => StringResources.Get("AboutLeft", b);
        public static StringGetter AboutRight = (b) => StringResources.Get("AboutRight", b);
        public static StringGetter AboutLeftT = (b) => StringResources.Get("AboutLeftT", b);
        public static StringGetter AboutRightT = (b) => StringResources.Get("AboutRightT", b);
        public static StringGetter LeftRightArg1 = (b) => StringResources.Get("LeftRightArg1", b);
        public static StringGetter LeftRightTArg1 = (b) => StringResources.Get("LeftRightTArg1", b);
        public static StringGetter LeftRightArg2 = (b) => StringResources.Get("LeftRightArg2", b);

        public static StringGetter AboutIsBlank = (b) => StringResources.Get("AboutIsBlank", b);
        public static StringGetter IsBlankArg1 = (b) => StringResources.Get("IsBlankArg1", b);

        public static StringGetter AboutIsBlankOrError = (b) => StringResources.Get("AboutIsBlankOrError", b);
        public static StringGetter IsBlankOrErrorArg1 = (b) => StringResources.Get("IsBlankOrErrorArg1", b);

        public static StringGetter AboutIsEmpty = (b) => StringResources.Get("AboutIsEmpty", b);
        public static StringGetter IsEmptyArg1 = (b) => StringResources.Get("IsEmptyArg1", b);

        public static StringGetter AboutIsNumeric = (b) => StringResources.Get("AboutIsNumeric", b);
        public static StringGetter IsNumericArg1 = (b) => StringResources.Get("IsNumericArg1", b);

        public static StringGetter AboutShuffle = (b) => StringResources.Get("AboutShuffle", b);
        public static StringGetter ShuffleArg1 = (b) => StringResources.Get("ShuffleArg1", b);

        public static StringGetter AboutLookUp = (b) => StringResources.Get("AboutLookUp", b);
        public static StringGetter LookUpArg1 = (b) => StringResources.Get("LookUpArg1", b);
        public static StringGetter LookUpArg2 = (b) => StringResources.Get("LookUpArg2", b);
        public static StringGetter LookUpArg3 = (b) => StringResources.Get("LookUpArg3", b);

        public static StringGetter AboutStdevP = (b) => StringResources.Get("AboutStdevP", b);
        public static StringGetter AboutStdevPT = (b) => StringResources.Get("AboutStdevPT", b);

        public static StringGetter AboutVarP = (b) => StringResources.Get("AboutVarP", b);
        public static StringGetter AboutVarPT = (b) => StringResources.Get("AboutVarPT", b);

        public static StringGetter AboutSin = (b) => StringResources.Get("AboutSin", b);
        public static StringGetter AboutSinT = (b) => StringResources.Get("AboutSinT", b);
        public static StringGetter AboutAsin = (b) => StringResources.Get("AboutAsin", b);
        public static StringGetter AboutAsinT = (b) => StringResources.Get("AboutAsinT", b);
        public static StringGetter AboutCos = (b) => StringResources.Get("AboutCos", b);
        public static StringGetter AboutCosT = (b) => StringResources.Get("AboutCosT", b);
        public static StringGetter AboutAcos = (b) => StringResources.Get("AboutAcos", b);
        public static StringGetter AboutAcosT = (b) => StringResources.Get("AboutAcosT", b);

        public static StringGetter AboutTan = (b) => StringResources.Get("AboutTan", b);
        public static StringGetter AboutTanT = (b) => StringResources.Get("AboutTanT", b);
        public static StringGetter AboutAtan = (b) => StringResources.Get("AboutAtan", b);
        public static StringGetter AboutAtanT = (b) => StringResources.Get("AboutAtanT", b);
        public static StringGetter AboutCot = (b) => StringResources.Get("AboutCot", b);
        public static StringGetter AboutCotT = (b) => StringResources.Get("AboutCotT", b);
        public static StringGetter AboutAcot = (b) => StringResources.Get("AboutAcot", b);
        public static StringGetter AboutAcotT = (b) => StringResources.Get("AboutAcotT", b);

        public static StringGetter AboutLn = (b) => StringResources.Get("AboutLn", b);
        public static StringGetter AboutLnT = (b) => StringResources.Get("AboutLnT", b);

        public static StringGetter AboutLog = (b) => StringResources.Get("AboutLog", b);
        public static StringGetter AboutLogT = (b) => StringResources.Get("AboutLogT", b);
        public static StringGetter LogBase = (b) => StringResources.Get("LogBase", b);

        public static StringGetter AboutExp = (b) => StringResources.Get("AboutExp", b);
        public static StringGetter AboutExpT = (b) => StringResources.Get("AboutExpT", b);

        public static StringGetter AboutRadians = (b) => StringResources.Get("AboutRadians", b);
        public static StringGetter AboutRadiansT = (b) => StringResources.Get("AboutRadiansT", b);
        public static StringGetter AboutDegrees = (b) => StringResources.Get("AboutDegrees", b);
        public static StringGetter AboutDegreesT = (b) => StringResources.Get("AboutDegreesT", b);

        public static StringGetter AboutAtan2 = (b) => StringResources.Get("AboutAtan2", b);
        public static StringGetter AboutAtan2Arg1 = (b) => StringResources.Get("AboutAtan2Arg1", b);
        public static StringGetter AboutAtan2Arg2 = (b) => StringResources.Get("AboutAtan2Arg2", b);

        public static StringGetter AboutPi = (b) => StringResources.Get("AboutPi", b);

        public static StringGetter AboutDate = (b) => StringResources.Get("AboutDate", b);
        public static StringGetter DateArg1 = (b) => StringResources.Get("DateArg1", b);
        public static StringGetter DateArg2 = (b) => StringResources.Get("DateArg2", b);
        public static StringGetter DateArg3 = (b) => StringResources.Get("DateArg3", b);
        public static StringGetter AboutTime = (b) => StringResources.Get("AboutTime", b);
        public static StringGetter TimeArg1 = (b) => StringResources.Get("TimeArg1", b);
        public static StringGetter TimeArg2 = (b) => StringResources.Get("TimeArg2", b);
        public static StringGetter TimeArg3 = (b) => StringResources.Get("TimeArg3", b);
        public static StringGetter TimeArg4 = (b) => StringResources.Get("TimeArg4", b);
        public static StringGetter AboutYear = (b) => StringResources.Get("AboutYear", b);
        public static StringGetter YearArg1 = (b) => StringResources.Get("YearArg1", b);
        public static StringGetter AboutMonth = (b) => StringResources.Get("AboutMonth", b);
        public static StringGetter MonthArg1 = (b) => StringResources.Get("MonthArg1", b);
        public static StringGetter AboutDay = (b) => StringResources.Get("AboutDay", b);
        public static StringGetter DayArg1 = (b) => StringResources.Get("DayArg1", b);
        public static StringGetter AboutHour = (b) => StringResources.Get("AboutHour", b);
        public static StringGetter HourArg1 = (b) => StringResources.Get("HourArg1", b);
        public static StringGetter AboutMinute = (b) => StringResources.Get("AboutMinute", b);
        public static StringGetter MinuteArg1 = (b) => StringResources.Get("MinuteArg1", b);
        public static StringGetter AboutSecond = (b) => StringResources.Get("AboutSecond", b);
        public static StringGetter SecondArg1 = (b) => StringResources.Get("SecondArg1", b);
        public static StringGetter AboutWeekday = (b) => StringResources.Get("AboutWeekday", b);
        public static StringGetter WeekdayArg1 = (b) => StringResources.Get("WeekdayArg1", b);
        public static StringGetter WeekdayArg2 = (b) => StringResources.Get("WeekdayArg2", b);
        public static StringGetter AboutWeekNum = (b) => StringResources.Get("AboutWeekNum", b);
        public static StringGetter WeekNumArg1 = (b) => StringResources.Get("WeekNumArg1", b);
        public static StringGetter WeekNumArg2 = (b) => StringResources.Get("WeekNumArg2", b);
        public static StringGetter AboutISOWeekNum = (b) => StringResources.Get("AboutISOWeekNum", b);
        public static StringGetter ISOWeekNumArg1 = (b) => StringResources.Get("ISOWeekNumArg1", b);

        public static StringGetter AboutCalendar__MonthsLong = (b) => StringResources.Get("AboutCalendar__MonthsLong", b);
        public static StringGetter AboutCalendar__MonthsShort = (b) => StringResources.Get("AboutCalendar__MonthsShort", b);
        public static StringGetter AboutCalendar__WeekdaysLong = (b) => StringResources.Get("AboutCalendar__WeekdaysLong", b);
        public static StringGetter AboutCalendar__WeekdaysShort = (b) => StringResources.Get("AboutCalendar__WeekdaysShort", b);

        public static StringGetter AboutClock__AmPm = (b) => StringResources.Get("AboutClock__AmPm", b);
        public static StringGetter AboutClock__AmPmShort = (b) => StringResources.Get("AboutClock__AmPmShort", b);
        public static StringGetter AboutClock__IsClock24 = (b) => StringResources.Get("AboutClock__IsClock24", b);

        public static StringGetter AboutDateTime = (b) => StringResources.Get("AboutDateTime", b);
        public static StringGetter AboutDateValue = (b) => StringResources.Get("AboutDateValue", b);
        public static StringGetter DateValueArg1 = (b) => StringResources.Get("DateValueArg1", b);
        public static StringGetter DateValueArg2 = (b) => StringResources.Get("DateValueArg2", b);
        public static StringGetter AboutTimeValue = (b) => StringResources.Get("AboutTimeValue", b);
        public static StringGetter TimeValueArg1 = (b) => StringResources.Get("TimeValueArg1", b);
        public static StringGetter TimeValueArg2 = (b) => StringResources.Get("TimeValueArg2", b);
        public static StringGetter SupportedDateTimeLanguageCodes = (b) => StringResources.Get("SupportedDateTimeLanguageCodes", b);
        public static StringGetter AboutDateTimeValue = (b) => StringResources.Get("AboutDateTimeValue", b);
        public static StringGetter DateTimeValueArg1 = (b) => StringResources.Get("DateTimeValueArg1", b);
        public static StringGetter DateTimeValueArg2 = (b) => StringResources.Get("DateTimeValueArg2", b);

        public static StringGetter AboutDateAdd = (b) => StringResources.Get("AboutDateAdd", b);
        public static StringGetter DateAddArg1 = (b) => StringResources.Get("DateAddArg1", b);
        public static StringGetter DateAddArg2 = (b) => StringResources.Get("DateAddArg2", b);
        public static StringGetter DateAddArg3 = (b) => StringResources.Get("DateAddArg3", b);

        public static StringGetter AboutDateAddT = (b) => StringResources.Get("AboutDateAddT", b);
        public static StringGetter DateAddTArg1 = (b) => StringResources.Get("DateAddTArg1", b);
        public static StringGetter DateAddTArg2 = (b) => StringResources.Get("DateAddTArg2", b);
        public static StringGetter DateAddTArg3 = (b) => StringResources.Get("DateAddTArg3", b);

        public static StringGetter AboutDateDiff = (b) => StringResources.Get("AboutDateDiff", b);
        public static StringGetter DateDiffArg1 = (b) => StringResources.Get("DateDiffArg1", b);
        public static StringGetter DateDiffArg2 = (b) => StringResources.Get("DateDiffArg2", b);
        public static StringGetter DateDiffArg3 = (b) => StringResources.Get("DateDiffArg3", b);

        public static StringGetter AboutDateDiffT = (b) => StringResources.Get("AboutDateDiffT", b);
        public static StringGetter DateDiffTArg1 = (b) => StringResources.Get("DateDiffTArg1", b);
        public static StringGetter DateDiffTArg2 = (b) => StringResources.Get("DateDiffTArg2", b);
        public static StringGetter DateDiffTArg3 = (b) => StringResources.Get("DateDiffTArg3", b);

        public static StringGetter AboutFind = (b) => StringResources.Get("AboutFind", b);
        public static StringGetter FindArg1 = (b) => StringResources.Get("FindArg1", b);
        public static StringGetter FindArg2 = (b) => StringResources.Get("FindArg2", b);
        public static StringGetter FindArg3 = (b) => StringResources.Get("FindArg3", b);
        public static StringGetter AboutFindT = (b) => StringResources.Get("AboutFindT", b);
        public static StringGetter FindTArg1 = (b) => StringResources.Get("FindTArg1", b);
        public static StringGetter FindTArg2 = (b) => StringResources.Get("FindTArg2", b);
        public static StringGetter FindTArg3 = (b) => StringResources.Get("FindTArg3", b);

        public static StringGetter AboutColorValue = (b) => StringResources.Get("AboutColorValue", b);
        public static StringGetter ColorValueArg1 = (b) => StringResources.Get("ColorValueArg1", b);

        public static StringGetter AboutTable = (b) => StringResources.Get("AboutTable", b);
        public static StringGetter TableArg1 = (b) => StringResources.Get("TableArg1", b);

        public static StringGetter AboutMod = (b) => StringResources.Get("AboutMod", b);
        public static StringGetter ModFuncArg1 = (b) => StringResources.Get("ModFuncArg1", b);
        public static StringGetter ModFuncArg2 = (b) => StringResources.Get("ModFuncArg2", b);

        public static StringGetter AboutModT = (b) => StringResources.Get("AboutModT", b);
        public static StringGetter ModTFuncArg1 = (b) => StringResources.Get("ModTFuncArg1", b);
        public static StringGetter ModTFuncArg2 = (b) => StringResources.Get("ModTFuncArg2", b);

        public static StringGetter AboutForAll = (b) => StringResources.Get("AboutForAll", b);
        public static StringGetter ForAllArg1 = (b) => StringResources.Get("ForAllArg1", b);
        public static StringGetter ForAllArg2 = (b) => StringResources.Get("ForAllArg2", b);

        public static StringGetter AboutPower = (b) => StringResources.Get("AboutPower", b);
        public static StringGetter PowerFuncArg1 = (b) => StringResources.Get("PowerFuncArg1", b);
        public static StringGetter PowerFuncArg2 = (b) => StringResources.Get("PowerFuncArg2", b);
        public static StringGetter AboutPowerT = (b) => StringResources.Get("AboutPowerT", b);
        public static StringGetter PowerTFuncArg1 = (b) => StringResources.Get("PowerTFuncArg1", b);
        public static StringGetter PowerTFuncArg2 = (b) => StringResources.Get("PowerTFuncArg2", b);

        public static StringGetter AboutStartsWith = (b) => StringResources.Get("AboutStartsWith", b);
        public static StringGetter StartsWithArg1 = (b) => StringResources.Get("StartsWithArg1", b);
        public static StringGetter StartsWithArg2 = (b) => StringResources.Get("StartsWithArg2", b);

        public static StringGetter AboutEndsWith = (b) => StringResources.Get("AboutEndsWith", b);
        public static StringGetter EndsWithArg1 = (b) => StringResources.Get("EndsWithArg1", b);
        public static StringGetter EndsWithArg2 = (b) => StringResources.Get("EndsWithArg2", b);

        public static StringGetter AboutBlank = (b) => StringResources.Get("AboutBlank", b);

        public static StringGetter AboutSplit = (b) => StringResources.Get("AboutSplit", b);
        public static StringGetter SplitArg1 = (b) => StringResources.Get("SplitArg1", b);
        public static StringGetter SplitArg2 = (b) => StringResources.Get("SplitArg2", b);

        public static StringGetter AboutIsType = (b) => StringResources.Get("AboutIsType", b);
        public static StringGetter IsTypeArg1 = (b) => StringResources.Get("IsTypeArg1", b);
        public static StringGetter IsTypeArg2 = (b) => StringResources.Get("IsTypeArg2", b);

        public static StringGetter AboutAsType = (b) => StringResources.Get("AboutAsType", b);
        public static StringGetter AsTypeArg1 = (b) => StringResources.Get("AsTypeArg1", b);
        public static StringGetter AsTypeArg2 = (b) => StringResources.Get("AsTypeArg2", b);

        public static StringGetter AboutWith = (b) => StringResources.Get("AboutWith", b);
        public static StringGetter WithArg1 = (b) => StringResources.Get("WithArg1", b);
        public static StringGetter WithArg2 = (b) => StringResources.Get("WithArg2", b);

        public static StringGetter AboutSequence = (b) => StringResources.Get("AboutSequence", b);
        public static StringGetter SequenceArg1 = (b) => StringResources.Get("SequenceArg1", b);
        public static StringGetter SequenceArg2 = (b) => StringResources.Get("SequenceArg2", b);
        public static StringGetter SequenceArg3 = (b) => StringResources.Get("SequenceArg3", b);

        public static StringGetter AboutParseJSON = (b) => StringResources.Get("AboutParseJSON", b);
        public static StringGetter ParseJSONArg1 = (b) => StringResources.Get("ParseJSONArg1", b);

        public static StringGetter AboutIndex = (b) => StringResources.Get("AboutIndex", b);
        public static StringGetter IndexArg1 = (b) => StringResources.Get("IndexArg1", b);
        public static StringGetter IndexArg2 = (b) => StringResources.Get("IndexArg2", b);

        public static StringGetter AboutPatch = (b) => StringResources.Get("AboutPatch", b);
        public static StringGetter PatchDataSourceArg = (b) => StringResources.Get("PatchDataSourceArg", b);
        public static StringGetter PatchBaseRecordArg = (b) => StringResources.Get("PatchBaseRecordArg", b);
        public static StringGetter PatchChangeRecordsArg = (b) => StringResources.Get("PatchChangeRecordsArg", b);

        public static StringGetter AboutCollect = (b) => StringResources.Get("AboutCollect", b);
        public static StringGetter CollectDataSourceArg = (b) => StringResources.Get("CollectDataSourceArg", b);
        public static StringGetter CollectRecordArg = (b) => StringResources.Get("CollectRecordArg", b);

        public static StringGetter AboutClearCollect = (b) => StringResources.Get("AboutClearCollect", b);
        public static StringGetter ClearCollectDataSourceArg = (b) => StringResources.Get("ClearCollectDataSourceArg", b);
        public static StringGetter ClearCollectRecordArg = (b) => StringResources.Get("ClearCollectRecordArg", b);

        public static StringGetter AboutClear = (b) => StringResources.Get("AboutClear", b);
        public static StringGetter ClearCollectionArg = (b) => StringResources.Get("ClearCollectionArg", b);

        public static StringGetter AboutRemove = (b) => StringResources.Get("AboutRemove", b);
        public static StringGetter RemoveDataSourceArg = (b) => StringResources.Get("RemoveDataSourceArg", b);
        public static StringGetter RemoveRecordsArg = (b) => StringResources.Get("RemoveRecordsArg", b);

        public static StringGetter AboutDec2Hex = (b) => StringResources.Get("AboutDec2Hex", b);
        public static StringGetter Dec2HexArg1 = (b) => StringResources.Get("Dec2HexArg1", b);
        public static StringGetter Dec2HexArg2 = (b) => StringResources.Get("Dec2HexArg2", b);

        public static StringGetter AboutDec2HexT = (b) => StringResources.Get("AboutDec2HexT", b);
        public static StringGetter Dec2HexTArg1 = (b) => StringResources.Get("Dec2HexTArg1", b);
        public static StringGetter Dec2HexTArg2 = (b) => StringResources.Get("Dec2HexTArg2", b);

        public static StringGetter AboutHex2Dec = (b) => StringResources.Get("AboutHex2Dec", b);
        public static StringGetter Hex2DecArg1 = (b) => StringResources.Get("Hex2DecArg1", b);

        public static StringGetter AboutHex2DecT = (b) => StringResources.Get("AboutHex2DecT", b);
        public static StringGetter Hex2DecTArg1 = (b) => StringResources.Get("Hex2DecTArg1", b);

        public static StringGetter AboutOptionSetInfo = (b) => StringResources.Get("AboutOptionSetInfo", b);
        public static StringGetter AboutOptionSetInfoArg1 = (b) => StringResources.Get("AboutOptionSetInfoArg1", b);
        
        // Previously, errors were listed here in the form of a StringGetter, which would be evaluated to fetch
        // an error message to pass to the BaseError class constructor. We are switching to passing the message key itself
        // to the BaseError class, and the BaseError itself is responsible for fetching the resource. (This allows the
        // BaseError class to contain logic to fetch auxillary resources, such as HowToFix and WhyToFix messages.)
        //
        // Any new additions here should be of type ErrorResourceKey and contain the value of the resource key.
        public static ErrorResourceKey ErrUnSupportedComponentBehaviorInvocation = new ErrorResourceKey("ErrUnSupportedComponentBehaviorInvocation");
        public static ErrorResourceKey ErrUnSupportedComponentDataPropertyAccess = new ErrorResourceKey("ErrUnSupportedComponentDataPropertyAccess");
        public static ErrorResourceKey ErrOperandExpected = new ErrorResourceKey("ErrOperandExpected");
        public static ErrorResourceKey ErrBadToken = new ErrorResourceKey("ErrBadToken");
        public static ErrorResourceKey UnexpectedCharacterToken = new ErrorResourceKey("UnexpectedCharacterToken");
        public static ErrorResourceKey ErrMissingEndOfBlockComment = new ErrorResourceKey("ErrMissingEndOfBlockComment");
        public static ErrorResourceKey ErrExpectedFound_Ex_Fnd = new ErrorResourceKey("ErrExpectedFound_Ex_Fnd");
        public static ErrorResourceKey ErrInvalidName = new ErrorResourceKey("ErrInvalidName");
        public static ErrorResourceKey ErrInvalidIdentifier = new ErrorResourceKey("ErrInvalidIdentifier");
        public static ErrorResourceKey ErrInvalidPropertyAccess = new ErrorResourceKey("ErrInvalidPropertyAccess");
        public static ErrorResourceKey ErrInvalidPropertyReference = new ErrorResourceKey("ErrInvalidPropertyReference");
        public static ErrorResourceKey ErrInvalidParentUse = new ErrorResourceKey("ErrInvalidParentUse");
        public static ErrorResourceKey ErrTooManyUps = new ErrorResourceKey("ErrTooManyUps");
        public static ErrorResourceKey ErrRuleNestedTooDeeply = new ErrorResourceKey("ErrRuleNestedTooDeeply");
        public static ErrorResourceKey ErrInvalidDot = new ErrorResourceKey("ErrInvalidDot");
        public static ErrorResourceKey ErrUnknownFunction = new ErrorResourceKey("ErrUnknownFunction");
        public static ErrorResourceKey ErrUnknownNamespaceFunction = new ErrorResourceKey("ErrUnknownNamespaceFunction");
        public static ErrorResourceKey ErrBadArity = new ErrorResourceKey("ErrBadArity");
        public static ErrorResourceKey ErrBadArityRange = new ErrorResourceKey("ErrBadArityRange");
        public static ErrorResourceKey ErrBadArityMinimum = new ErrorResourceKey("ErrBadArityMinimum");
        public static ErrorResourceKey ErrBadArityOdd = new ErrorResourceKey("ErrBadArityOdd");
        public static ErrorResourceKey ErrBadArityEven = new ErrorResourceKey("ErrBadArityEven");
        public static ErrorResourceKey ErrBadType = new ErrorResourceKey("ErrBadType");
        public static ErrorResourceKey ErrBadType_Type = new ErrorResourceKey("ErrBadType_Type");
        public static ErrorResourceKey ErrBadOperatorTypes = new ErrorResourceKey("ErrBadOperatorTypes");
        public static ErrorResourceKey ErrGuidStrictComparison = new ErrorResourceKey("ErrGuidStrictComparison");
        public static ErrorResourceKey ErrBadType_ExpectedType = new ErrorResourceKey("ErrBadType_ExpectedType");
        public static ErrorResourceKey ErrBadType_ExpectedTypesCSV = new ErrorResourceKey("ErrBadType_ExpectedTypesCSV");
        public static ErrorResourceKey ErrBadType_ExpectedType_ProvidedType = new ErrorResourceKey("ErrBadType_ExpectedType_ProvidedType");
        public static ErrorResourceKey ErrBadSchema_ExpectedType = new ErrorResourceKey("ErrBadSchema_ExpectedType");
        public static ErrorResourceKey ErrInvalidArgs_Func = new ErrorResourceKey("ErrInvalidArgs_Func");
        public static ErrorResourceKey ErrNeedTable_Func = new ErrorResourceKey("ErrNeedTable_Func");
        public static ErrorResourceKey ErrNeedTableCol_Func = new ErrorResourceKey("ErrNeedTableCol_Func");
        public static ErrorResourceKey ErrNotAccessibleInCurrentContext = new ErrorResourceKey("ErrNotAccessibleInCurrentContext");
        public static ErrorResourceKey ErrInternalControlInInputProperty = new ErrorResourceKey("ErrInternalControlInInputProperty");
        public static ErrorResourceKey ErrColumnNotAccessibleInCurrentContext = new ErrorResourceKey("ErrColumnNotAccessibleInCurrentContext");
        public static ErrorResourceKey WrnRowScopeOneToNExpandNumberOfCalls = new ErrorResourceKey("WrnRowScopeOneToNExpandNumberOfCalls");
        public static ErrorResourceKey ErrInvalidSchemaNeedStringCol_Col = new ErrorResourceKey("ErrInvalidSchemaNeedStringCol_Col");
        public static ErrorResourceKey ErrInvalidSchemaNeedNumCol_Col = new ErrorResourceKey("ErrInvalidSchemaNeedNumCol_Col");
        public static ErrorResourceKey ErrInvalidSchemaNeedDecCol_Col = new ErrorResourceKey("ErrInvalidSchemaNeedDecCol_Col");
        public static ErrorResourceKey ErrInvalidSchemaNeedDateCol_Col = new ErrorResourceKey("ErrInvalidSchemaNeedDateCol_Col");
        public static ErrorResourceKey ErrInvalidSchemaNeedColorCol_Col = new ErrorResourceKey("ErrInvalidSchemaNeedColorCol_Col");
        public static ErrorResourceKey ErrInvalidSchemaNeedCol = new ErrorResourceKey("ErrInvalidSchemaNeedCol");
        public static ErrorResourceKey ErrNeedRecord = new ErrorResourceKey("ErrNeedRecord");
        public static ErrorResourceKey ErrAutoRefreshNotAllowed = new ErrorResourceKey("ErrAutoRefreshNotAllowed");
        public static ErrorResourceKey ErrIncompatibleRecord = new ErrorResourceKey("ErrIncompatibleRecord");
        public static ErrorResourceKey ErrNeedRecord_Func = new ErrorResourceKey("ErrNeedRecord_Func");
        public static ErrorResourceKey ErrNeedEntity_EntityName = new ErrorResourceKey("ErrNeedEntity_EntityName");
        public static ErrorResourceKey ErrNeedValidVariableName_Arg = new ErrorResourceKey("ErrNeedValidVariableName_Arg");
        public static ErrorResourceKey ErrOperatorExpected = new ErrorResourceKey("ErrOperatorExpected");
        public static ErrorResourceKey ErrNumberExpected = new ErrorResourceKey("ErrNumberExpected");
        public static ErrorResourceKey ErrNumberTooLarge = new ErrorResourceKey("ErrNumberTooLarge");
        public static ErrorResourceKey ErrTextTooLarge = new ErrorResourceKey("ErrTextTooLarge");
        public static ErrorResourceKey ErrTextFormatTooLarge = new ErrorResourceKey("ErrTextFormatTooLarge");
        public static ErrorResourceKey ErrTextInvalidFormat = new ErrorResourceKey("ErrTextInvalidFormat");
        public static ErrorResourceKey ErrTextInvalidArgDateTime = new ErrorResourceKey("ErrTextInvalidArgDateTime");
        public static ErrorResourceKey ErrBooleanExpected = new ErrorResourceKey("ErrBooleanExpected");
        public static ErrorResourceKey ErrOnlyOneViewExpected = new ErrorResourceKey("ErrOnlyOneViewExpected");
        public static ErrorResourceKey ErrViewFromCurrentTableExpected = new ErrorResourceKey("ErrViewFromCurrentTableExpected");
        public static ErrorResourceKey ErrColonExpected = new ErrorResourceKey("ErrColonExpected");
        public static ErrorResourceKey ErrInvalidDataSource = new ErrorResourceKey("ErrInvalidDataSource");
        public static ErrorResourceKey ErrExpectedDataSourceRestriction = new ErrorResourceKey("ErrExpectedDataSourceRestriction");
        public static ErrorResourceKey ErrBehaviorPropertyExpected = new ErrorResourceKey("ErrBehaviorPropertyExpected");
        public static ErrorResourceKey ErrTestPropertyExpected = new ErrorResourceKey("ErrTestPropertyExpected");
        public static ErrorResourceKey ErrStringExpected = new ErrorResourceKey("ErrStringExpected");
        public static ErrorResourceKey ErrDateExpected = new ErrorResourceKey("ErrDateExpected");
        public static ErrorResourceKey ErrCannotCoerce_SourceType_TargetType = new ErrorResourceKey("ErrCannotCoerce_SourceType_TargetType");
        public static ErrorResourceKey ErrNumberOrStringExpected = new ErrorResourceKey("ErrNumberOrStringExpected");
        public static ErrorResourceKey ErrClosingBracketExpected = new ErrorResourceKey("ErrClosingBracketExpected");
        public static ErrorResourceKey ErrEmptyInvalidIdentifier = new ErrorResourceKey("ErrEmptyInvalidIdentifier");
        public static ErrorResourceKey ErrIncompatibleTypes = new ErrorResourceKey("ErrIncompatibleTypes");
        public static ErrorResourceKey ErrIncompatibleTypesForEquality_Left_Right = new ErrorResourceKey("ErrIncompatibleTypesForEquality_Left_Right");
        public static ErrorResourceKey ErrServiceFunctionUnknownOptionalParam_Name = new ErrorResourceKey("ErrServiceFunctionUnknownOptionalParam_Name");
        public static ErrorResourceKey ErrColumnTypeMismatch_ColName_ExpectedType_ActualType = new ErrorResourceKey("ErrColumnTypeMismatch_ColName_ExpectedType_ActualType");
        public static ErrorResourceKey ErrColumnMissing_ColName_ExpectedType = new ErrorResourceKey("ErrColumnMissing_ColName_ExpectedType");
        public static ErrorResourceKey ErrTableDoesNotAcceptThisType = new ErrorResourceKey("ErrTableDoesNotAcceptThisType");
        public static ErrorResourceKey ErrRecordDoesNotAcceptThisType = new ErrorResourceKey("ErrRecordDoesNotAcceptThisType");
        public static ErrorResourceKey ErrTypeError = new ErrorResourceKey("ErrTypeError");
        public static ErrorResourceKey ErrTypeError_Ex1_Ex2_Found = new ErrorResourceKey("ErrTypeError_Ex1_Ex2_Found");
        public static ErrorResourceKey ErrTypeError_Arg_Expected_Found = new ErrorResourceKey("ErrTypeError_Arg_Expected_Found");
        public static ErrorResourceKey ErrTypeError_WrongType = new ErrorResourceKey("ErrTypeError_WrongType");
        public static ErrorResourceKey ErrTypeErrorRecordIncompatibleWithSource = new ErrorResourceKey("ErrTypeErrorRecordIncompatibleWithSource");
        public static ErrorResourceKey ErrExpectedStringLiteralArg_Name = new ErrorResourceKey("ErrExpectedStringLiteralArg_Name");
        public static ErrorResourceKey ErrExpectedIdentifierArg_Name = new ErrorResourceKey("ErrExpectedIdentifierArg_Name");
        public static ErrorResourceKey ErrArgNotAValidIdentifier_Name = new ErrorResourceKey("ErrArgNotAValidIdentifier_Name");
        public static ErrorResourceKey ErrColExists_Name = new ErrorResourceKey("ErrColExists_Name");
        public static ErrorResourceKey ErrColConflict_Name = new ErrorResourceKey("ErrColConflict_Name");
        public static ErrorResourceKey ErrColDNE_Name = new ErrorResourceKey("ErrColDNE_Name");
        public static ErrorResourceKey ErrColumnDoesNotExist_Name_Similar = new ErrorResourceKey("ErrColumnDoesNotExist_Name_Similar");
        public static ErrorResourceKey ErrSortIncorrectOrder = new ErrorResourceKey("ErrSortIncorrectOrder");
        public static ErrorResourceKey ErrSortWrongType = new ErrorResourceKey("ErrSortWrongType");
        public static ErrorResourceKey ErrDistinctWrongType = new ErrorResourceKey("ErrDistinctWrongType");
        public static ErrorResourceKey ErrFunctionDoesNotAcceptThisType_Function_Expected = new ErrorResourceKey("ErrFunctionDoesNotAcceptThisType_Function_Expected");
        public static ErrorResourceKey ErrIncorrectFormat_Func = new ErrorResourceKey("ErrIncorrectFormat_Func");
        public static ErrorResourceKey ErrAsyncLambda = new ErrorResourceKey("ErrAsyncLambda");
        public static ErrorResourceKey ErrMultipleValuesForField_Name = new ErrorResourceKey("ErrMultipleValuesForField_Name");
        public static ErrorResourceKey ErrScopeModificationLambda = new ErrorResourceKey("ErrScopeModificationLambda");
        public static ErrorResourceKey ErrFunctionDisallowedWithinNondeterministicOperationOrder = new ErrorResourceKey("ErrFunctionDisallowedWithinNondeterministicOperationOrder");
        public static ErrorResourceKey ErrBadRecordFieldType_FieldName_ExpectedType = new ErrorResourceKey("ErrBadRecordFieldType_FieldName_ExpectedType");
        public static ErrorResourceKey ErrAsTypeAndIsTypeExpectConnectedDataSource = new ErrorResourceKey("ErrAsTypeAndIsTypeExpectConnectedDataSource");
        public static ErrorResourceKey ErrInvalidControlReference = new ErrorResourceKey("ErrInvalidControlReference");
        public static ErrorResourceKey ErrInvalidStringInterpolation = new ErrorResourceKey("ErrInvalidStringInterpolation");
        public static ErrorResourceKey ErrEmptyIsland = new ErrorResourceKey("ErrEmptyIsland");
        public static ErrorResourceKey ErrDeprecated = new ErrorResourceKey("ErrDeprecated");
        public static ErrorResourceKey ErrInvalidFunction = new ErrorResourceKey("ErrInvalidFunction");
        public static ErrorResourceKey ErrUntypedObjectScope = new ErrorResourceKey("ErrUntypedObjectScope");

        public static ErrorResourceKey ErrErrorIrrelevantField = new ErrorResourceKey("ErrErrorIrrelevantField");
        public static ErrorResourceKey ErrAsNotInContext = new ErrorResourceKey("ErrAsNotInContext");
        public static ErrorResourceKey ErrValueMustBeFullyQualified = new ErrorResourceKey("ErrValueMustBeFullyQualified");
        public static ErrorResourceKey WarnColumnNameSpecifiedMultipleTimes_Name = new ErrorResourceKey("WarnColumnNameSpecifiedMultipleTimes_Name");
        public static ErrorResourceKey WarnLiteralPredicate = new ErrorResourceKey("WarnLiteralPredicate");
        public static ErrorResourceKey WarnDynamicMetadata = new ErrorResourceKey("WarnDynamicMetadata");
        public static ErrorResourceKey WarnDeferredType = new ErrorResourceKey("WarnDeferredType");

        public static StringGetter InfoMessage = (b) => StringResources.Get("InfoMessage", b);
        public static StringGetter InfoNode_Node = (b) => StringResources.Get("InfoNode_Node", b);
        public static StringGetter InfoTok_Tok = (b) => StringResources.Get("InfoTok_Tok", b);
        public static StringGetter FormatSpan_Min_Lim = (b) => StringResources.Get("FormatSpan_Min_Lim", b);
        public static StringGetter FormatErrorSeparator = (b) => StringResources.Get("FormatErrorSeparator", b);

        public static ErrorResourceKey SuggestRemoteExecutionHint = new ErrorResourceKey("SuggestRemoteExecutionHint");
        public static ErrorResourceKey SuggestRemoteExecutionHint_InOpRhs = new ErrorResourceKey("SuggestRemoteExecutionHint_InOpRhs");
        public static ErrorResourceKey SuggestRemoteExecutionHint_StringMatchSecondParam = new ErrorResourceKey("SuggestRemoteExecutionHint_StringMatchSecondParam");
        public static ErrorResourceKey SuggestRemoteExecutionHint_InOpInvalidColumn = new ErrorResourceKey("SuggestRemoteExecutionHint_InOpInvalidColumn");
        public static ErrorResourceKey OpNotSupportedByColumnSuggestionMessage_OpNotSupportedByColumn = new ErrorResourceKey("SuggestRemoteExecutionHint_OpNotSupportedByColumn");
        public static ErrorResourceKey OpNotSupportedByServiceSuggestionMessage_OpNotSupportedByService = new ErrorResourceKey("SuggestRemoteExecutionHint_OpNotSupportedByService");
        public static ErrorResourceKey OpNotSupportedByClientSuggestionMessage_OpNotSupportedByClient = new ErrorResourceKey("SuggestRemoteExecutionHint_OpNotSupportedByClient");

        public static ErrorResourceKey ErrNamedFormula_MissingSemicolon = new ErrorResourceKey("ErrNamedFormula_MissingSemicolon");
        public static ErrorResourceKey ErrNamedFormula_MissingValue = new ErrorResourceKey("ErrNamedFormula_MissingValue");
        public static ErrorResourceKey ErrNamedFormula_AlreadyDefined = new ErrorResourceKey("ErrNamedFormula_AlreadyDefined");
        public static ErrorResourceKey ErrorResource_NameConflict = new ErrorResourceKey("ErrorResource_NameConflict");

        // ErrorResourceKey for creating an error from an arbitrary string message. The key resolves to "{0}", meaning
        // that a single string arg can be supplied representing the entire text of the error.
        public static ErrorResourceKey ErrGeneralError = new ErrorResourceKey("ErrGeneralError");
        public static ErrorResourceKey ErrRemoveAllArg = new ErrorResourceKey("ErrRemoveAllArg");

        public static ErrorResourceKey BooleanOptionSetOptionNotSupported = new ErrorResourceKey("BooleanOptionSetOptionNotSupported");
    }
}
