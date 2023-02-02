// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics.Contracts;
using System.Text;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    public class HyperlinkValue : PrimitiveValue<string>
    {
        internal HyperlinkValue(IRContext irContext, string value)
            : base(irContext, value)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.Hyperlink);
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings)
        {
            sb.Append($"\"{CharacterUtils.ExcelEscapeString(Value)}\"");
        }
    }
}
