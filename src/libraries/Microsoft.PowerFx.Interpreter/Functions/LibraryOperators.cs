﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
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

        public static readonly AsyncFunctionPtr OperatorDecimalBinaryAdd = StandardErrorHandling<DecimalValue>(
            "+",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithZeroDecimal,
            checkRuntimeTypes: ExactValueType<DecimalValue>,
            checkRuntimeValues: DeferRuntimeTypeChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: DecimalAdd);

        public static readonly AsyncFunctionPtr OperatorBinaryMul = StandardErrorHandling<NumberValue>(
            "*",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithZero,
            checkRuntimeTypes: ExactValueType<NumberValue>,
            checkRuntimeValues: DeferRuntimeTypeChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: NumericMul);

        public static readonly AsyncFunctionPtr OperatorDecimalBinaryMul = StandardErrorHandling<DecimalValue>(
            "*",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithZeroDecimal,
            checkRuntimeTypes: ExactValueType<DecimalValue>,
            checkRuntimeValues: DeferRuntimeTypeChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: DecimalMul);

        public static readonly AsyncFunctionPtr OperatorBinaryDiv = StandardErrorHandling<NumberValue>(
            "/",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithZero,
            checkRuntimeTypes: ExactValueType<NumberValue>,
            checkRuntimeValues: DeferRuntimeTypeChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: NumericDiv);

        public static readonly AsyncFunctionPtr OperatorDecimalBinaryDiv = StandardErrorHandling<DecimalValue>(
            "/",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithZeroDecimal,
            checkRuntimeTypes: ExactValueType<DecimalValue>,
            checkRuntimeValues: DeferRuntimeTypeChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: DecimalDiv);

        public static readonly AsyncFunctionPtr OperatorBinaryGt = StandardErrorHandling<NumberValue>(
            ">",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithZero,
            checkRuntimeTypes: ExactValueType<NumberValue>,
            checkRuntimeValues: DeferRuntimeTypeChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: NumericGt);

        public static readonly AsyncFunctionPtr OperatorDecimalBinaryGt = StandardErrorHandling<DecimalValue>(
            ">",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithZeroDecimal,
            checkRuntimeTypes: ExactValueType<DecimalValue>,
            checkRuntimeValues: DeferRuntimeTypeChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: DecimalGt);

        public static readonly AsyncFunctionPtr OperatorBinaryGeq = StandardErrorHandling<NumberValue>(
            ">=",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithZero,
            checkRuntimeTypes: ExactValueType<NumberValue>,
            checkRuntimeValues: DeferRuntimeTypeChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: NumericGeq);

        public static readonly AsyncFunctionPtr OperatorDecimalBinaryGeq = StandardErrorHandling<DecimalValue>(
            ">=",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithZeroDecimal,
            checkRuntimeTypes: ExactValueType<DecimalValue>,
            checkRuntimeValues: DeferRuntimeTypeChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: DecimalGeq);

        public static readonly AsyncFunctionPtr OperatorBinaryLt = StandardErrorHandling<NumberValue>(
            "<",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithZero,
            checkRuntimeTypes: ExactValueType<NumberValue>,
            checkRuntimeValues: DeferRuntimeTypeChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: NumericLt);

        public static readonly AsyncFunctionPtr OperatorDecimalBinaryLt = StandardErrorHandling<DecimalValue>(
            "<",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithZeroDecimal,
            checkRuntimeTypes: ExactValueType<DecimalValue>,
            checkRuntimeValues: DeferRuntimeTypeChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: DecimalLt);

        public static readonly AsyncFunctionPtr OperatorBinaryLeq = StandardErrorHandling<NumberValue>(
            "<=",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithZero,
            checkRuntimeTypes: ExactValueType<NumberValue>,
            checkRuntimeValues: DeferRuntimeTypeChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: NumericLeq);

        public static readonly AsyncFunctionPtr OperatorDecimalBinaryLeq = StandardErrorHandling<DecimalValue>(
            "<=",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWithZeroDecimal,
            checkRuntimeTypes: ExactValueType<DecimalValue>,
            checkRuntimeValues: DeferRuntimeTypeChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: DecimalLeq);

        public static readonly AsyncFunctionPtr OperatorBinaryEq = StandardErrorHandling<FormulaValue>(
            "=",
            expandArguments: NoArgExpansion,
            replaceBlankValues: DoNotReplaceBlank,
            checkRuntimeTypes: DeferRuntimeTypeChecking,
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: AreEqual);

        public static readonly AsyncFunctionPtr OperatorBinaryEqNullUntyped = StandardErrorHandling<FormulaValue>(
            "=",
            expandArguments: NoArgExpansion,
            replaceBlankValues: DoNotReplaceBlank,
            checkRuntimeTypes: DeferRuntimeTypeChecking,
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: AreEqualNullUntyped);

        public static readonly AsyncFunctionPtr OperatorBinaryEqPolymorphic = StandardErrorHandling<FormulaValue>(
            "=",
            expandArguments: NoArgExpansion,
            replaceBlankValues: DoNotReplaceBlank,
            checkRuntimeTypes: ExactValueTypeOrBlank<RecordValue>,
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: AreEqualPolymorphic);

        public static readonly AsyncFunctionPtr OperatorBinaryNeq = StandardErrorHandling<FormulaValue>(
            "<>",
            expandArguments: NoArgExpansion,
            replaceBlankValues: DoNotReplaceBlank,
            checkRuntimeTypes: DeferRuntimeTypeChecking,
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: NotEqual);

        public static readonly AsyncFunctionPtr OperatorBinaryNeqNullUntyped = StandardErrorHandling<FormulaValue>(
            "<>",
            expandArguments: NoArgExpansion,
            replaceBlankValues: DoNotReplaceBlank,
            checkRuntimeTypes: DeferRuntimeTypeChecking,
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: NotEqualNullUntyped);

        public static readonly AsyncFunctionPtr OperatorBinaryNeqPolymorphic = StandardErrorHandling<FormulaValue>(
            "<>",
            expandArguments: NoArgExpansion,
            replaceBlankValues: DoNotReplaceBlank,
            checkRuntimeTypes: ExactValueTypeOrBlank<RecordValue>,
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: NotEqualPolymorphic);

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
            replaceBlankValues: ReplaceBlankWith(
                new NumberValue(IRContext.NotInSource(FormulaType.Number), 0),
                new DateValue(IRContext.NotInSource(FormulaType.Date), _epoch)),
            checkRuntimeTypes: ExactSequence(
                ExactValueType<NumberValue>,
                DateOrDateTime),
            checkRuntimeValues: DeferRuntimeValueChecking,
            returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
            targetFunction: SubtractNumberAndDate);

        public static readonly AsyncFunctionPtr OperatorSubtractNumberAndTime = StandardErrorHandling<FormulaValue>(
            "-",
            expandArguments: NoArgExpansion,
            replaceBlankValues: ReplaceBlankWith(
                new NumberValue(IRContext.NotInSource(FormulaType.Number), 0),
                new TimeValue(IRContext.NotInSource(FormulaType.Time), TimeSpan.Zero)),
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

        private static FormulaValue DecimalAdd(IRContext irContext, DecimalValue[] args)
        {
            decimal result;

            try
            {
                result = args[0].Value + args[1].Value;
            }
            catch (OverflowException)
            {
                return CommonErrors.OverflowError(irContext);
            }

            return new DecimalValue(irContext, result);
        }

        private static FormulaValue DecimalMul(IRContext irContext, DecimalValue[] args)
        {
            decimal result;

            try
            {
                result = args[0].Value * args[1].Value;
            }
            catch (OverflowException)
            {
                return CommonErrors.OverflowError(irContext);
            }

            return new DecimalValue(irContext, result);
        }

        private static FormulaValue DecimalDiv(IRContext irContext, DecimalValue[] args)
        {
            var dividend = args[0].Value;
            var divisor = args[1].Value;
            decimal result;

            if (divisor == 0m)
            {
                return CommonErrors.DivByZeroError(irContext);
            }

            try
            {
                result = dividend / divisor;
            }
            catch (OverflowException)
            {
                return CommonErrors.OverflowError(irContext);
            }

            return new DecimalValue(irContext, result);
        }

        private static BooleanValue DecimalGt(IRContext irContext, DecimalValue[] args)
        {
            var result = args[0].Value > args[1].Value;
            return new BooleanValue(irContext, result);
        }

        private static BooleanValue DecimalGeq(IRContext irContext, DecimalValue[] args)
        {
            var result = args[0].Value >= args[1].Value;
            return new BooleanValue(irContext, result);
        }

        private static BooleanValue DecimalLt(IRContext irContext, DecimalValue[] args)
        {
            var result = args[0].Value < args[1].Value;
            return new BooleanValue(irContext, result);
        }

        private static BooleanValue DecimalLeq(IRContext irContext, DecimalValue[] args)
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

        private static BooleanValue AreEqualNullUntyped(IRContext irContext, FormulaValue[] args)
        {
            var arg1 = args[0];
            var arg2 = args[1];

            if (arg1 is BlankValue && arg2 is BlankValue)
            {
                return new BooleanValue(irContext, true);
            }

            Contracts.Assert(arg1 is UntypedObjectValue ^ arg2 is UntypedObjectValue, "UO = UO is undefined");

            if (arg1 is UntypedObjectValue uo1)
            {
                return new BooleanValue(irContext, uo1.Impl.Type == FormulaType.Blank);
            }

            var uo2 = (UntypedObjectValue)arg2;
            return new BooleanValue(irContext, uo2.Impl.Type == FormulaType.Blank);
        }

        private static BooleanValue AreEqualPolymorphic(IRContext irContext, FormulaValue[] args)
        {
            var result = false;
            if (args[0] is RecordValue arg1 && 
                arg1.TryGetPrimaryKey(out var arg1Key) && 
                args[1] is RecordValue arg2 &&
                arg2.TryGetPrimaryKey(out var arg2Key))
            {
                result = arg1Key.Equals(arg2Key, StringComparison.Ordinal);
            }

            return new BooleanValue(irContext, result); 
        }

        private static BooleanValue NotEqual(IRContext irContext, FormulaValue[] args)
        {
            var arg1 = args[0];
            var arg2 = args[1];
            return new BooleanValue(irContext, !RuntimeHelpers.AreEqual(arg1, arg2));
        }

        private static BooleanValue NotEqualNullUntyped(IRContext irContext, FormulaValue[] args)
        {
            var arg1 = args[0];
            var arg2 = args[1];

            if (arg1 is BlankValue && arg2 is BlankValue)
            {
                return new BooleanValue(irContext, false);
            }

            Contracts.Assert(arg1 is UntypedObjectValue ^ arg2 is UntypedObjectValue, "UO <> UO is undefined");

            if (arg1 is UntypedObjectValue uo1)
            {
                return new BooleanValue(irContext, uo1.Impl.Type != FormulaType.Blank);
            }

            var uo2 = (UntypedObjectValue)arg2;
            return new BooleanValue(irContext, uo2.Impl.Type != FormulaType.Blank);
        }

        private static BooleanValue NotEqualPolymorphic(IRContext irContext, FormulaValue[] args)
        {
            return new BooleanValue(irContext, !AreEqualPolymorphic(irContext, args).Value);
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

                return new BooleanValue(irContext, rightStr.Value.IndexOf(leftStr.Value, exact ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase) >= 0);
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

        private static FormulaValue AddTimeAndNumber(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var timeZoneInfo = runner.TimeZoneInfo;
            TimeSpan arg0 = runner.GetNormalizedTimeSpanWithoutDay(args[0]);

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

        private static FormulaValue AddTimeAndTime(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var timeZoneInfo = runner.TimeZoneInfo;
            TimeSpan arg0, arg1;
            arg0 = runner.GetNormalizedTimeSpanWithoutDay(args[0]);

            arg1 = runner.GetNormalizedTimeSpanWithoutDay(args[1]);

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

        private static FormulaValue DateDifference(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var timeZoneInfo = runner.TimeZoneInfo;
            DateTime arg0 = runner.GetNormalizedDateTime(args[0]);

            DateTime arg1 = runner.GetNormalizedDateTime(args[1]);

            var result = arg0.Subtract(arg1);
            if (irContext.ResultType == FormulaType.Decimal)
            {
                return new DecimalValue(irContext, (decimal)result.TotalDays);
            }
            else
            {
                return new NumberValue(irContext, result.TotalDays);
            }
        }

        private static FormulaValue TimeDifference(IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (TimeValue)args[0];
            var arg1 = (TimeValue)args[1];

            var result = arg0.Value.Subtract(arg1.Value);
            if (irContext.ResultType == FormulaType.Decimal)
            {
                return new DecimalValue(irContext, (decimal)result.TotalDays);
            }
            else
            {
                return new NumberValue(irContext, result.TotalDays);
            }
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

        private static FormulaValue LtDateTime(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var timeZoneInfo = runner.TimeZoneInfo;
            DateTime arg0 = runner.GetNormalizedDateTime(args[0]);
            
            DateTime arg1 = runner.GetNormalizedDateTime(args[1]);

            var result = arg0 < arg1;
            return new BooleanValue(irContext, result);
        }

        private static FormulaValue LeqDateTime(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var timeZoneInfo = runner.TimeZoneInfo;
            DateTime arg0 = runner.GetNormalizedDateTime(args[0]);

            DateTime arg1 = runner.GetNormalizedDateTime(args[1]);

            var result = arg0 <= arg1;
            return new BooleanValue(irContext, result);
        }

        private static FormulaValue GtDateTime(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var timeZoneInfo = runner.TimeZoneInfo;
            DateTime arg0 = runner.GetNormalizedDateTime(args[0]);

            DateTime arg1 = runner.GetNormalizedDateTime(args[1]);

            var result = arg0 > arg1;
            return new BooleanValue(irContext, result);
        }

        private static FormulaValue GeqDateTime(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var timeZoneInfo = runner.TimeZoneInfo;
            DateTime arg0 = runner.GetNormalizedDateTime(args[0]);

            DateTime arg1 = runner.GetNormalizedDateTime(args[1]);

            var result = arg0 >= arg1;
            return new BooleanValue(irContext, result);
        }

        private static FormulaValue LtDate(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var timeZoneInfo = runner.TimeZoneInfo;
            DateTime arg0 = runner.GetNormalizedDateTime(args[0]);

            DateTime arg1 = runner.GetNormalizedDateTime(args[1]);

            var result = arg0 < arg1;
            return new BooleanValue(irContext, result);
        }

        private static FormulaValue LeqDate(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var timeZoneInfo = runner.TimeZoneInfo;
            DateTime arg0 = runner.GetNormalizedDateTime(args[0]);

            DateTime arg1 = runner.GetNormalizedDateTime(args[1]);

            var result = arg0 <= arg1;
            return new BooleanValue(irContext, result);
        }

        private static FormulaValue GtDate(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var timeZoneInfo = runner.TimeZoneInfo;
            DateTime arg0 = runner.GetNormalizedDateTime(args[0]);

            DateTime arg1 = runner.GetNormalizedDateTime(args[1]);

            var result = arg0 > arg1;
            return new BooleanValue(irContext, result);
        }

        private static FormulaValue GeqDate(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var timeZoneInfo = runner.TimeZoneInfo;
            DateTime arg0 = runner.GetNormalizedDateTime(args[0]);

            DateTime arg1 = runner.GetNormalizedDateTime(args[1]);

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
