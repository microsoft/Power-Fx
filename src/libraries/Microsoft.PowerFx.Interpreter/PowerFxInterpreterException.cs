using System;
using System.Runtime.Serialization;

namespace Microsoft.PowerFx.Interpreter
{
    [Serializable]
    public class PowerFxInterpreterException : Exception
    {
        public ExpressionError ExpressionError { get; }

        public PowerFxInterpreterException(ExpressionError ee)            
        {
            ExpressionError = ee;
        }

        public PowerFxInterpreterException(string message)
            : base(message)
        {
            throw new NotImplementedException();
        }

        public PowerFxInterpreterException(string message, Exception innerException)
            : base(message, innerException)
        {
            throw new NotImplementedException();
        }

        protected PowerFxInterpreterException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            throw new NotImplementedException();
        }
    }
}
