// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Types
{
    public class DateType : FormulaType
    {
        internal DateType()
            : base(DType.Date)
        {
        }

        public override void Visit(ITypeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
