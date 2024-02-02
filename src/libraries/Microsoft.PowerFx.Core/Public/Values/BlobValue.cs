// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics.Contracts;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    public class BlobValue : FileValue
    {
        public BlobValue(IResourceManager resourceManager, IResourceElement element)
            : base(resourceManager, element)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.Blob);
        }

        internal BlobValue(IRContext irContext)
            : base(null, null)
        {
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
