// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Types
{
    public class DateTimeType : FormulaType
    {
        internal DateTimeType()
            : base(DType.DateTime)
        {
        }

        public override void Visit(ITypeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
