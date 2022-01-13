using System;

namespace Microsoft.PowerFx.Core.IR
{
    internal class InvalidCoercionException : InvalidOperationException
    {
        public InvalidCoercionException(string message)
            : base(message)
        {
        }
    }
}
