// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Types
{
    public class VoidValue : ValidFormulaValue
    {
        internal VoidValue(IRContext irContext) 
            : base(irContext)
        {
        }

        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings)
        {
            sb.Append($"\"Void\"");
        }

        public override object ToObject()
        {
            return null;
        }

        public override string ToString()
        {
            return "Void";
        }

        public override void Visit(IValueVisitor visitor)
        {
            throw new NotSupportedException();
        }
    }
}
