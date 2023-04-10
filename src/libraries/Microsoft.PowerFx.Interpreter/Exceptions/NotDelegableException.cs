// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx.Interpreter
{   
    /// <summary>
    /// Used to fail attempted delegation and fall back to in memory evaluation.
    /// Should always be caught by PowerFx code.
    /// </summary>
    public sealed class NotDelegableException : Exception
    {
        public NotDelegableException()
            : base()
        {
        }

        public NotDelegableException(string message)
            : base(message)
        {
        }

        public NotDelegableException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
