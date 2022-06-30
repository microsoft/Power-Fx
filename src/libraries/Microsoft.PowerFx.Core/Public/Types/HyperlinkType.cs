// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Types
{
    public class HyperlinkType : FormulaType
    {
        internal HyperlinkType()
            : base(DType.Hyperlink)
        {
        }

        public override void Visit(ITypeVisitor visitor)
        {
            throw new NotImplementedException();
        }
    }
}
