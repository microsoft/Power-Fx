// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Net;
using System.Runtime.Serialization;

namespace Microsoft.PowerFx
{
    [Serializable]
    internal class PowerFxJsonException : Exception
    {
        public PowerFxJsonException()
        {
        }

        public PowerFxJsonException(string message)
            : base(message)
        {
        }

        public PowerFxJsonException(string message, string name)
            : base($"{message}{(string.IsNullOrEmpty(name) ? string.Empty : $", in {name}")}")
        {
        }

        public PowerFxJsonException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected PowerFxJsonException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
