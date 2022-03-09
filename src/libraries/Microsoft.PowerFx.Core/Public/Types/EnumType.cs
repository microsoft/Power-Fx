// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Public.Types
{
    public class EnumType : FormulaType
    {
        internal EnumType(DType superType, params KeyValuePair<DName, object>[] pairs)
            : base(DType.CreateEnum(superType, pairs))
        {
        }

        internal EnumType(DType superType, ValueTree valueTree)
            : base(DType.CreateEnum(superType, valueTree))
        {
        }

        public override void Visit(ITypeVistor vistor)
        {
            vistor.Visit(this);
        }
    }
}
