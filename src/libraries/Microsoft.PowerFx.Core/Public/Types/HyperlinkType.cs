// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    public class HyperlinkType : FormulaType
    {
        public override DName Name => new DName("Hyperlink");

        internal HyperlinkType()
            : base(DType.Hyperlink)
        {
        }

        public override void Visit(ITypeVisitor vistor)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return "Hyperlink";
        }
    }
}
