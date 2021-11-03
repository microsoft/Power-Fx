using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using System.Text.Json;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;

namespace Microsoft.PowerFx.Core.Public.Values
{
    public class CustomObjectValue : ValidFormulaValue
    {
        protected readonly JsonElement _element;

        public JsonElement Element => _element;

        internal CustomObjectValue(IRContext irContext, JsonElement element)
            : base(irContext)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.CustomObject);
            _element = element;
        }

        public override object ToObject()
        {
            return _element.GetRawText();
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
