// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Runtime.Serialization;

namespace Microsoft.PowerFx.Core.Tests
{
    [Serializable]
    public class SetupHandlerNotFoundException : Exception
    {
        public SetupHandlerNotFoundException()
        {
        }

        public SetupHandlerNotFoundException(string message)
            : base(message)
        {
        }

        public SetupHandlerNotFoundException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        protected SetupHandlerNotFoundException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}
