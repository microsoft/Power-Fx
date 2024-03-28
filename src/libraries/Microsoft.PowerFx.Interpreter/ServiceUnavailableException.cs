// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx.Interpreter
{
    /// <summary>
    /// Exception thrown from hosting callbacks which will get converted into an <see cref="ExpressionError"/> and become a Power Fx error value. 
    /// </summary>
    public class ServiceUnavailableException : Exception
    {
        public ErrorKind ErrorKind => ErrorKind.ServiceUnavailable;

        public readonly ExpressionError ExpressionError;

        public ServiceUnavailableException(ExpressionError expressionError)
            : base(expressionError.Message)
        {
            this.ExpressionError = expressionError;
        }

        public ServiceUnavailableException(string message)
            : this(new ExpressionError() { Message = message, Kind = ErrorKind.ServiceUnavailable })
        {
        }
    }
}
