// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Interpreter.Localization;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Syntax.PrettyPrintVisitor;

namespace Microsoft.PowerFx.Functions
{
    internal static class CommonErrors
    {
        // Runtime type mismatch.
        public static ErrorValue RuntimeTypeMismatch(IRContext irContext, CultureInfo locale)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrRuntimeTypeMismatch, ErrorKind.InvalidArgument, irContext.SourceContext, locale));
        }

        // Argument out of range.
        public static ErrorValue ArgumentOutOfRange(IRContext irContext, CultureInfo locale)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrArgumentOutOfRange, ErrorKind.InvalidArgument, irContext.SourceContext, locale));
        }

        // Invalid numerical value, must be in 1...255 range.
        public static ErrorValue InvalidCharValue(IRContext irContext, CultureInfo locale)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrInvalidCharValue, ErrorKind.InvalidArgument, irContext.SourceContext, locale));
        }

        // Invalid operation: division by zero
        public static ErrorValue DivByZeroError(IRContext irContext, CultureInfo locale)
        {
            return new ErrorValue(
                irContext, 
                NewExpressionError(RuntimeStringResources.ErrDivByZero, ErrorKind.Div0, irContext.SourceContext, locale));
        }

        public static ErrorValue OverflowError(IRContext irContext)
        {
            return PrimitiveValueConversions.OverflowError(irContext);
        }

        // Language code '{0}' not supported.
        public static ErrorValue BadLanguageCode(IRContext irContext, string languageCode, CultureInfo locale)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrBadLanguageCode, ErrorKind.BadLanguageCode, irContext.SourceContext, locale, languageCode));
        }

        // The Date/Time could not be parsed.
        public static ErrorValue InvalidDateTimeParsingError(IRContext irContext, CultureInfo locale)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrInvalidDateTimeParsingError, ErrorKind.InvalidArgument, irContext.SourceContext, locale));
        }

        // Invalid date/time value.
        public static ErrorValue InvalidDateTimeError(IRContext irContext, CultureInfo locale)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrInvalidDateTimeError, ErrorKind.InvalidArgument, irContext.SourceContext, locale));
        }

        public static ErrorValue InvalidArgumentError(IRContext irContext, ErrorResourceKey errorResourceKey, CultureInfo locale)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(errorResourceKey, ErrorKind.InvalidArgument, irContext.SourceContext, locale));
        }

        // The value could not be interpreted as a Boolean.
        public static ErrorValue InvalidBooleanFormatError(IRContext irContext, CultureInfo locale)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrInvalidBooleanFormatError, ErrorKind.InvalidArgument, irContext.SourceContext, locale));
        }

        // The value could not be interpreted as a Color.
        public static ErrorValue InvalidColorFormatError(IRContext irContext, CultureInfo locale)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrInvalidColorFormatError, ErrorKind.InvalidArgument, irContext.SourceContext, locale));
        }

        // Unknown error.
        public static ErrorValue UnreachableCodeError(IRContext irContext, CultureInfo locale)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrInvalidColorFormatError, ErrorKind.Validation, irContext.SourceContext, locale));
        }

        // Not yet implemented function: {0}.
        public static ErrorValue NotYetImplementedFunctionError(IRContext irContext, CultureInfo locale, string functionName)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrNotYetImplementedFunctionError, ErrorKind.NotSupported, irContext.SourceContext, locale, functionName));
        }

        // Not yet implemented unary operator: {0}.
        public static ErrorValue NotYetImplementedUnaryOperatorError(IRContext irContext, CultureInfo locale, string unaryOperatorName)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrNotYetImplementedUnaryOperatorError, ErrorKind.NotSupported, irContext.SourceContext, locale, unaryOperatorName));
        }

        // Invalid Chain: {0}.
        public static ErrorValue InvalidChain(IRContext irContext, CultureInfo locale, string message)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrInvalidChain, ErrorKind.NotSupported, irContext.SourceContext, locale, message));
        }

        // Max call depth exceeded.
        internal static ErrorValue MaxCallDepth(IRContext irContext, CultureInfo locale)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrMaxCallDepth, ErrorKind.Validation, irContext.SourceContext, locale));
        }

        // The specified record was not found.
        internal static ExpressionError RecordNotFound(CultureInfo locale)
        {
            return NewExpressionError(RuntimeStringResources.ErrRecordNotFound, ErrorKind.NotFound, null, locale);
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
        internal static ErrorValue RuntimeExceptionError(IRContext irContext, CultureInfo locale, string message)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrRuntimeExceptionError, ErrorKind.Internal, irContext.SourceContext, locale, message));
        }

        // Only primitive values are allowed for the operation.
        internal static ErrorValue OnlyPrimitiveValuesAllowed(IRContext irContext, CultureInfo locale)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrOnlyPrimitiveValuesAllowed, ErrorKind.InvalidArgument, irContext.SourceContext, locale));
        }

        // The value '{0}' cannot be converted to a number.
        internal static ErrorValue CanNotConvertToNumber(IRContext irContext, FormulaValue arg, CultureInfo locale)
        {
            if (!arg.TryGetPrimitiveValue(out var primitveOrKind))
            {
                primitveOrKind = arg.Type._type.Kind.ToString();
            }

            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrCanNotConvertToNumber, ErrorKind.InvalidArgument, irContext.SourceContext, locale, primitveOrKind.ToString()));
        }

        // Expected a value from the StartOfWeek enumeration to indicate how to number the weekdays.
        internal static ErrorValue StartOfWeekInvalid(IRContext irContext, CultureInfo locale)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrStartOfWeekInvalid, ErrorKind.InvalidArgument, irContext.SourceContext, locale));
        }

        // Class '{0}' does not implement 'SetProperty'.
        internal static ErrorValue UntypedObjectDoesNotImplementSetPropertyError(IRContext irContext, CultureInfo locale, string className)
        {
            return new ErrorValue(
                irContext,
                NewExpressionError(RuntimeStringResources.ErrUntypedObjectDoesNotImplementSetProperty, ErrorKind.NotSupported, irContext.SourceContext, locale, className));
        }

        private static ExpressionError NewExpressionError(ErrorResourceKey errorKey, ErrorKind kind, Span span, CultureInfo locale, params string[] args)
        {
            return new ExpressionError(locale)
            {
                ResourceKey = errorKey,
                Span = span,
                Kind = kind,
                MessageArgs = args
            };
        }
    }
}
