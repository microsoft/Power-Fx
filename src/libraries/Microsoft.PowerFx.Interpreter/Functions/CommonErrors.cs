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
        public static ErrorValue ArgumentOutOfRange(IRContext irContext)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                ResourceKey = RuntimeStringResources.ErrArgumentOutOfRange,
                Span = irContext.SourceContext,
                Kind = ErrorKind.InvalidArgument
            });
        }

        // Invalid Char value, must be in 1...255 range.
        public static ErrorValue InvalidCharValue(IRContext irContext)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                ResourceKey = RuntimeStringResources.ErrInvalidCharValue,
                Span = irContext.SourceContext,
                Kind = ErrorKind.InvalidArgument
            });
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
        public static ErrorValue BadLanguageCode(IRContext irContext, string languageCode)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                ResourceKey = RuntimeStringResources.ErrBadLanguageCode,
                Span = irContext.SourceContext,
                Kind = ErrorKind.BadLanguageCode,
                MessageArgs = new object[] { languageCode }
            });
        }

        public static ErrorValue InvalidDateTimeParsingError(IRContext irContext)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = "The Date/Time could not be parsed",
                Span = irContext.SourceContext,
                Kind = ErrorKind.InvalidArgument
            });
        }

        public static ErrorValue InvalidDateTimeError(IRContext irContext)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = "Invalid date/time value",
                Span = irContext.SourceContext,
                Kind = ErrorKind.InvalidArgument
            });
        }

        public static ErrorValue InvalidNumberFormatError(IRContext irContext)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = "The Number could not be parsed",
                Span = irContext.SourceContext,
                Kind = ErrorKind.InvalidArgument
            });
        }

        public static ErrorValue GenericInvalidArgument(IRContext irContext, string message = "Invalid Argument")
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = message,
                Span = irContext.SourceContext,
                Kind = ErrorKind.InvalidArgument
            });
        }

        public static ErrorValue InvalidBooleanFormatError(IRContext irContext)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = "The value could not be interpreted as a Boolean",
                Span = irContext.SourceContext,
                Kind = ErrorKind.InvalidArgument
            });
        }

        public static ErrorValue InvalidColorFormatError(IRContext irContext)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = "The value could not be interpreted as a Color",
                Span = irContext.SourceContext,
                Kind = ErrorKind.InvalidArgument
            });
        }

        public static ErrorValue InvalidGuidFormatError(IRContext irContext)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = "The value could not be interpreted as a GUID",
                Span = irContext.SourceContext,
                Kind = ErrorKind.InvalidArgument
            });
        }

        public static ErrorValue UnreachableCodeError(IRContext irContext)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = "Unknown error",
                Span = irContext.SourceContext,
                Kind = ErrorKind.Validation
            });
        }

        public static ErrorValue NotYetImplementedError(IRContext irContext, string message)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = $"Not implemented: {message}",
                Span = irContext.SourceContext,
                Kind = ErrorKind.NotSupported
            });
        }

        public static ErrorValue InvalidChain(IRContext irContext, string message)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = $"Invalid Chain: {message}",
                Span = irContext.SourceContext,
                Kind = ErrorKind.NotSupported
            });
        }

        internal static FormulaValue MaxCallDepth(IRContext irContext)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = "Max call depth exceeded",
                Span = irContext.SourceContext,
                Kind = ErrorKind.Internal
            });
        }

        internal static ExpressionError RecordNotFound()
        {
            return new ExpressionError()
            {
                Message = "The specified record was not found.",
                Kind = ErrorKind.NotFound
            };
        }

        internal static FormulaValue CustomError(IRContext irContext, string message)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = message,
                Span = irContext.SourceContext,
                Kind = ErrorKind.Custom
            });
        }

        internal static FormulaValue OnlyPrimitiveValuesAllowed(IRContext irContext)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = "Only primitive values are allowed for the operation",
                Span = irContext.SourceContext,
                Kind = ErrorKind.InvalidArgument
            });
        }

        internal static FormulaValue CanNotConvertToNumber(IRContext irContext, FormulaValue arg)
        {
            if (!arg.TryGetPrimitiveValue(out var primitveOrKind))
            {
                primitveOrKind = arg.Type._type.Kind.ToString();
            }

            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = $"The value '{primitveOrKind}' cannot be converted to a number.",
                Span = irContext.SourceContext,
                Kind = ErrorKind.InvalidArgument
            });
        }

        internal static ErrorValue StartOfWeekInvalid(IRContext irContext)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = "Expected a value from the StartOfWeek enumeration to indicate how to number the weekdays.",
                Span = irContext.SourceContext,
                Kind = ErrorKind.InvalidArgument
            });
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
