// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Public.Types
{
    public class CustomObjectType : FormulaType
    {
        public CustomObjectType()
            : base(DType.CustomObject)
        {
        }

        public override void Visit(ITypeVistor vistor)
        {
            vistor.Visit(this);
        }
    }
}
