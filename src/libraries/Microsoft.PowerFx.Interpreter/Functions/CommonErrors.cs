// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Interpreter.Localization;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    internal static class CommonErrors
    {
        // Runtime type mismatch.
        public static ErrorValue RuntimeTypeMismatch(IRContext irContext)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrRuntimeTypeMismatch, ErrorKind.InvalidArgument, irContext.SourceContext));
        }

        // Argument out of range.
        public static ErrorValue ArgumentOutOfRange(IRContext irContext)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrArgumentOutOfRange, ErrorKind.InvalidArgument, irContext.SourceContext));
        }

        // Invalid numerical value, must be in 1...255 range.
        public static ErrorValue InvalidCharValue(IRContext irContext)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrInvalidCharValue, ErrorKind.InvalidArgument, irContext.SourceContext));
        }

        // Invalid operation: division by zero
        public static ErrorValue DivByZeroError(IRContext irContext)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrDivByZero, ErrorKind.Div0, irContext.SourceContext));
        }

        public static ErrorValue OverflowError(IRContext irContext)
        {
            return PrimitiveValueConversions.OverflowError(irContext);
        }

        // Language code '{0}' not supported.
        public static ErrorValue BadLanguageCode(IRContext irContext, string languageCode)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrBadLanguageCode, ErrorKind.BadLanguageCode, irContext.SourceContext, languageCode));
        }

        // The Date/Time could not be parsed.
        public static ErrorValue InvalidDateTimeParsingError(IRContext irContext)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrInvalidDateTimeParsingError, ErrorKind.InvalidArgument, irContext.SourceContext));
        }

        // Invalid date/time value.
        public static ErrorValue InvalidDateTimeError(IRContext irContext)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrInvalidDateTimeError, ErrorKind.InvalidArgument, irContext.SourceContext));
        }

        public static ErrorValue InvalidArgumentError(IRContext irContext, ErrorResourceKey errorResourceKey)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(errorResourceKey, ErrorKind.InvalidArgument, irContext.SourceContext));
        }

        // The value could not be interpreted as a Boolean.
        public static ErrorValue InvalidBooleanFormatError(IRContext irContext)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrInvalidBooleanFormatError, ErrorKind.InvalidArgument, irContext.SourceContext));
        }

        // The value could not be interpreted as a Color.
        public static ErrorValue InvalidColorFormatError(IRContext irContext)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrInvalidColorFormatError, ErrorKind.InvalidArgument, irContext.SourceContext));
        }

        // Unknown error.
        public static ErrorValue UnreachableCodeError(IRContext irContext)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrInvalidColorFormatError, ErrorKind.Internal, irContext.SourceContext));
        }

        // Not yet implemented function: {0}.
        public static ErrorValue NotYetImplementedFunctionError(IRContext irContext, string functionName)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrNotYetImplementedFunctionError, ErrorKind.NotSupported, irContext.SourceContext, functionName));
        }

        // Not yet implemented unary operator: {0}.
        public static ErrorValue NotYetImplementedUnaryOperatorError(IRContext irContext, string unaryOperatorName)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrNotYetImplementedUnaryOperatorError, ErrorKind.NotSupported, irContext.SourceContext, unaryOperatorName));
        }

        // Invalid Chain: {0}.
        public static ErrorValue InvalidChain(IRContext irContext, string message)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrInvalidChain, ErrorKind.NotSupported, irContext.SourceContext, message));
        }

        // Max call depth exceeded.
        internal static ErrorValue MaxCallDepth(IRContext irContext)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrMaxCallDepth, ErrorKind.Internal, irContext.SourceContext));
        }

        // The specified record was not found.
        internal static ExpressionError RecordNotFound()
        {
            return NewExpressionError(RuntimeStringResources.ErrRecordNotFound, ErrorKind.NotFound, null);
        }

        // Custom errors are not localized.
        internal static ErrorValue CustomError(IRContext irContext, string message)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = message,
                Span = irContext.SourceContext,
                Kind = ErrorKind.Custom
            });
        }

        // An exception has been thrown: {0}
        internal static ErrorValue RuntimeExceptionError(IRContext irContext, string message)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrRuntimeExceptionError, ErrorKind.Internal, irContext.SourceContext, message));
        }

        // Only primitive values are allowed for the operation.
        internal static ErrorValue OnlyPrimitiveValuesAllowed(IRContext irContext)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrOnlyPrimitiveValuesAllowed, ErrorKind.InvalidArgument, irContext.SourceContext));
        }

        // The value '{0}' cannot be converted to a number.
        internal static ErrorValue CanNotConvertToNumber(IRContext irContext, FormulaValue arg)
        {
            if (!arg.TryGetPrimitiveValue(out var primitveOrKind))
            {
                primitveOrKind = arg.Type._type.Kind.ToString();
            }

            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrCanNotConvertToNumber, ErrorKind.InvalidArgument, irContext.SourceContext, primitveOrKind.ToString()));
        }

        // Expected a value from the StartOfWeek enumeration to indicate how to number the weekdays.
        internal static ErrorValue StartOfWeekInvalid(IRContext irContext)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrStartOfWeekInvalid, ErrorKind.InvalidArgument, irContext.SourceContext));
        }

        // Class '{0}' does not implement 'SetProperty'.
        internal static ErrorValue UntypedObjectDoesNotImplementSetPropertyError(IRContext irContext, string className)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrUntypedObjectDoesNotImplementSetProperty, ErrorKind.NotSupported, irContext.SourceContext, className));
        }

        // Lower value cannot be greater than upper value.
        internal static ErrorValue LowerValueGreaterThanUpperValue(IRContext irContext)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrLowerValueGreaterThanUpperValue, ErrorKind.Numeric, irContext.SourceContext));
        }

        private static ExpressionError NewExpressionError(ErrorResourceKey errorKey, ErrorKind kind, Span span, params string[] args)
        {
            return new ExpressionError()
            {
                ResourceKey = errorKey,
                Span = span,
                Kind = kind,
                MessageArgs = args
            };
        }
    }
}
