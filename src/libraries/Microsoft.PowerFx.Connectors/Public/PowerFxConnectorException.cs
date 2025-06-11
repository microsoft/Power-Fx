// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Net;
using System.Runtime.Serialization;

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Represents errors that occur during Power Fx connector operations.
    /// </summary>
    [Serializable]
    public class PowerFxConnectorException : Exception
    {
        /// <summary>
        /// Gets the status code associated with the exception.
        /// </summary>
        public int StatusCode { get; init; } = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerFxConnectorException"/> class.
        /// </summary>
        public PowerFxConnectorException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerFxConnectorException"/> class with a specified error message.
        /// </summary>
        public PowerFxConnectorException(string message) 
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerFxConnectorException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        public PowerFxConnectorException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerFxConnectorException"/> class with serialized data.
        /// </summary>
        protected PowerFxConnectorException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}
