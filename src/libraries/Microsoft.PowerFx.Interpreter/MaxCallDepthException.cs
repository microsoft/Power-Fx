// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter
{
    /// <summary>
    /// This is an interpreter internal excpetion and should be converted to a ErrorValue before returning to the host.
    /// </summary>
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

        public FormulaValue ToErrorValue(IRContext irContext)
        {
            return CommonErrors.MaxCallDepth(irContext);
        }
    }
}
