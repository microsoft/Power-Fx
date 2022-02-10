// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Functions
{
    internal partial class Library
    {
        private static readonly DateTime _epoch = new DateTime(1899, 12, 30, 0, 0, 0, 0);

        public static IReadOnlyDictionary<UnaryOpKind, FunctionPtr> UnaryOps => _unaryOps;

        #region Standard Error Handling Wrappers for Unary Operators
        private static readonly Dictionary<UnaryOpKind, FunctionPtr> _unaryOps = new Dictionary<UnaryOpKind, FunctionPtr>()
        {
            {
                UnaryOpKind.Negate,
                StandardErrorHandling<NumberValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueType<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: NumericNegate)
            },
            {
                UnaryOpKind.Percent,
                StandardErrorHandling<NumberValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueType<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: NumericPercent)
            },
            {
                UnaryOpKind.NumberToText,
                StandardErrorHandling<NumberValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: NumberToText)
            },
            {
                UnaryOpKind.NumberToBoolean,
                StandardErrorHandling<NumberValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: NumberToBoolean)
            },
            {
                UnaryOpKind.BooleanToText,
                StandardErrorHandling<BooleanValue>(
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
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: NumberToDate)
            },
            {
                UnaryOpKind.NumberToDateTime,
                StandardErrorHandling<NumberValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: NumberToDateTime)
            },
            {
                UnaryOpKind.DateToDateTime,
                StandardErrorHandling<FormulaValue>(
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
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: NumberToTime)
            },
            {
                UnaryOpKind.DateTimeToTime,
                StandardErrorHandling<FormulaValue>(
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

        public static FormulaValue NumberToText(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, NumberValue[] args)
        {
            return Text(runner, symbolContext, irContext, args);
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

        public static BooleanValue TextToBoolean(IRContext irContext, StringValue[] args)
        {
            var s = args[0].Value;
            return new BooleanValue(irContext, s == "true");
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

        public static DateTimeValue NumberToDateTime(IRContext irContext, NumberValue[] args)
        {
            var n = args[0].Value;
            var date = _epoch.AddDays(n);
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

        public static DateValue TimeToDate(IRContext irContext, TimeValue[] args)
        {
            var t = args[0].Value;
            var date = _epoch.Add(t);
            return new DateValue(irContext, date.Date);
        }

        public static DateTimeValue TimeToDateTime(IRContext irContext, TimeValue[] args)
        {
            var t = args[0].Value;
            var date = _epoch.Add(t);
            return new DateTimeValue(irContext, date);
        }

        public static FormulaValue OptionSetValueToString(IRContext irContext, OptionSetValue[] args)
        {
            // The type checker and IR have already validated that this is a valid OptionSet and that it contains a member matching this Option.
            // These are just defensive error checks, and should be unreachable.
            var os = args[0].Type?._type?.OptionSetInfo;
            if (os is not OptionSet optionSet) 
            {
                return CommonErrors.UnreachableCodeError(irContext);
            }

            var option = args[0].Option;

            if (!optionSet.Options.TryGetValue(new DName(option), out var displayName))
            { 
                return CommonErrors.UnreachableCodeError(irContext);
            }

            return new StringValue(irContext, displayName);
        }
        #endregion
    }
}
