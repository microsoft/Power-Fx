using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using System.Text.Json;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;

namespace Microsoft.PowerFx.Core.Public.Values
{
    public interface ICustomObject
    {
        bool IsArray { get; }

        bool IsNull { get; }

        bool IsObject { get; }

        bool IsString { get; }

        bool IsNumber { get; }

        object ToObject();

        int GetArrayLength();

        ICustomObject this[int index] { get; }

        bool TryGetProperty(string value, out ICustomObject result);

        string GetString();

        double GetDouble();
    }

    public class CustomObjectValue : ValidFormulaValue
    {
        protected readonly ICustomObject _impl;

        public ICustomObject Impl => _impl;

        internal CustomObjectValue(IRContext irContext, ICustomObject impl)
            : base(irContext)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.CustomObject);
            _impl = impl;
        }

        public override object ToObject()
        {
            return _impl.ToObject();
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
