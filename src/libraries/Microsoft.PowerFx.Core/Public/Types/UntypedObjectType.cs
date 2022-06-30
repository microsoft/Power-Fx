// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Types
{
    public class UntypedObjectType : FormulaType
    {
        public UntypedObjectType()
            : base(DType.UntypedObject)
        {
        }

        public override void Visit(ITypeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
