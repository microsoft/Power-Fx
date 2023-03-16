// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Interpreter.Exceptions;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
        #region Standard Error Handling Wrappers for Unary Operators
        public static IReadOnlyDictionary<UnaryOpKind, AsyncFunctionPtr> UnaryOps { get; } = new Dictionary<UnaryOpKind, AsyncFunctionPtr>()
        {
            {
                UnaryOpKind.Negate,
                StandardErrorHandling<NumberValue>(
                    "-",
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: DateNumberTimeOrDateTime,
                    checkRuntimeValues: DeferRuntimeTypeChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: NumericNegate)
            },
            {
                UnaryOpKind.Percent,
                StandardErrorHandling<NumberValue>(
                    "%",
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueType<NumberValue>,
                    checkRuntimeValues: DeferRuntimeTypeChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: NumericPercent)
            },
            {
                UnaryOpKind.NumberToText,
                StandardErrorHandling<NumberValue>(
                    functionName: null, // internal function, no user-facing name
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeTypeChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: NumberToText)
            },
            {
                UnaryOpKind.NumberToBoolean,
                StandardErrorHandling<NumberValue>(
                    functionName: null, // internal function, no user-facing name
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeTypeChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: NumberToBoolean)
            },
            {
                UnaryOpKind.BooleanToText,
                StandardErrorHandling<BooleanValue>(
                    functionName: null, // internal function, no user-facing name
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<BooleanValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: BooleanToText)
            },
            {
                UnaryOpKind.BooleanToNumber,
                StandardErrorHandling<BooleanValue>(
                    functionName: null, // internal function, no user-facing name
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<BooleanValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: BooleanToNumber)
            },
            {
                UnaryOpKind.TextToBoolean,
                StandardErrorHandling<StringValue>(
                    functionName: null, // internal function, no user-facing name
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: TextToBoolean)
            },
            {
                UnaryOpKind.DateToNumber,
                StandardErrorHandling<FormulaValue>(
                    functionName: null, // internal function, no user-facing name
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DateOrDateTime,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: DateToNumber)
            },
            {
                UnaryOpKind.NumberToDate,
                StandardErrorHandling<NumberValue>(
                    functionName: null, // internal function, no user-facing name
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeTypeChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: NumberToDate)
            },
            {
                UnaryOpKind.DateTimeToNumber,
                StandardErrorHandling<FormulaValue>(
                    functionName: null, // internal function, no user-facing name
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DateOrDateTime,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: DateToNumber)
            },
            {
                UnaryOpKind.NumberToDateTime,
                StandardErrorHandling<NumberValue>(
                    functionName: null, // internal function, no user-facing name
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeTypeChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: NumberToDateTime)
            },
            {
                UnaryOpKind.DateToDateTime,
                StandardErrorHandling<FormulaValue>(
                    functionName: null, // internal function, no user-facing name
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DateOrDateTime,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: DateToDateTime)
            },
            {
                UnaryOpKind.DateTimeToDate,
                StandardErrorHandling<FormulaValue>(
                    functionName: null, // internal function, no user-facing name
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DateOrDateTime,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: DateTimeToDate)
            },
            {
                UnaryOpKind.TimeToNumber,
                StandardErrorHandling<TimeValue>(
                    functionName: null, // internal function, no user-facing name
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<TimeValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: TimeToNumber)
            },
            {
                UnaryOpKind.NumberToTime,
                StandardErrorHandling<NumberValue>(
                    functionName: null, // internal function, no user-facing name
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeTypeChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: NumberToTime)
            },
            {
                UnaryOpKind.DateTimeToTime,
                StandardErrorHandling<FormulaValue>(
                    functionName: null, // internal function, no user-facing name
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DateOrDateTime,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: DateTimeToTime)
            },
            {
                UnaryOpKind.DateToTime,
                StandardErrorHandling<FormulaValue>(
                    functionName: null, // internal function, no user-facing name
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DateOrDateTime,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: DateTimeToTime)
            },
            {
                UnaryOpKind.TimeToDate,
                StandardErrorHandling<TimeValue>(
                    functionName: null, // internal function, no user-facing name
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<TimeValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: TimeToDate)
            },
            {
                UnaryOpKind.TimeToDateTime,
                StandardErrorHandling<TimeValue>(
                    functionName: null, // internal function, no user-facing name
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<TimeValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: TimeToDateTime)
            },
            {
                UnaryOpKind.TextToDate,
                StandardErrorHandling<StringValue>(
                    functionName: null, // internal function, no user-facing name
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: DateParse)
            },
            {
                UnaryOpKind.TextToDateTime,
                StandardErrorHandling<StringValue>(
                    functionName: null, // internal function, no user-facing name
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: DateTimeParse)
            },
            {
                UnaryOpKind.TextToTime,
                StandardErrorHandling<StringValue>(
                    functionName: null, // internal function, no user-facing name
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: TimeParse)
            },
            {
                UnaryOpKind.OptionSetToText,
                StandardErrorHandling<OptionSetValue>(
                    functionName: null, // internal function, no user-facing name
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<OptionSetValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: OptionSetValueToString)
            },
            {
                UnaryOpKind.OptionSetToNumber,
                StandardErrorHandling<OptionSetValue>(
                    functionName: null, // internal function, no user-facing name
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<OptionSetValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: OptionSetValueToNumber)
            },
            {
                UnaryOpKind.OptionSetToBoolean,
                StandardErrorHandling<OptionSetValue>(
                    functionName: null, // internal function, no user-facing name
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<OptionSetValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: OptionSetValueToBoolean)
            },
            {
                UnaryOpKind.OptionSetToColor,
                StandardErrorHandling<OptionSetValue>(
                    functionName: null, // internal function, no user-facing name
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<OptionSetValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: OptionSetValueToColor)
            },
            {
                UnaryOpKind.BlankToEmptyString,
                StandardErrorHandling<FormulaValue>(
                    functionName: null, // internal function, no user-facing name
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<FormulaValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: BlankToEmptyString)
            },
        };
        #endregion

        #region Unary Operator Implementations
        private static NumberValue NumericNegate(IRContext irContext, NumberValue[] args)
        {
            var result = -args[0].Value;
            return new NumberValue(irContext, result);
        }

        private static NumberValue NumericPercent(IRContext irContext, NumberValue[] args)
        {
            var result = args[0].Value / 100.0;
            return new NumberValue(irContext, result);
        }

        public static FormulaValue NumberToText(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, NumberValue[] args)
        {
            return Text(runner, context, irContext, args);
        }

        public static BooleanValue NumberToBoolean(IRContext irContext, NumberValue[] args)
        {
            var n = args[0].Value;
            return new BooleanValue(irContext, n != 0.0);
        }

        public static StringValue BooleanToText(IRContext irContext, BooleanValue[] args)
        {
            var b = args[0].Value;
            return new StringValue(irContext, PowerFxBooleanToString(b));
        }

        private static string PowerFxBooleanToString(bool b)
        {
            return b ? "true" : "false";
        }

        public static NumberValue BooleanToNumber(IRContext irContext, BooleanValue[] args)
        {
            return BooleanToNumber(irContext, args[0]);
        }

        public static NumberValue BooleanToNumber(IRContext irContext, BooleanValue value)
        {
            return new NumberValue(irContext, value.Value ? 1.0 : 0.0);
        }

        public static FormulaValue TextToBoolean(IRContext irContext, StringValue[] args)
        {
            if (string.IsNullOrEmpty(args[0].Value))
            {
                return new BlankValue(irContext);
            }

            bool isBoolean = TryTextToBoolean(irContext, args[0], out BooleanValue result);

            return isBoolean ? result : CommonErrors.InvalidBooleanFormatError(irContext);
        }

        public static bool TryTextToBoolean(IRContext irContext, StringValue value, out BooleanValue result)
        {
            result = null;
            var lower = value.Value.ToLowerInvariant();

            if (lower == "true")
            {
                result = new BooleanValue(irContext, true);
            }
            else if (lower == "false")
            {
                result = new BooleanValue(irContext, false);
            }

            return result != null;
        }

        public static bool TryGetBoolean(IRContext irContext, FormulaValue value, out BooleanValue result)
        {
            result = null;
            switch (value)
            {
                case StringValue sv:
                    if (string.IsNullOrEmpty(sv.Value))
                    {
                        return false;
                    }

                    return TryTextToBoolean(irContext, sv, out result);
                case NumberValue num:
                    result = NumberToBoolean(irContext, new NumberValue[] { num });
                    break;
                case BooleanValue boolVal:
                    result = boolVal;
                    break;
            }

            return result != null;
        }

        public static DateTime GetNormalizedDateTimeLibrary(FormulaValue value, TimeZoneInfo timeZoneInfo)
        {
            switch (value)
            {
                case DateTimeValue dtv:
                    return dtv.GetConvertedValue(timeZoneInfo);
                case DateValue dv:
                    return dv.GetConvertedValue(timeZoneInfo);
                default:
                    throw CommonExceptions.RuntimeMisMatch;
            }
        }

        public static FormulaValue DateToNumber(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            return DateToNumber(CreateFormattingInfo(runner), irContext, args[0]);
        }

        public static NumberValue DateToNumber(FormattingInfo formatInfo, IRContext irContext, FormulaValue value)
        {
            DateTime dateTime = GetNormalizedDateTimeLibrary(value, formatInfo.TimeZoneInfo);
            return new NumberValue(irContext, dateTime.Subtract(_epoch).TotalDays);
        }

        public static NumberValue DateTimeToNumber(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, DateTimeValue[] args)
        {
            return DateTimeToNumber(CreateFormattingInfo(runner), irContext, args[0]);
        }

        public static NumberValue DateTimeToNumber(FormattingInfo formatInfo, IRContext irContext, DateTimeValue value)
        {
            var d = value.GetConvertedValue(formatInfo.TimeZoneInfo);
            return new NumberValue(irContext, d.Subtract(_epoch).TotalDays);
        }

        public static DateValue NumberToDate(IRContext irContext, NumberValue[] args)
        {
            var n = args[0].Value;
            var date = _epoch.AddDays(n);
            return new DateValue(irContext, date);
        }

        public static DateTimeValue NumberToDateTime(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, NumberValue[] args)
        {
            return NumberToDateTime(CreateFormattingInfo(runner), irContext, args[0]);
        }

        public static DateTimeValue NumberToDateTime(FormattingInfo formatInfo, IRContext irContext, NumberValue value)
        {
            try
            {
                var n = value.Value;
                var date = _epoch.AddDays(n);

                date = MakeValidDateTime(formatInfo.TimeZoneInfo, date);

                return new DateTimeValue(irContext, date);
            }
            catch (ArgumentOutOfRangeException)
            {
                (var shortMessage, _) = ErrorUtils.GetLocalizedErrorContent(TexlStrings.ErrTextInvalidArgDateTime, formatInfo.CultureInfo, out _);

                throw new CustomFunctionErrorException(shortMessage, ErrorKind.InvalidArgument);
            }          
        }

        public static FormulaValue DateToDateTime(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var timeZoneInfo = runner.TimeZoneInfo;
            switch (args[0])
            {
                case DateTimeValue dtv:
                    return dtv;
                case DateValue dv:
                    return new DateTimeValue(irContext, dv.GetConvertedValue(timeZoneInfo));
                default:
                    return CommonErrors.RuntimeTypeMismatch(irContext);
            }
        }

        public static bool TryGetDateTime(FormattingInfo formatInfo, IRContext irContext, FormulaValue value, out DateTimeValue result)
        {
            result = null;

            switch (value)
            {
                case StringValue st:
                    return TryDateTimeParse(formatInfo, irContext, st, out result);
                case NumberValue num:
                    result = NumberToDateTime(formatInfo, irContext, num);
                    break;
                case DateValue dv:
                    result = new DateTimeValue(irContext, dv.GetConvertedValue(formatInfo.TimeZoneInfo));
                    break;
                case DateTimeValue dtv:
                    result = dtv;
                    break;
            }

            return result != null;
        }

        public static FormulaValue DateTimeToDate(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var timeZoneInfo = runner.TimeZoneInfo;
            switch (args[0])
            {
                case DateTimeValue dtv:
                    var startOfDate = dtv.GetConvertedValue(timeZoneInfo).Date;
                    return new DateValue(irContext, startOfDate);
                case DateValue dv:
                    return dv;
                default:
                    return CommonErrors.RuntimeTypeMismatch(irContext);
            }
        }

        public static NumberValue TimeToNumber(IRContext irContext, TimeValue[] args)
        {
            var t = args[0].Value;
            return new NumberValue(irContext, t.TotalDays);
        }

        public static TimeValue NumberToTime(IRContext irContext, NumberValue[] args)
        {
            var n = args[0].Value;
            var days = TimeSpan.FromDays(n);
            return new TimeValue(irContext, days);
        }

        public static FormulaValue DateTimeToTime(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            TimeZoneInfo timeZoneInfo = runner.TimeZoneInfo;
            DateTime arg0 = runner.GetNormalizedDateTime(args[0]);

            var time = arg0.TimeOfDay;
            return new TimeValue(irContext, time);
        }

        public static FormulaValue TimeToDate(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, TimeValue[] args)
        {
            var t = args[0].Value;
            var date = _epoch.Add(t);

            if (!date.IsValid(runner))
            {
                return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            return new DateValue(irContext, date.Date);
        }

        public static DateTimeValue TimeToDateTime(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, TimeValue[] args)
        {
            return TimeToDateTime(CreateFormattingInfo(runner), irContext, args[0]);
        }

        public static DateTimeValue TimeToDateTime(FormattingInfo formatInfo, IRContext irContext, TimeValue value)
        {
            var t = value.Value;
            var date = _epoch.Add(t);

            date = MakeValidDateTime(formatInfo.TimeZoneInfo, date);

            return new DateTimeValue(irContext, date);
        }

        public static StringValue OptionSetValueToString(IRContext irContext, OptionSetValue[] args)
        {
            var optionSet = args[0];
            if (optionSet.ExecutionValue is string evalValue)
            {                
                return new StringValue(IRContext.NotInSource(FormulaType.String), evalValue);
            }

            return new StringValue(irContext, optionSet.DisplayName);
        }
        
        public static FormulaValue OptionSetValueToNumber(IRContext irContext, OptionSetValue[] args)
        {
            var optionSet = args[0];
            if (optionSet.ExecutionValue is double evalValue)
            {                
                return new NumberValue(IRContext.NotInSource(FormulaType.Number), evalValue);
            }

            var errorMessage = ErrorUtils.FormatMessage(StringResources.Get(TexlStrings.OptionSetOptionNotSupported), null, optionSet.DisplayName, FormulaType.Number._type.GetKindString());
            return CommonErrors.CustomError(IRContext.NotInSource(FormulaType.Number), errorMessage);
        }

        public static FormulaValue OptionSetValueToBoolean(IRContext irContext, OptionSetValue[] args)
        {
            var optionSet = args[0];
            if (optionSet.ExecutionValue is bool evalValue)
            {                
                return new BooleanValue(IRContext.NotInSource(FormulaType.Boolean), evalValue);
            }

            var errorMessage = ErrorUtils.FormatMessage(StringResources.Get(TexlStrings.OptionSetOptionNotSupported), null, optionSet.DisplayName, FormulaType.Boolean._type.GetKindString());
            return CommonErrors.CustomError(IRContext.NotInSource(FormulaType.Boolean), errorMessage);
        }

        public static FormulaValue OptionSetValueToColor(IRContext irContext, OptionSetValue[] args)
        {
            var optionSet = args[0];
            if (optionSet.ExecutionValue is double evalValue)
            {
                // Color enums are backed by a double
                return new ColorValue(IRContext.NotInSource(FormulaType.Color), ToColor(evalValue));
            }

            var errorMessage = ErrorUtils.FormatMessage(StringResources.Get(TexlStrings.OptionSetOptionNotSupported), null, optionSet.DisplayName, FormulaType.Color._type.GetKindString());
            return CommonErrors.CustomError(IRContext.NotInSource(FormulaType.Color), errorMessage);
        }

        private static System.Drawing.Color ToColor(double doubValue)
        {
            var value = Convert.ToUInt32(doubValue);
            return System.Drawing.Color.FromArgb(
                        (byte)((value >> 24) & 0xFF),
                        (byte)((value >> 16) & 0xFF),
                        (byte)((value >> 8) & 0xFF),
                        (byte)(value & 0xFF));
        }

        public static FormulaValue BlankToEmptyString(IRContext irContext, FormulaValue[] args)
        {
            if (args[0] is BlankValue)
            {
                return new StringValue(irContext, string.Empty);
            }

            return args[0];
        }
        #endregion
    }
}
