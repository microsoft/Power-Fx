// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Types
{
    public class UntypedObjectType : FormulaType
    {
        public UntypedObjectType()
            : base(DType.UntypedObject)
        {
        }

        public override void Visit(ITypeVistor vistor)
        {
            vistor.Visit(this);
        }
    }
}
