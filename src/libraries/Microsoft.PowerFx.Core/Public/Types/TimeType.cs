// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Types
{
    public class TimeType : FormulaType
    {
        internal TimeType()
            : base(DType.Time)
        {
        }

        public override void Visit(ITypeVistor vistor)
        {
            vistor.Visit(this);
        }

        public override string ToString()
        {
            return "Time";
        }
    }
}
