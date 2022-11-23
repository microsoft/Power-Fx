// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// The interpreter is called in an illegal way. 
    /// </summary>
    [Serializable]
    public class InterpreterConfigException : InvalidOperationException
    {
        public InterpreterConfigException()
            : base()
        {
        }

        public InterpreterConfigException(string message)
            : base(message)
        {
        }

        public InterpreterConfigException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected InterpreterConfigException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}
