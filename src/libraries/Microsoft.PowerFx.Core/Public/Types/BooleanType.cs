// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Types
{
    public class BooleanType : FormulaType
    {
        internal BooleanType()
            : base(new DType(DKind.Boolean))
        {
        }

        public override void Visit(ITypeVistor vistor)
        {
            vistor.Visit(this);
        }

        public override string ToString()
        {
            return "Blank";
        }
    }
}
