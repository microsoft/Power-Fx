// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx.Core.Types.Enums
{
    public class InvalidEnumException : Exception
    {
        public InvalidEnumException(string message)
            : base(message)
        {
        }

        public InvalidEnumException()
        {
        }

        public InvalidEnumException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}