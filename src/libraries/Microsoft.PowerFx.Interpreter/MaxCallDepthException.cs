// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Interpreter
{
    [Serializable]
    internal class MaxCallDepthException : Exception
    {
        public MaxCallDepthException()
            : base()
        {
        }

        public MaxCallDepthException(string message)
            : base(message)
        {
        }

        public MaxCallDepthException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected MaxCallDepthException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}
