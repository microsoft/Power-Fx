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
        public static readonly AsyncFunctionPtr OperatorBinaryAdd = StandardErrorHandling<NumberValue>(
            "+",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithZero,
            checkRuntimeTypes: ExactValueType<NumberValue>,
            checkRuntimeValues: DeferRuntimeTypeChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: NumericAdd);

        public static readonly AsyncFunctionPtr OperatorBinaryMul = StandardErrorHandling<NumberValue>(
            "*",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithZero,
            checkRuntimeTypes: ExactValueType<NumberValue>,
            checkRuntimeValues: DeferRuntimeTypeChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: NumericMul);

        public static readonly AsyncFunctionPtr OperatorBinaryDiv = StandardErrorHandling<NumberValue>(
            "/",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithZero,
            checkRuntimeTypes: ExactValueType<NumberValue>,
            checkRuntimeValues: DeferRuntimeTypeChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: NumericDiv);

        public static readonly AsyncFunctionPtr OperatorBinaryGt = StandardErrorHandling<NumberValue>(
            ">",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithZero,
            checkRuntimeTypes: ExactValueType<NumberValue>,
            checkRuntimeValues: DeferRuntimeTypeChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: NumericGt);

        public static readonly AsyncFunctionPtr OperatorBinaryGeq = StandardErrorHandling<NumberValue>(
            ">=",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithZero,
            checkRuntimeTypes: ExactValueType<NumberValue>,
            checkRuntimeValues: DeferRuntimeTypeChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: NumericGeq);

        public static readonly AsyncFunctionPtr OperatorBinaryLt = StandardErrorHandling<NumberValue>(
            "<",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithZero,
            checkRuntimeTypes: ExactValueType<NumberValue>,
            checkRuntimeValues: DeferRuntimeTypeChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: NumericLt);

        public static readonly AsyncFunctionPtr OperatorBinaryLeq = StandardErrorHandling<NumberValue>(
            "<=",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithZero,
            checkRuntimeTypes: ExactValueType<NumberValue>,
            checkRuntimeValues: DeferRuntimeTypeChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: NumericLeq);

        public static readonly AsyncFunctionPtr OperatorBinaryEq = StandardErrorHandling<FormulaValue>(
            "=",
            expandArguments: NoArgExpansion,
            replaceBlankValues: DoNotReplaceBlank,
            checkRuntimeTypes: DeferRuntimeTypeChecking,
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: AreEqual);

        public static readonly AsyncFunctionPtr OperatorBinaryNeq = StandardErrorHandling<FormulaValue>(
            "<>",
            expandArguments: NoArgExpansion,
            replaceBlankValues: DoNotReplaceBlank,
            checkRuntimeTypes: DeferRuntimeTypeChecking,
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: NotEqual);

        public static readonly AsyncFunctionPtr OperatorTextIn = StandardErrorHandling(
            "in",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithEmptyString,
            checkRuntimeTypes: DeferRuntimeTypeChecking,
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: StringInOperator(false));

        public static readonly AsyncFunctionPtr OperatorTextInExact = StandardErrorHandling(
            "exactin",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithEmptyString,
            checkRuntimeTypes: DeferRuntimeTypeChecking,
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: StringInOperator(true));

        public static readonly AsyncFunctionPtr OperatorScalarTableIn = StandardErrorHandling(
            "in",
            expandArguments: NoArgExpansion,
            replaceBlankValues: DoNotReplaceBlank,
            checkRuntimeTypes: DeferRuntimeTypeChecking,
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: InScalarTableOperator(false));

        public static readonly AsyncFunctionPtr OperatorScalarTableInExact = StandardErrorHandling(
            "exactin",
            expandArguments: NoArgExpansion,
            replaceBlankValues: DoNotReplaceBlank,
            checkRuntimeTypes: DeferRuntimeTypeChecking,
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: InScalarTableOperator(true));

        public static readonly AsyncFunctionPtr OperatorAddDateAndTime = StandardErrorHandling<FormulaValue>(
            "+",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWith(
                new DateValue(IRContext.NotInSource(FormulaType.Date), _epoch),
                new TimeValue(IRContext.NotInSource(FormulaType.Time), TimeSpan.Zero)),
            checkRuntimeTypes: ExactSequence(
                DateOrDateTime,
                ExactValueType<TimeValue>),
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: AddDateAndTime);

        public static readonly AsyncFunctionPtr OperatorAddTimeAndNumber = StandardErrorHandling<FormulaValue>(
            "+",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWith(
                new TimeValue(IRContext.NotInSource(FormulaType.Time), TimeSpan.Zero),
                new NumberValue(IRContext.NotInSource(FormulaType.Number), 0)),
            checkRuntimeTypes: ExactSequence(
                TimeOrDateTime,
                ExactValueType<NumberValue>),
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: AddTimeAndNumber);

        public static readonly AsyncFunctionPtr OperatorAddTimeAndTime = StandardErrorHandling<FormulaValue>(
            "+",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWith(
                new TimeValue(IRContext.NotInSource(FormulaType.Time), TimeSpan.Zero),
                new TimeValue(IRContext.NotInSource(FormulaType.Time), TimeSpan.Zero)),
            checkRuntimeTypes: ExactSequence(
                TimeOrDateTime,
                TimeOrDateTime),
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: AddTimeAndTime);

        public static readonly AsyncFunctionPtr OperatorAddDateAndDay = StandardErrorHandling<FormulaValue>(
            "+",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWith(
                new DateValue(IRContext.NotInSource(FormulaType.Date), _epoch),
                new NumberValue(IRContext.NotInSource(FormulaType.Number), 0)),
            checkRuntimeTypes: ExactSequence(
                DateOrDateTime,
                ExactValueType<NumberValue>),
            checkRuntimeValues: DeferRuntimeTypeChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: AddDateAndDay);

        public static readonly AsyncFunctionPtr OperatorAddDateTimeAndDay = StandardErrorHandling<FormulaValue>(
            "+",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWith(
                new DateValue(IRContext.NotInSource(FormulaType.Date), _epoch),
                new NumberValue(IRContext.NotInSource(FormulaType.Number), 0)),
            checkRuntimeTypes: ExactSequence(
                DateOrDateTime,
                ExactValueType<NumberValue>),
            checkRuntimeValues: DeferRuntimeTypeChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: AddDateTimeAndDay);

        public static readonly AsyncFunctionPtr OperatorDateDifference = StandardErrorHandling<FormulaValue>(
            "-",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWith(
                new DateValue(IRContext.NotInSource(FormulaType.Date), _epoch),
                new DateValue(IRContext.NotInSource(FormulaType.Date), _epoch)),
            checkRuntimeTypes: ExactSequence(
                DateOrDateTime,
                DateOrDateTime),
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: DateDifference);

        public static readonly AsyncFunctionPtr OperatorTimeDifference = StandardErrorHandling<FormulaValue>(
            "-",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWith(
                new TimeValue(IRContext.NotInSource(FormulaType.Time), TimeSpan.Zero),
                new TimeValue(IRContext.NotInSource(FormulaType.Time), TimeSpan.Zero)),
            checkRuntimeTypes: ExactSequence(
                ExactValueType<TimeValue>,
                ExactValueType<TimeValue>),
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: TimeDifference);

        public static readonly AsyncFunctionPtr OperatorSubtractDateAndTime = StandardErrorHandling<FormulaValue>(
            "-",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWith(
                new DateValue(IRContext.NotInSource(FormulaType.Date), _epoch),
                new TimeValue(IRContext.NotInSource(FormulaType.Time), TimeSpan.Zero)),
            checkRuntimeTypes: ExactSequence(
                DateOrDateTime,
                ExactValueType<TimeValue>),
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: SubtractDateAndTime);

        public static readonly AsyncFunctionPtr OperatorSubtractNumberAndDate = StandardErrorHandling<FormulaValue>(
            "-",
            expandArguments: NoArgExpansion,
            replaceBlankValues: DoNotReplaceBlank,
            checkRuntimeTypes: ExactSequence(
                ExactValueType<NumberValue>,
                DateOrDateTime),
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: SubtractNumberAndDate);

        public static readonly AsyncFunctionPtr OperatorSubtractNumberAndTime = StandardErrorHandling<FormulaValue>(
            "-",
            expandArguments: NoArgExpansion,
            replaceBlankValues: DoNotReplaceBlank,
            checkRuntimeTypes: ExactSequence(
                ExactValueType<NumberValue>,
                ExactValueType<TimeValue>),
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: SubtractNumberAndTime);

        public static readonly AsyncFunctionPtr OperatorLtDateTime = StandardErrorHandling<FormulaValue>(
            "<",
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

        public static readonly AsyncFunctionPtr OperatorLeqDateTime = StandardErrorHandling<FormulaValue>(
            "<=",
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

        public static readonly AsyncFunctionPtr OperatorGtDateTime = StandardErrorHandling<FormulaValue>(
            ">",
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

        public static readonly AsyncFunctionPtr OperatorGeqDateTime = StandardErrorHandling<FormulaValue>(
            ">=",
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

        public static readonly AsyncFunctionPtr OperatorLtDate = StandardErrorHandling<FormulaValue>(
            "<",
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

        public static readonly AsyncFunctionPtr OperatorLeqDate = StandardErrorHandling<FormulaValue>(
            "<=",
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

        public static readonly AsyncFunctionPtr OperatorGtDate = StandardErrorHandling<FormulaValue>(
            ">",
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

        public static readonly AsyncFunctionPtr OperatorGeqDate = StandardErrorHandling<FormulaValue>(
            ">=",
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

        public static readonly AsyncFunctionPtr OperatorLtTime = StandardErrorHandling<FormulaValue>(
            "<",
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

        public static readonly AsyncFunctionPtr OperatorLeqTime = StandardErrorHandling<FormulaValue>(
            "<=",
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

        public static readonly AsyncFunctionPtr OperatorGtTime = StandardErrorHandling<FormulaValue>(
            ">",
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

        public static readonly AsyncFunctionPtr OperatorGeqTime = StandardErrorHandling<FormulaValue>(
            ">=",
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

        private static FormulaValue NumericDiv(IRContext irContext, NumberValue[] args)
        {
            var dividend = args[0].Value;
            var divisor = args[1].Value;
            if (divisor == 0)
            {
                return CommonErrors.DivByZeroError(irContext);
            }

            return new NumberValue(irContext, dividend / divisor);
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
                            rhs = strRhs.ToLower();
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

        private static FormulaValue AddDateAndTime(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            return DateAdd(runner, context, irContext, args);
        }

        private static FormulaValue AddTimeAndNumber(IRContext irContext, FormulaValue[] args)
        {
            TimeSpan arg0;
            switch (args[0])
            {
                case DateTimeValue dtv:
                    arg0 = new TimeSpan(0, dtv.Value.Hour, dtv.Value.Minute, dtv.Value.Second, dtv.Value.Millisecond);
                    break;
                case TimeValue tv:
                    arg0 = tv.Value;
                    break;
                default:
                    return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            var arg1 = (NumberValue)args[1];

            try
            {
                var result = arg0.Add(TimeSpan.FromDays(arg1.Value));
                return new TimeValue(irContext, result);
            }
            catch
            {
                return CommonErrors.ArgumentOutOfRange(irContext);
            }
        }

        private static FormulaValue AddTimeAndTime(IRContext irContext, FormulaValue[] args)
        {
            TimeSpan arg0, arg1;
            switch (args[0])
            {
                case DateTimeValue dtv:
                    arg0 = new TimeSpan(0, dtv.Value.Hour, dtv.Value.Minute, dtv.Value.Second, dtv.Value.Millisecond);
                    break;
                case TimeValue tv:
                    arg0 = tv.Value;
                    break;
                default:
                    return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            switch (args[1])
            {
                case DateTimeValue dtv:
                    arg1 = new TimeSpan(0, dtv.Value.Hour, dtv.Value.Minute, dtv.Value.Second, dtv.Value.Millisecond);
                    break;
                case TimeValue tv:
                    arg1 = tv.Value;
                    break;
                default:
                    return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            try
            {
                var result = arg0.Add(arg1);
                return new TimeValue(irContext, result);
            }
            catch
            {
                return CommonErrors.ArgumentOutOfRange(irContext);
            }
        }

        private static FormulaValue AddDateAndDay(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            return DateAdd(runner, context, irContext, new FormulaValue[3] { args[0], args[1], StringValue.New("Days") });
        }

        private static FormulaValue AddDateTimeAndDay(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            return DateAdd(runner, context, irContext, new FormulaValue[3] { args[0], args[1], StringValue.New("Days") });
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
            return new NumberValue(irContext, result.TotalDays);
        }

        private static FormulaValue SubtractDateAndTime(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            return DateAdd(runner, context, irContext, new FormulaValue[2] { args[0], TimeValue.New(new TimeSpan(-((TimeValue)args[1]).Value.Ticks)) });            
        }

        private static FormulaValue SubtractNumberAndDate(IRContext irContext, FormulaValue[] args)
        {
            return OperatorNotSupportedError(irContext);
        }

        private static FormulaValue SubtractNumberAndTime(IRContext irContext, FormulaValue[] args)
        {
            return OperatorNotSupportedError(irContext);
        }

        public static ErrorValue OperatorNotSupportedError(IRContext irContext)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = $"The operator is invalid for these types",
                Span = irContext.SourceContext,
                Kind = ErrorKind.NotSupported
            });
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
