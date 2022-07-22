// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Interpreter
{
    [Serializable]
    internal class UDFBindingMissingException : Exception
    {
        public UDFBindingMissingException()
           : base()
        {
        }

        public UDFBindingMissingException(string message)
            : base(message)
        {
        }

        public UDFBindingMissingException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected UDFBindingMissingException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}
