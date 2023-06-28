// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Runtime.Serialization;

namespace Microsoft.PowerFx.Connectors
{
    [Serializable]
    public class PowerFxConnectorException : Exception
    {
        public PowerFxConnectorException()
        {
        }

        public PowerFxConnectorException(string message) 
            : base(message)
        {
        }

        public PowerFxConnectorException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        protected PowerFxConnectorException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}
