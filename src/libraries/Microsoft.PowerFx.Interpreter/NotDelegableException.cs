// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx.Interpreter
{
    // Used to fail attempted delegation and fall back to in memory evaluation
    // Should always be caught by PowerFx code
    internal sealed class NotDelegableException : Exception
    {
        internal NotDelegableException()
            : base()
        {
        }

        internal NotDelegableException(string message)
            : base(message)
        {
        }

        internal NotDelegableException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
