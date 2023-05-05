// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Interpreter
{
    [Serializable]
    internal class RuntimeMaxCallDepthException : Exception
    {
        public RuntimeMaxCallDepthException()
            : base()
        {
        }

        public RuntimeMaxCallDepthException(string message)
            : base(message)
        { 
        }

        public RuntimeMaxCallDepthException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected RuntimeMaxCallDepthException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context) 
        { 
        }
    }
}
