// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Public.Types
{
    public class OptionSetValueType : FormulaType
    {
        internal OptionSetValueType() : base(new DType(DKind.OptionSetValue))
        {
        }
        public override void Visit(ITypeVistor vistor)
        {
            vistor.Visit(this);
        }
    }
}
