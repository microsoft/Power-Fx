// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    public class StringValue : PrimitiveValue<string>
    {
        internal static readonly IReadOnlyList<FormulaType> AllowedListConvertToString = new FormulaType[] { FormulaType.String, FormulaType.Number, FormulaType.Decimal, FormulaType.DateTime, FormulaType.Date, FormulaType.Time, FormulaType.Boolean, FormulaType.Guid };

        internal StringValue(IRContext irContext, string value)
            : base(irContext, value)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.String);
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal StringValue ToLower()
        {
            return new StringValue(IRContext.NotInSource(FormulaType.String), Value.ToLowerInvariant());
        }

        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings)
        {
            sb.Append($"\"{CharacterUtils.ExcelEscapeString(Value)}\"");
        }
    }
}
