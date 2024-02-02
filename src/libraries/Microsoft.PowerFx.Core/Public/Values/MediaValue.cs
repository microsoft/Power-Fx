// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics.Contracts;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    public class MediaValue : FileValue
    {
        public MediaValue(IResourceManager resourceManager, IResourceElement element)
            : base(resourceManager, element)
        {
            Contract.Assert(IRContext.ResultType == FormulaType.Media);
        }

        internal MediaValue(IRContext irContext)
            : base(null, null)
        {
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
