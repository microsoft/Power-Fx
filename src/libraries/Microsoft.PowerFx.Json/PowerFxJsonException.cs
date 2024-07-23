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

        public PowerFxJsonException(string message, string path)
            : base($"{message}{(string.IsNullOrEmpty(path) ? string.Empty : $", in {path}")}")
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
