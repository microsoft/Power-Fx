// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Core
{
    /// <summary>
    /// Exception raised when converting an invalid FormulaTypeSchema to FormulaType. 
    /// Message contains a localized string that can be used as an error message.
    /// </summary>
    [Serializable]
    internal class TypeDefinitionException : Exception
    {
        public TypeDefinitionException()
            : base()
        {
        }

        public TypeDefinitionException(string message)
            : base(message)
        {
        }

        public TypeDefinitionException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected TypeDefinitionException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}
