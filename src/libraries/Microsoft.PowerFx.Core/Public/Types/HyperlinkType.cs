// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Public.Types
{
    public class HyperlinkType : FormulaType
    {
        internal HyperlinkType() : base(DType.Hyperlink)
        {
        }

        public override void Visit(ITypeVistor vistor)
        {
            throw new NotImplementedException();
        }
    }
}
