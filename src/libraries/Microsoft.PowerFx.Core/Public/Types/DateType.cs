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

        public override void Visit(ITypeVistor vistor)
        {
            vistor.Visit(this);
        }

        public override string ToString()
        {
            return "Date";
        }
    }
}
