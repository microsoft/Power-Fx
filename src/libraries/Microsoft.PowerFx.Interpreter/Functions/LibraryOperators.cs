﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
        #region Operator Standard Error Handling Wrappers
        public static readonly FunctionPtr OperatorBinaryAdd = StandardErrorHandling<NumberValue>(
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithZero,
            checkRuntimeTypes: ExactValueType<NumberValue>,
            checkRuntimeValues: FiniteChecker,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: NumericAdd);

        public static readonly FunctionPtr OperatorBinaryMul = StandardErrorHandling<NumberValue>(
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithZero,
            checkRuntimeTypes: ExactValueType<NumberValue>,
            checkRuntimeValues: FiniteChecker,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: NumericMul);

        public static readonly FunctionPtr OperatorBinaryDiv = StandardErrorHandling<NumberValue>(
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithZero,
            checkRuntimeTypes: ExactValueType<NumberValue>,
            checkRuntimeValues: DivideByZeroChecker,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: NumericDiv);

        public static readonly FunctionPtr OperatorBinaryGt = StandardErrorHandling<NumberValue>(
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithZero,
            checkRuntimeTypes: ExactValueType<NumberValue>,
            checkRuntimeValues: FiniteChecker,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: NumericGt);

        public static readonly FunctionPtr OperatorBinaryGeq = StandardErrorHandling<NumberValue>(
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithZero,
            checkRuntimeTypes: ExactValueType<NumberValue>,
            checkRuntimeValues: FiniteChecker,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: NumericGeq);

        public static readonly FunctionPtr OperatorBinaryLt = StandardErrorHandling<NumberValue>(
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithZero,
            checkRuntimeTypes: ExactValueType<NumberValue>,
            checkRuntimeValues: FiniteChecker,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: NumericLt);

        public static readonly FunctionPtr OperatorBinaryLeq = StandardErrorHandling<NumberValue>(
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithZero,
            checkRuntimeTypes: ExactValueType<NumberValue>,
            checkRuntimeValues: FiniteChecker,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: NumericLeq);

        public static readonly FunctionPtr OperatorBinaryEq = StandardErrorHandling<FormulaValue>(
            expandArguments: NoArgExpansion,
            replaceBlankValues: DoNotReplaceBlank,
            checkRuntimeTypes: DeferRuntimeTypeChecking,
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: AreEqual);

        public static readonly FunctionPtr OperatorBinaryNeq = StandardErrorHandling<FormulaValue>(
            expandArguments: NoArgExpansion,
            replaceBlankValues: DoNotReplaceBlank,
            checkRuntimeTypes: DeferRuntimeTypeChecking,
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: NotEqual);

        public static readonly FunctionPtr OperatorTextIn = StandardErrorHandling(
            expandArguments: NoArgExpansion,
            replaceBlankValues: DoNotReplaceBlank,
            checkRuntimeTypes: DeferRuntimeTypeChecking,
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: StringInOperator(false));

        public static readonly FunctionPtr OperatorTextInExact = StandardErrorHandling(
            expandArguments: NoArgExpansion,
            replaceBlankValues: DoNotReplaceBlank,
            checkRuntimeTypes: DeferRuntimeTypeChecking,
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: StringInOperator(true));

        public static readonly FunctionPtr OperatorScalarTableIn = StandardErrorHandling(
            expandArguments: NoArgExpansion,
            replaceBlankValues: DoNotReplaceBlank,
            checkRuntimeTypes: DeferRuntimeTypeChecking,
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: InScalarTableOperator(false));

        public static readonly FunctionPtr OperatorScalarTableInExact = StandardErrorHandling(
            expandArguments: NoArgExpansion,
            replaceBlankValues: DoNotReplaceBlank,
            checkRuntimeTypes: DeferRuntimeTypeChecking,
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: InScalarTableOperator(true));

        public static readonly FunctionPtr OperatorAddDateAndTime = StandardErrorHandling<FormulaValue>(
            expandArguments: NoArgExpansion,
            replaceBlankValues: DoNotReplaceBlank,
            checkRuntimeTypes: ExactSequence(
                DateOrDateTime,
                ExactValueType<TimeValue>),
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: AddDateAndTime);

        public static readonly FunctionPtr OperatorAddDateAndDay = StandardErrorHandling<FormulaValue>(
            expandArguments: NoArgExpansion,
            replaceBlankValues: DoNotReplaceBlank,
            checkRuntimeTypes: ExactSequence(
                DateOrDateTime,
                ExactValueType<NumberValue>),
            checkRuntimeValues: FiniteChecker,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: AddDateAndDay);

        public static readonly FunctionPtr OperatorAddDateTimeAndDay = StandardErrorHandling<FormulaValue>(
            expandArguments: NoArgExpansion,
            replaceBlankValues: DoNotReplaceBlank,
            checkRuntimeTypes: ExactSequence(
                DateOrDateTime,
                ExactValueType<NumberValue>),
            checkRuntimeValues: FiniteChecker,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: AddDateTimeAndDay);

        public static readonly FunctionPtr OperatorDateDifference = StandardErrorHandling<FormulaValue>(
            expandArguments: NoArgExpansion,
            replaceBlankValues: DoNotReplaceBlank,
            checkRuntimeTypes: ExactSequence(
                DateOrDateTime,
                DateOrDateTime),
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: DateDifference);

        public static readonly FunctionPtr OperatorTimeDifference = StandardErrorHandling<FormulaValue>(
            expandArguments: NoArgExpansion,
            replaceBlankValues: DoNotReplaceBlank,
            checkRuntimeTypes: ExactSequence(
                ExactValueType<TimeValue>,
                ExactValueType<TimeValue>),
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: TimeDifference);

        public static readonly FunctionPtr OperatorLtDateTime = StandardErrorHandling<FormulaValue>(
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWith(
                new DateTimeValue(IRContext.NotInSource(FormulaType.DateTime), _epoch),
                new DateTimeValue(IRContext.NotInSource(FormulaType.DateTime), _epoch)),
            checkRuntimeTypes: ExactSequence(
                DateOrDateTime,
                DateOrDateTime),
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: LtDateTime);

        public static readonly FunctionPtr OperatorLeqDateTime = StandardErrorHandling<FormulaValue>(
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWith(
                new DateTimeValue(IRContext.NotInSource(FormulaType.DateTime), _epoch),
                new DateTimeValue(IRContext.NotInSource(FormulaType.DateTime), _epoch)),
            checkRuntimeTypes: ExactSequence(
                DateOrDateTime,
                DateOrDateTime),
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: LeqDateTime);

        public static readonly FunctionPtr OperatorGtDateTime = StandardErrorHandling<FormulaValue>(
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWith(
                new DateTimeValue(IRContext.NotInSource(FormulaType.DateTime), _epoch),
                new DateTimeValue(IRContext.NotInSource(FormulaType.DateTime), _epoch)),
            checkRuntimeTypes: ExactSequence(
                DateOrDateTime,
                DateOrDateTime),
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: GtDateTime);

        public static readonly FunctionPtr OperatorGeqDateTime = StandardErrorHandling<FormulaValue>(
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWith(
                new DateTimeValue(IRContext.NotInSource(FormulaType.DateTime), _epoch),
                new DateTimeValue(IRContext.NotInSource(FormulaType.DateTime), _epoch)),
            checkRuntimeTypes: ExactSequence(
                DateOrDateTime,
                DateOrDateTime),
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: GeqDateTime);

        public static readonly FunctionPtr OperatorLtDate = StandardErrorHandling<FormulaValue>(
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWith(
                new DateValue(IRContext.NotInSource(FormulaType.Date), _epoch),
                new DateValue(IRContext.NotInSource(FormulaType.Date), _epoch)),
            checkRuntimeTypes: ExactSequence(
                DateOrDateTime,
                DateOrDateTime),
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: LtDate);

        public static readonly FunctionPtr OperatorLeqDate = StandardErrorHandling<FormulaValue>(
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWith(
                new DateValue(IRContext.NotInSource(FormulaType.Date), _epoch),
                new DateValue(IRContext.NotInSource(FormulaType.Date), _epoch)),
            checkRuntimeTypes: ExactSequence(
                DateOrDateTime,
                DateOrDateTime),
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: LeqDate);

        public static readonly FunctionPtr OperatorGtDate = StandardErrorHandling<FormulaValue>(
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWith(
                new DateValue(IRContext.NotInSource(FormulaType.Date), _epoch),
                new DateValue(IRContext.NotInSource(FormulaType.Date), _epoch)),
            checkRuntimeTypes: ExactSequence(
                DateOrDateTime,
                DateOrDateTime),
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: GtDate);

        public static readonly FunctionPtr OperatorGeqDate = StandardErrorHandling<FormulaValue>(
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWith(
                new DateValue(IRContext.NotInSource(FormulaType.Date), _epoch),
                new DateValue(IRContext.NotInSource(FormulaType.Date), _epoch)),
            checkRuntimeTypes: ExactSequence(
                DateOrDateTime,
                DateOrDateTime),
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: GeqDate);

        public static readonly FunctionPtr OperatorLtTime = StandardErrorHandling<FormulaValue>(
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWith(
                new TimeValue(IRContext.NotInSource(FormulaType.Time), TimeSpan.Zero),
                new TimeValue(IRContext.NotInSource(FormulaType.Time), TimeSpan.Zero)),
            checkRuntimeTypes: ExactSequence(
                ExactValueType<TimeValue>,
                ExactValueType<TimeValue>),
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: LtTime);

        public static readonly FunctionPtr OperatorLeqTime = StandardErrorHandling<FormulaValue>(
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWith(
                new TimeValue(IRContext.NotInSource(FormulaType.Time), TimeSpan.Zero),
                new TimeValue(IRContext.NotInSource(FormulaType.Time), TimeSpan.Zero)),
            checkRuntimeTypes: ExactSequence(
                ExactValueType<TimeValue>,
                ExactValueType<TimeValue>),
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: LeqTime);

        public static readonly FunctionPtr OperatorGtTime = StandardErrorHandling<FormulaValue>(
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWith(
                new TimeValue(IRContext.NotInSource(FormulaType.Time), TimeSpan.Zero),
                new TimeValue(IRContext.NotInSource(FormulaType.Time), TimeSpan.Zero)),
            checkRuntimeTypes: ExactSequence(
                ExactValueType<TimeValue>,
                ExactValueType<TimeValue>),
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: GtTime);

        public static readonly FunctionPtr OperatorGeqTime = StandardErrorHandling<FormulaValue>(
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWith(
                new TimeValue(IRContext.NotInSource(FormulaType.Time), TimeSpan.Zero),
                new TimeValue(IRContext.NotInSource(FormulaType.Time), TimeSpan.Zero)),
            checkRuntimeTypes: ExactSequence(
                ExactValueType<TimeValue>,
                ExactValueType<TimeValue>),
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: GeqTime);
        #endregion

        private static NumberValue NumericAdd(IRContext irContext, NumberValue[] args)
        {
            var result = args[0].Value + args[1].Value;
            return new NumberValue(irContext, result);
        }

        private static NumberValue NumericMul(IRContext irContext, NumberValue[] args)
        {
            var result = args[0].Value * args[1].Value;
            return new NumberValue(irContext, result);
        }

        private static NumberValue NumericDiv(IRContext irContext, NumberValue[] args)
        {
            var result = args[0].Value / args[1].Value;
            return new NumberValue(irContext, result);
        }

        private static BooleanValue NumericGt(IRContext irContext, NumberValue[] args)
        {
            var result = args[0].Value > args[1].Value;
            return new BooleanValue(irContext, result);
        }

        private static BooleanValue NumericGeq(IRContext irContext, NumberValue[] args)
        {
            var result = args[0].Value >= args[1].Value;
            return new BooleanValue(irContext, result);
        }

        private static BooleanValue NumericLt(IRContext irContext, NumberValue[] args)
        {
            var result = args[0].Value < args[1].Value;
            return new BooleanValue(irContext, result);
        }

        private static BooleanValue NumericLeq(IRContext irContext, NumberValue[] args)
        {
            var result = args[0].Value <= args[1].Value;
            return new BooleanValue(irContext, result);
        }

        private static BooleanValue AreEqual(IRContext irContext, FormulaValue[] args)
        {
            var arg1 = args[0];
            var arg2 = args[1];
            return new BooleanValue(irContext, RuntimeHelpers.AreEqual(arg1, arg2));
        }

        private static BooleanValue NotEqual(IRContext irContext, FormulaValue[] args)
        {
            var arg1 = args[0];
            var arg2 = args[1];
            return new BooleanValue(irContext, !RuntimeHelpers.AreEqual(arg1, arg2));
        }

        // See in_SS in JScript membershipReplacementFunctions
        public static Func<IRContext, FormulaValue[], FormulaValue> StringInOperator(bool exact)
        {
            return (irContext, args) =>
            {
                var left = args[0];
                var right = args[1];
                if (left is BlankValue)
                {
                    return new BooleanValue(irContext, right is BlankValue);
                }

                if (right is BlankValue)
                {
                    return new BooleanValue(irContext, false);
                }

                var leftStr = (StringValue)left;
                var rightStr = (StringValue)right;

                if (exact)
                {
                    return new BooleanValue(irContext, rightStr.Value.IndexOf(leftStr.Value) >= 0);
                }

                return new BooleanValue(irContext, rightStr.Value.ToLowerInvariant().IndexOf(leftStr.Value.ToLowerInvariant()) >= 0);
            };
        }

        // Left is a scalar. Right is a single-column table.
        // See in_ST()
        public static Func<IRContext, FormulaValue[], FormulaValue> InScalarTableOperator(bool exact)
        {
            return (irContext, args) =>
            {
                var left = args[0];
                var right = args[1];

                if (!exact && left is StringValue strLhs)
                {
                    left = strLhs.ToLower();
                }

                var source = (TableValue)right;

                foreach (var row in source.Rows)
                {
                    if (row.IsValue)
                    {
                        var rhs = row.Value.Fields.First().Value;

                        if (!exact && rhs is StringValue strRhs)
                        {
                            right = strRhs.ToLower();
                        }

                        if (RuntimeHelpers.AreEqual(left, rhs))
                        {
                            return new BooleanValue(irContext, true);
                        }
                    }
                }

                return new BooleanValue(irContext, false);
            };
        }

        private static FormulaValue AddDateAndTime(IRContext irContext, FormulaValue[] args)
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

            var arg1 = (TimeValue)args[1];

            try
            {
                var result = arg0.Add(arg1.Value);
                return new DateTimeValue(irContext, result);
            }
            catch
            {
                return CommonErrors.ArgumentOutOfRange(irContext);
            }
        }

        private static FormulaValue AddDateAndDay(IRContext irContext, FormulaValue[] args)
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

            var arg1 = (NumberValue)args[1];

            try
            {
                var result = arg0.AddDays(arg1.Value);
                if (args[0] is DateTimeValue)
                {
                    return new DateTimeValue(irContext, result);
                }
                else
                {
                    return new DateValue(irContext, result.Date);
                }
            }
            catch
            {
                return CommonErrors.ArgumentOutOfRange(irContext);
            }
        }

        private static FormulaValue AddDateTimeAndDay(IRContext irContext, FormulaValue[] args)
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

            var arg1 = (NumberValue)args[1];

            try
            {
                var result = arg0.AddDays(arg1.Value);
                return new DateTimeValue(irContext, result);
            }
            catch
            {
                return CommonErrors.ArgumentOutOfRange(irContext);
            }
        }

        private static FormulaValue DateDifference(IRContext irContext, FormulaValue[] args)
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

            DateTime arg1;
            switch (args[1])
            {
                case DateTimeValue dtv:
                    arg1 = dtv.Value;
                    break;
                case DateValue dv:
                    arg1 = dv.Value;
                    break;
                default:
                    return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            var result = arg0.Subtract(arg1);
            return new NumberValue(irContext, result.Days);
        }

        private static FormulaValue TimeDifference(IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (TimeValue)args[0];
            var arg1 = (TimeValue)args[1];

            var result = arg0.Value.Subtract(arg1.Value);
            return new TimeValue(irContext, result);
        }

        private static FormulaValue LtDateTime(IRContext irContext, FormulaValue[] args)
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

            DateTime arg1;
            switch (args[1])
            {
                case DateTimeValue dtv:
                    arg1 = dtv.Value;
                    break;
                case DateValue dv:
                    arg1 = dv.Value;
                    break;
                default:
                    return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            var result = arg0 < arg1;
            return new BooleanValue(irContext, result);
        }

        private static FormulaValue LeqDateTime(IRContext irContext, FormulaValue[] args)
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

            DateTime arg1;
            switch (args[1])
            {
                case DateTimeValue dtv:
                    arg1 = dtv.Value;
                    break;
                case DateValue dv:
                    arg1 = dv.Value;
                    break;
                default:
                    return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            var result = arg0 <= arg1;
            return new BooleanValue(irContext, result);
        }

        private static FormulaValue GtDateTime(IRContext irContext, FormulaValue[] args)
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

            DateTime arg1;
            switch (args[1])
            {
                case DateTimeValue dtv:
                    arg1 = dtv.Value;
                    break;
                case DateValue dv:
                    arg1 = dv.Value;
                    break;
                default:
                    return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            var result = arg0 > arg1;
            return new BooleanValue(irContext, result);
        }

        private static FormulaValue GeqDateTime(IRContext irContext, FormulaValue[] args)
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

            DateTime arg1;
            switch (args[1])
            {
                case DateTimeValue dtv:
                    arg1 = dtv.Value;
                    break;
                case DateValue dv:
                    arg1 = dv.Value;
                    break;
                default:
                    return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            var result = arg0 >= arg1;
            return new BooleanValue(irContext, result);
        }

        private static FormulaValue LtDate(IRContext irContext, FormulaValue[] args)
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

            DateTime arg1;
            switch (args[1])
            {
                case DateTimeValue dtv:
                    arg1 = dtv.Value;
                    break;
                case DateValue dv:
                    arg1 = dv.Value;
                    break;
                default:
                    return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            var result = arg0 < arg1;
            return new BooleanValue(irContext, result);
        }

        private static FormulaValue LeqDate(IRContext irContext, FormulaValue[] args)
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

            DateTime arg1;
            switch (args[1])
            {
                case DateTimeValue dtv:
                    arg1 = dtv.Value;
                    break;
                case DateValue dv:
                    arg1 = dv.Value;
                    break;
                default:
                    return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            var result = arg0 <= arg1;
            return new BooleanValue(irContext, result);
        }

        private static FormulaValue GtDate(IRContext irContext, FormulaValue[] args)
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

            DateTime arg1;
            switch (args[1])
            {
                case DateTimeValue dtv:
                    arg1 = dtv.Value;
                    break;
                case DateValue dv:
                    arg1 = dv.Value;
                    break;
                default:
                    return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            var result = arg0 > arg1;
            return new BooleanValue(irContext, result);
        }

        private static FormulaValue GeqDate(IRContext irContext, FormulaValue[] args)
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

            DateTime arg1;
            switch (args[1])
            {
                case DateTimeValue dtv:
                    arg1 = dtv.Value;
                    break;
                case DateValue dv:
                    arg1 = dv.Value;
                    break;
                default:
                    return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            var result = arg0 >= arg1;
            return new BooleanValue(irContext, result);
        }

        private static FormulaValue LtTime(IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (TimeValue)args[0];
            var arg1 = (TimeValue)args[1];

            var result = arg0.Value < arg1.Value;
            return new BooleanValue(irContext, result);
        }

        private static FormulaValue LeqTime(IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (TimeValue)args[0];
            var arg1 = (TimeValue)args[1];

            var result = arg0.Value <= arg1.Value;
            return new BooleanValue(irContext, result);
        }

        private static FormulaValue GtTime(IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (TimeValue)args[0];
            var arg1 = (TimeValue)args[1];

            var result = arg0.Value > arg1.Value;
            return new BooleanValue(irContext, result);
        }

        private static FormulaValue GeqTime(IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (TimeValue)args[0];
            var arg1 = (TimeValue)args[1];

            var result = arg0.Value >= arg1.Value;
            return new BooleanValue(irContext, result);
        }
    }
}
