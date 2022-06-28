// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Types
{
    public class ColorType : FormulaType
    {
        internal ColorType()
            : base(DType.Color)
        {
        }

        public override void Visit(ITypeVistor vistor)
        {
            vistor.Visit(this);
        }

        public override string ToString()
        {
            return "Color";
        }
    }
}
