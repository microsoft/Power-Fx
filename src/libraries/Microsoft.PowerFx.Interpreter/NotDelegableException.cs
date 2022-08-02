// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx.Interpreter
{
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
