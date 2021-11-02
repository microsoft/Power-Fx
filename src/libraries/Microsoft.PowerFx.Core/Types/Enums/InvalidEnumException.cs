// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.PowerFx.Core.Types.Enums
{
    internal class InvalidEnumException : Exception
    {
        public InvalidEnumException(string message) : base(message)
        {
        }
    }
}