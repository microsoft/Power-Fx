// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Interpreter
{
    public class CustomFunctionErrorException : Exception
    {
        public CustomFunctionErrorException(string message)
            : base(message) 
        { 
        }
    }
}
