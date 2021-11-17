// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Public.Values;

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
                Kind = ErrorKind.Validation
            });
        }

        public static ErrorValue ArgumentOutOfRange(IRContext irContext)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = "Argument out of range",
                Span = irContext.SourceContext,
                Kind = ErrorKind.Numeric
            });
        }

        public static ErrorValue DivByZeroError(IRContext irContext)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = "Divide by zero",
                Span = irContext.SourceContext,
                Kind = ErrorKind.Div0
            });
        }

        public static ErrorValue InvalidDateTimeError(IRContext irContext)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = "The Date/Time could not be parsed",
                Span = irContext.SourceContext,
                Kind = ErrorKind.BadLanguageCode
            });
        }

        public static ErrorValue InvalidNumberFormatError(IRContext irContext)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = "The Number could not be parsed",
                Span = irContext.SourceContext,
                Kind = ErrorKind.BadLanguageCode
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
    }
}
