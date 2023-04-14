// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    public class BooleanValue : PrimitiveValue<bool>
    {
        // List of types that allowed to convert to BooleanValue
        internal static readonly IReadOnlyList<FormulaType> AllowedListConvertToBoolean = new FormulaType[] { FormulaType.String, FormulaType.Number, FormulaType.Decimal, FormulaType.Boolean };

        internal BooleanValue(IRContext irContext, bool value)
            : base(irContext, value)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.Boolean);
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings)
        {
            sb.Append(Value.ToString().ToLowerInvariant());
        }
    }
}
