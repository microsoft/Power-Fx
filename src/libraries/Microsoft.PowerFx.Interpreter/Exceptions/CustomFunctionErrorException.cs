// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter
{
    public class CustomFunctionErrorException : Exception
    {
        public readonly ErrorKind ErrorKind;

        public CustomFunctionErrorException(string message)
            : base(message) 
        {
            ErrorKind = ErrorKind.None;
        }

        public CustomFunctionErrorException(string message, ErrorKind kind)
            : this(message)
        {
            ErrorKind = kind;
        }
    }
}
