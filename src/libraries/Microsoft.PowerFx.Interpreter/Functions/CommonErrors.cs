// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    internal static class CommonErrors
    {
        public static ErrorValue RuntimeTypeMismatch(IRContext irContext)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = "Runtime type mismatch",
                Span = irContext.SourceContext,
                Kind = ErrorKind.InvalidArgument
            });
        }

        public static ErrorValue ArgumentOutOfRange(IRContext irContext)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = "Argument out of range",
                Span = irContext.SourceContext,
                Kind = ErrorKind.InvalidArgument
            });
        }

        public static ErrorValue InvalidCharValue(IRContext irContext)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = "Invalid Char value, must be in 1...255 range",
                Span = irContext.SourceContext,
                Kind = ErrorKind.InvalidArgument
            });
        }

        public static ErrorValue DivByZeroError(IRContext irContext)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = "Invalid operation: division by zero.",
                Span = irContext.SourceContext,
                Kind = ErrorKind.Div0
            });
        }

        public static ErrorValue OverflowError(IRContext irContext)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = "Overflow",
                Span = irContext.SourceContext,
                Kind = ErrorKind.Numeric
            });
        }

        public static ErrorValue BadLanguageCode(IRContext irContext, string languageCode)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = $"Language code {languageCode} not supported",
                Span = irContext.SourceContext,
                Kind = ErrorKind.BadLanguageCode
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
    }
}
