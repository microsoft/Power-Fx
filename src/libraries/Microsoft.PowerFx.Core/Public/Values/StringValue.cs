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
        // List of types that allowed to convert to StringValue
        internal static readonly IReadOnlyList<FormulaType> AllowedListConvertToString = new FormulaType[]
        {
            FormulaType.Blob,
            FormulaType.Boolean,
            FormulaType.Date,
            FormulaType.DateTime,
            FormulaType.Decimal,
            FormulaType.Guid,
            FormulaType.Number,
            FormulaType.String,
            FormulaType.Time
        };

        internal StringValue(IRContext irContext, string value)
            : base(irContext, value)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.String);
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
