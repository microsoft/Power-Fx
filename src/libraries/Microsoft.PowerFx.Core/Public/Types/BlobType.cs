// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Types
{
    public class BlobType : FormulaType
    {
        internal BlobType()
            : base(DType.Blob)
        {
        }

        public override void Visit(ITypeVisitor vistor)
        {
            vistor.Visit(this);
        }

        public override string ToString()
        {
            return "Blob";
        }        
    }
}
