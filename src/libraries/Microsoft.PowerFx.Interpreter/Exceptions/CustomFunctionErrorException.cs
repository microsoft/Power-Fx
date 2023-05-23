// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter
{
    /// <summary>
    /// Exception thrown from hosting callbacks which will get converted into an <see cref="ExpressionError"/> and become a Power Fx error value. 
    /// </summary>
    public class CustomFunctionErrorException : Exception
    {
        public ErrorKind ErrorKind => ExpressionError.Kind;

        public readonly ExpressionError ExpressionError;

        public CustomFunctionErrorException(ExpressionError expressionError)
            : base(expressionError.Message)
        {
            this.ExpressionError = expressionError;
        }

        public CustomFunctionErrorException(string message)
            : this(new ExpressionError() { Message = message, Kind = ErrorKind.None }) 
        {
        }

        public CustomFunctionErrorException(string message, ErrorKind kind)
            : this(new ExpressionError() { Message = message, Kind = kind })
        {
        }
    }
}
