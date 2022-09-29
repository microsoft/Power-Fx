// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    internal partial class Library
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
            }
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
            var b = args[0].Value;
            return new NumberValue(irContext, b ? 1.0 : 0.0);
        }

        public static FormulaValue TextToBoolean(IRContext irContext, StringValue[] args)
        {
            var val = args[0].Value;

            if (string.IsNullOrEmpty(val))
            {
                return new BlankValue(irContext);
            }

            var lower = val.ToLowerInvariant();
            if (lower == "true")
            {
                return new BooleanValue(irContext, true);
            }
            else if (lower == "false")
            {
                return new BooleanValue(irContext, false);
            }

            return CommonErrors.InvalidBooleanFormatError(irContext);
        }

        public static FormulaValue DateToNumber(IRContext irContext, FormulaValue[] args)
        {
            DateTime arg0;
            switch (args[0])
            {
                case DateTimeValue dtv:
                    arg0 = dtv.Value;
                    break;
                case DateValue dv:
                    arg0 = dv.Value;
                    break;
                default:
                    return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            var diff = arg0.Subtract(_epoch).TotalDays;
            return new NumberValue(irContext, diff);
        }

        public static NumberValue DateTimeToNumber(IRContext irContext, DateTimeValue[] args)
        {
            var d = args[0].Value;
            var diff = d.Subtract(_epoch).TotalDays;
            return new NumberValue(irContext, diff);
        }

        public static DateValue NumberToDate(IRContext irContext, NumberValue[] args)
        {
            var n = args[0].Value;
            var date = _epoch.AddDays(n);
            return new DateValue(irContext, date);
        }

        public static DateTimeValue NumberToDateTime(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, NumberValue[] args)
        {
            var n = args[0].Value;
            var date = _epoch.AddDays(n);

            date = MakeValidDateTime(runner, date, runner.GetService<TimeZoneInfo>() ?? LocalTimeZone);            

            return new DateTimeValue(irContext, date);
        }

        public static FormulaValue DateToDateTime(IRContext irContext, FormulaValue[] args)
        {
            switch (args[0])
            {
                case DateTimeValue dtv:
                    return dtv;
                case DateValue dv:
                    return new DateTimeValue(irContext, dv.Value);
                default:
                    return CommonErrors.RuntimeTypeMismatch(irContext);
            }
        }

        public static FormulaValue DateTimeToDate(IRContext irContext, FormulaValue[] args)
        {
            switch (args[0])
            {
                case DateTimeValue dtv:
                    var startOfDate = dtv.Value.Date;
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

        public static FormulaValue DateTimeToTime(IRContext irContext, FormulaValue[] args)
        {
            DateTime arg0;
            switch (args[0])
            {
                case DateTimeValue dtv:
                    arg0 = dtv.Value;
                    break;
                case DateValue dv:
                    arg0 = dv.Value;
                    break;
                default:
                    return CommonErrors.RuntimeTypeMismatch(irContext);
            }

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
            var t = args[0].Value;
            var date = _epoch.Add(t);

            date = MakeValidDateTime(runner, date, runner.GetService<TimeZoneInfo>() ?? LocalTimeZone);

            return new DateTimeValue(irContext, date);
        }

        public static FormulaValue OptionSetValueToString(IRContext irContext, OptionSetValue[] args)
        {
            var optionSet = args[0];
            var displayName = optionSet.DisplayName;
            return new StringValue(irContext, displayName);
        }

        public static FormulaValue TextToColor(IRContext irContext, StringValue[] args)
        {
            var val = args[0].Value;

            if (string.IsNullOrEmpty(val))
            {
                return new BlankValue(irContext);
            }

            var regex = new Regex(@"^#(?<r>[0-9a-fA-F]{2})(?<g>[0-9a-fA-F]{2})(?<b>[0-9a-fA-F]{2})(?<a>[0-9a-fA-F]{2})?$");
            var match = regex.Match(val);
            if (match.Success)
            {
                var r = byte.Parse(match.Groups["r"].Value, System.Globalization.NumberStyles.HexNumber);
                var g = byte.Parse(match.Groups["g"].Value, System.Globalization.NumberStyles.HexNumber);
                var b = byte.Parse(match.Groups["b"].Value, System.Globalization.NumberStyles.HexNumber);
                var a = match.Groups["a"].Captures.Count == 1 ? byte.Parse(match.Groups["a"].Value, System.Globalization.NumberStyles.HexNumber) : 255;

                return new ColorValue(irContext, Color.FromArgb(a, r, g, b));
            }

            return CommonErrors.InvalidColorFormatError(irContext);
        }

        public static FormulaValue RGBA(IRContext irContext, NumberValue[] args)
        {
            if (args.Length != 4)
            {
                return CommonErrors.GenericInvalidArgument(irContext);
            }

            // Ensure rgb numbers are in range (0-255)
            if (args[0].Value < 0.0d || args[0].Value > 255.0d
                || args[1].Value < 0.0d || args[1].Value > 255.0d
                || args[2].Value < 0.0d || args[2].Value > 255.0d)
            {
                return CommonErrors.ArgumentOutOfRange(irContext);
            }

            // Truncate (strip decimals)
            var r = (int)args[0].Value;
            var g = (int)args[1].Value;
            var b = (int)args[2].Value;

            // Ensure alpha is between 0 and 1
            if (args[3].Value < 0.0d || args[3].Value > 1.0d)
            {
                return CommonErrors.ArgumentOutOfRange(irContext);
            }

            var a = System.Convert.ToInt32(args[3].Value * 255.0d);

            return new ColorValue(irContext, Color.FromArgb(a, r, g, b));
        }
        #endregion
    }
}
