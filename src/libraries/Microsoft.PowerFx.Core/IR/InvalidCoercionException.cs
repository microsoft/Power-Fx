﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx.Core.IR
{
    internal class InvalidCoercionException : InvalidOperationException
    {
        public InvalidCoercionException(string message)
            : base(message)
        {
        }
    }
}
