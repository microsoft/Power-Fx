// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Date()
    // Equivalent DAX/Excel function: Date
    internal sealed class DateFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool HasPreciseErrors => true;

        public override bool SupportsParamCoercion => true;

        public DateFunction()
            : base("Date", TexlStrings.AboutDate, FunctionCategories.DateTime, DType.Date, 0, 3, 3, DType.Number, DType.Number, DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.DateArg1, TexlStrings.DateArg2, TexlStrings.DateArg3 };
        }
    }

    // Base for all extract date/time functions.
    internal abstract class ExtractDateTimeFunctionBase : BuiltinFunction
    {
        public override bool HasPreciseErrors => true;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public ExtractDateTimeFunctionBase(string name, TexlStrings.StringGetter description, FunctionCategories fc, DType returnType, BigInteger maskLambdas, int arityMin, int arityMax, params DType[] paramTypes)
            : base(name, description, fc, returnType, maskLambdas, arityMin, arityMax, paramTypes)
        {
            Contracts.Assert(arityMin == 1);
            Contracts.Assert(arityMax == 1);
            Contracts.Assert(paramTypes[0] == DType.DateTime);
        }

        public override bool IsRowScopedServerDelegatable(CallNode callNode, TexlBinding binding, OperationCapabilityMetadata metadata)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);
            Contracts.AssertValue(metadata);

            return base.IsRowScopedServerDelegatable(callNode, binding, metadata);
        }
    }

    // Time()
    // Equivalent DAX/Excel function: Time
    internal sealed class TimeFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool HasPreciseErrors => true;

        public override bool SupportsParamCoercion => true;

        public TimeFunction()
            : base("Time", TexlStrings.AboutTime, FunctionCategories.DateTime, DType.Time, 0, 3, 4, DType.Number, DType.Number, DType.Number, DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.TimeArg1, TexlStrings.TimeArg2, TexlStrings.TimeArg3 };
            yield return new[] { TexlStrings.TimeArg1, TexlStrings.TimeArg2, TexlStrings.TimeArg3, TexlStrings.TimeArg4 };
        }
    }

    // DateTime(year, month, day, hour, minute, second[, millisecond])
    internal sealed class DateTimeFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool HasPreciseErrors => true;

        public override bool SupportsParamCoercion => true;

        public DateTimeFunction()
            : base("DateTime", TexlStrings.AboutDateTime, FunctionCategories.DateTime, DType.DateTime, 0, 6, 7, DType.Number, DType.Number, DType.Number, DType.Number, DType.Number, DType.Number, DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.DateArg1, TexlStrings.DateArg2, TexlStrings.DateArg3, TexlStrings.TimeArg1, TexlStrings.TimeArg2, TexlStrings.TimeArg3 };
            yield return new[] { TexlStrings.DateArg1, TexlStrings.DateArg2, TexlStrings.DateArg3, TexlStrings.TimeArg1, TexlStrings.TimeArg2, TexlStrings.TimeArg3, TexlStrings.TimeArg4 };
        }
    }

    // Year()
    // Equivalent DAX/Excel function: Year
    internal sealed class YearFunction : ExtractDateTimeFunctionBase
    {
        public YearFunction()
            : base("Year", TexlStrings.AboutYear, FunctionCategories.DateTime, DType.Number, 0, 1, 1, DType.DateTime)
        {
        }

        public override DelegationCapability FunctionDelegationCapability => DelegationCapability.Year | DelegationCapability.Add;

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new TexlStrings.StringGetter[] { TexlStrings.YearArg1 };
        }
    }

    // Month()
    // Equivalent DAX/Excel function: Month
    internal sealed class MonthFunction : ExtractDateTimeFunctionBase
    {
        public MonthFunction()
            : base("Month", TexlStrings.AboutMonth, FunctionCategories.DateTime, DType.Number, 0, 1, 1, DType.DateTime)
        {
        }

        public override DelegationCapability FunctionDelegationCapability => DelegationCapability.Month | DelegationCapability.Add;

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.MonthArg1 };
        }
    }

    // Day()
    // Equivalent DAX/Excel function: Day
    internal sealed class DayFunction : ExtractDateTimeFunctionBase
    {
        public DayFunction()
            : base("Day", TexlStrings.AboutDay, FunctionCategories.DateTime, DType.Number, 0, 1, 1, DType.DateTime)
        {
        }

        public override DelegationCapability FunctionDelegationCapability => DelegationCapability.Day | DelegationCapability.Add;

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.DayArg1 };
        }
    }

    // Hour()
    // Equivalent DAX/Excel function: Hour
    internal sealed class HourFunction : ExtractDateTimeFunctionBase
    {
        public HourFunction()
            : base("Hour", TexlStrings.AboutHour, FunctionCategories.DateTime, DType.Number, 0, 1, 1, DType.DateTime)
        {
        }

        public override DelegationCapability FunctionDelegationCapability => DelegationCapability.Hour | DelegationCapability.Add;

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.HourArg1 };
        }
    }

    // Minute()
    // Equivalent DAX/Excel function: Minute
    internal sealed class MinuteFunction : ExtractDateTimeFunctionBase
    {
        public MinuteFunction()
            : base("Minute", TexlStrings.AboutMinute, FunctionCategories.DateTime, DType.Number, 0, 1, 1, DType.DateTime)
        {
        }

        public override DelegationCapability FunctionDelegationCapability => DelegationCapability.Minute | DelegationCapability.Add;

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.MinuteArg1 };
        }
    }

    // Second()
    // Equivalent DAX/Excel function: Second
    internal sealed class SecondFunction : ExtractDateTimeFunctionBase
    {
        public SecondFunction()
            : base("Second", TexlStrings.AboutSecond, FunctionCategories.DateTime, DType.Number, 0, 1, 1, DType.DateTime)
        {
        }

        public override DelegationCapability FunctionDelegationCapability => DelegationCapability.Second | DelegationCapability.Add;

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.SecondArg1 };
        }
    }

    // Weekday(date:d, [startOfWeek:n])
    // Equivalent DAX/Excel function: Weekday
    internal sealed class WeekdayFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool HasPreciseErrors => true;

        public override bool SupportsParamCoercion => true;

        public WeekdayFunction()
            : base("Weekday", TexlStrings.AboutWeekday, FunctionCategories.DateTime, DType.Number, 0, 1, 2, DType.DateTime, DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.WeekdayArg1 };
            yield return new[] { TexlStrings.WeekdayArg1, TexlStrings.WeekdayArg2 };
        }

        public override IEnumerable<string> GetRequiredEnumNames()
        {
            return new List<string>() { EnumConstants.StartOfWeekEnumString };
        }
    }

    // WeekNum(date:d, [startOfWeek:n])
    // Equivalent DAX/Excel function: WeekNum
    internal sealed class WeekNumFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool HasPreciseErrors => true;

        public override bool SupportsParamCoercion => true;

        public WeekNumFunction()
            : base("WeekNum", TexlStrings.AboutWeekNum, FunctionCategories.DateTime, DType.Number, 0, 1, 2, DType.DateTime, DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.WeekNumArg1 };
            yield return new[] { TexlStrings.WeekNumArg1, TexlStrings.WeekNumArg2 };
        }

        public override IEnumerable<string> GetRequiredEnumNames()
        {
            return new List<string>() { EnumConstants.StartOfWeekEnumString };
        }
    }

    // ISOWeekNum(date:d)
    // Return the week number for a given date using ISO semantics.
    internal sealed class ISOWeekNumFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public ISOWeekNumFunction()
            : base("ISOWeekNum", TexlStrings.AboutISOWeekNum, FunctionCategories.DateTime, DType.Number, 0, 1, 1, DType.DateTime)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.ISOWeekNumArg1 };
        }
    }

    internal abstract class DateTimeGenericFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool HasPreciseErrors => true;

        protected DateTimeGenericFunction(string name, TexlStrings.StringGetter description, DType returnType)
            : base(name, description, FunctionCategories.DateTime, returnType, 0, 1, 2, DType.String, DType.String)
        {
        }

        public override IEnumerable<string> GetRequiredEnumNames()
        {
            return new List<string>() { EnumConstants.DateTimeFormatEnumString };
        }

        public override bool HasSuggestionsForParam(int index)
        {
            return index == 1;
        }
    }

    // DateValue(date_text:s, [languageCode:s]) : D
    internal sealed class DateValueFunction : DateTimeGenericFunction
    {
        public const string DateValueInvariantFunctionName = "DateValue";

        public override bool HasPreciseErrors => true;

        public override bool SupportsParamCoercion => true;

        public DateValueFunction()
            : base(DateValueInvariantFunctionName, TexlStrings.AboutDateValue, DType.Date)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.DateValueArg1 };
            yield return new[] { TexlStrings.DateValueArg1, TexlStrings.DateValueArg2 };
        }
    }

    // TimeValue(time_text:s, [languageCode:s]) : T
    internal sealed class TimeValueFunction : DateTimeGenericFunction
    {
        public const string TimeValueFunctionInvariantName = "TimeValue";

        public override bool HasPreciseErrors => true;

        public override bool SupportsParamCoercion => true;

        public TimeValueFunction()
            : base(TimeValueFunctionInvariantName, TexlStrings.AboutTimeValue, DType.Time)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.TimeValueArg1 };
            yield return new[] { TexlStrings.TimeValueArg1, TexlStrings.TimeValueArg2 };
        }
    }

    // DateTimeValue(time_text:s, [languageCode:s]) : d
    internal sealed class DateTimeValueFunction : DateTimeGenericFunction
    {
        public const string DateTimeValueInvariantFunctionName = "DateTimeValue";

        public override bool HasPreciseErrors => true;

        public override bool SupportsParamCoercion => true;

        public DateTimeValueFunction()
            : base(DateTimeValueInvariantFunctionName, TexlStrings.AboutDateTimeValue, DType.DateTime)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.DateTimeValueArg1 };
            yield return new[] { TexlStrings.DateTimeValueArg1, TexlStrings.DateTimeValueArg2 };
        }
    }

    // DateAdd(timestamp: d, delta: n, [ unit: TimeUnits ]) : d
    internal sealed class DateAddFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        internal static readonly List<string> SubDayStringList = new List<string>() { "Hours", "Minutes", "Seconds", "Milliseconds" };

        public DateAddFunction()
            : base("DateAdd", TexlStrings.AboutDateAdd, FunctionCategories.DateTime, DType.DateTime, 0, 2, 3, DType.DateTime, DType.Number, DType.String)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.DateAddArg1, TexlStrings.DateAddArg2 };
            yield return new[] { TexlStrings.DateAddArg1, TexlStrings.DateAddArg2, TexlStrings.DateAddArg3 };
        }

        public override IEnumerable<string> GetRequiredEnumNames()
        {
            return new List<string>() { EnumConstants.TimeUnitEnumString };
        }

        // This method returns true if there are special suggestions for a particular parameter of the function.
        public override bool HasSuggestionsForParam(int argumentIndex)
        {
            Contracts.Assert(argumentIndex >= 0);

            return argumentIndex == 2;
        }

        public override bool CheckTypes(TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = CheckTypes(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType == DType.DateTime);

            var type0 = argTypes[0];

            if (fValid)
            {
                // Arg0 should be either a DateTime or Date.
                if (type0.Kind == DKind.Date || type0.Kind == DKind.DateTime)
                {
                    returnType = ReturnType;
                }
                else
                {
                    fValid = false;
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrDateExpected);
                    returnType = ReturnType;
                }
            }

            return fValid;
        }
    }

    // DateAdd(timestamp:d|*[d], delta:n|*[n], [unit:TimeUnits])
    internal sealed class DateAddTFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public DateAddTFunction()
            : base("DateAdd", TexlStrings.AboutDateAddT, FunctionCategories.Table, DType.EmptyTable, 0, 2, 3)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.DateAddTArg1, TexlStrings.DateAddTArg2 };
            yield return new[] { TexlStrings.DateAddTArg1, TexlStrings.DateAddTArg2, TexlStrings.DateAddTArg3 };
        }

        public override IEnumerable<string> GetRequiredEnumNames()
        {
            return new List<string>() { EnumConstants.TimeUnitEnumString };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "_T");
        }

        public override bool CheckTypes(BindingConfig config, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = CheckTypes(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            var type0 = argTypes[0];
            var type1 = argTypes[1];

            // Arg0 should be either a date/dateTime or a column of dates/dateTimes.
            // Its type dictates the function return type.
            if (type0.IsTable)
            {
                // Ensure we have a one-column table of dates/dateTimes. Since dateTime is the supertype, checking
                // for DateTime alone is sufficient.
                fValid &= CheckDateColumnType(type0, args[0], errors, ref nodeToCoercedTypeMap);

                if (fValid)
                {
                    var resultColumnType = config.Features.HasFlag(Features.ConsistentOneColumnTableResult)
                        ? ColumnName_Value
                        : type0.GetNames(DPath.Root).Single().Name;
                    returnType = DType.CreateTable(new TypedName(DType.DateTime, resultColumnType));
                }
            }
            else
            {
                if (type0.Kind == DKind.DateTime || type0.Kind == DKind.Date)
                {
                    returnType = DType.CreateTable(new TypedName(DType.DateTime, GetOneColumnTableResultName(config.Features)));
                }
                else if (type0.CoercesTo(DType.DateTime))
                {
                    CollectionUtils.Add(ref nodeToCoercedTypeMap, args[0], DType.DateTime);
                }
                else
                {
                    fValid = false;
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrDateExpected);
                }
            }

            // Arg1 should be either a number or a column of numbers.
            if (type1.IsTable)
            {
                fValid &= CheckNumericColumnType(type1, args[1], errors, ref nodeToCoercedTypeMap);
            }
            else if (!DType.Number.Accepts(type1))
            {
                if (type1.CoercesTo(DType.Number))
                {
                    CollectionUtils.Add(ref nodeToCoercedTypeMap, args[1], DType.Number);
                }
                else
                {
                    fValid = false;
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrNumberExpected);
                }
            }

            var hasUnits = args.Length == 3;
            if (hasUnits && !DType.String.Accepts(argTypes[2]))
            {
                // Arg2 should be a string
                fValid = false;
                errors.EnsureError(DocumentErrorSeverity.Severe, args[2], TexlStrings.ErrStringExpected);
            }

            // At least one arg has to be a table.
            if (!(type0.IsTable || type1.IsTable))
            {
                fValid = false;
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrTypeError);
                errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrTypeError);
            }

            return fValid;
        }
    }

    // DateDiff(startdate: d, enddate : d, [ unit: TimeUnits ]) : n
    internal sealed class DateDiffFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool HasPreciseErrors => true;

        public override bool SupportsParamCoercion => true;

        public DateDiffFunction()
            : base("DateDiff", TexlStrings.AboutDateDiff, FunctionCategories.DateTime, DType.Number, 0, 2, 3, DType.DateTime, DType.DateTime, DType.String)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.DateDiffArg1, TexlStrings.DateDiffArg2 };
            yield return new[] { TexlStrings.DateDiffArg1, TexlStrings.DateDiffArg2, TexlStrings.DateDiffArg3 };
        }

        public override IEnumerable<string> GetRequiredEnumNames()
        {
            return new List<string>() { EnumConstants.TimeUnitEnumString };
        }

        // This method returns true if there are special suggestions for a particular parameter of the function.
        public override bool HasSuggestionsForParam(int argumentIndex)
        {
            Contracts.Assert(argumentIndex >= 0);

            return argumentIndex == 2;
        }
    }

    // DateDiffT(start:d|*[d], end:d|*[d], [unit:TimeUnits])
    internal sealed class DateDiffTFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public DateDiffTFunction()
            : base("DateDiff", TexlStrings.AboutDateDiffT, FunctionCategories.Table, DType.EmptyTable, 0, 2, 3)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.DateDiffTArg1, TexlStrings.DateDiffTArg2 };
            yield return new[] { TexlStrings.DateDiffTArg1, TexlStrings.DateDiffTArg2, TexlStrings.DateDiffTArg3 };
        }

        public override IEnumerable<string> GetRequiredEnumNames()
        {
            return new List<string>() { EnumConstants.TimeUnitEnumString };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "_T");
        }

        public override bool CheckTypes(BindingConfig config, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = CheckTypes(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            var type0 = argTypes[0];
            var type1 = argTypes[1];

            returnType = DType.CreateTable(new TypedName(DType.Number, GetOneColumnTableResultName(config.Features)));

            // Arg0 should be either a date or a column of dates.
            if (type0.IsTable)
            {
                // Ensure we have a one-column table of dates
                fValid &= CheckDateColumnType(type0, args[0], errors, ref nodeToCoercedTypeMap);
            }
            else if (!DType.DateTime.Accepts(type0))
            {
                if (type0.CoercesTo(DType.DateTime))
                {
                    CollectionUtils.Add(ref nodeToCoercedTypeMap, args[0], DType.DateTime);
                }
                else
                {
                    fValid = false;
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrDateExpected);
                }
            }

            // Arg1 should be either a a date or a column of dates.
            if (type1.IsTable)
            {
                // Ensure we have a one-column table of dates
                fValid &= CheckDateColumnType(type1, args[1], errors, ref nodeToCoercedTypeMap);
            }
            else if (!DType.DateTime.Accepts(type1))
            {
                if (type1.CoercesTo(DType.DateTime))
                {
                    CollectionUtils.Add(ref nodeToCoercedTypeMap, args[1], DType.DateTime);
                }
                else
                {
                    fValid = false;
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrDateExpected);
                }
            }

            var hasUnits = args.Length == 3;
            if (hasUnits && !DType.String.Accepts(argTypes[2]))
            {
                // Arg2 should be a string
                fValid = false;
                errors.EnsureError(DocumentErrorSeverity.Severe, args[2], TexlStrings.ErrStringExpected);
            }

            // At least one arg has to be a table.
            if (!(type0.IsTable || type1.IsTable))
            {
                fValid = false;
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrTypeError);
                errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrTypeError);
            }

            return fValid;
        }
    }

    // DateValue(arg:O) : D
    internal sealed class DateValueFunction_UO : BuiltinFunction
    {
        public override bool HasPreciseErrors => true;

        public override bool SupportsParamCoercion => false;

        public override bool IsSelfContained => true;

        public DateValueFunction_UO()
            : base(DateValueFunction.DateValueInvariantFunctionName, TexlStrings.AboutDateValue, FunctionCategories.DateTime, DType.Date, 0, 1, 1, DType.UntypedObject)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.DateValueArg1 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "_UO");
        }
    }

    // TimeValue(time_text:uo) : T
    internal sealed class TimeValueFunction_UO : BuiltinFunction
    {
        public override bool HasPreciseErrors => true;

        public override bool SupportsParamCoercion => false;

        public override bool IsSelfContained => true;

        public TimeValueFunction_UO()
            : base(TimeValueFunction.TimeValueFunctionInvariantName, TexlStrings.AboutTimeValue, FunctionCategories.DateTime, DType.Time, 0, 1, 1, DType.UntypedObject)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.TimeValueArg1 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "_UO");
        }
    }

    // DateTimeValue(arg:O) : d
    internal sealed class DateTimeValueFunction_UO : BuiltinFunction
    {
        public override bool HasPreciseErrors => true;

        public override bool SupportsParamCoercion => false;

        public override bool IsSelfContained => true;

        public DateTimeValueFunction_UO()
            : base(DateTimeValueFunction.DateTimeValueInvariantFunctionName, TexlStrings.AboutDateTimeValue, FunctionCategories.DateTime, DType.DateTime, 0, 1, 1, DType.UntypedObject)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.DateTimeValueArg1 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "_UO");
        }
    }
}
