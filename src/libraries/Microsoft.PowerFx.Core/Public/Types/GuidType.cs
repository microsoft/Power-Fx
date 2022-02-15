// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Public.Types
{
    public class GuidType : FormulaType
    {
        internal GuidType()
            : base(DType.Guid)
        {
        }

        public override void Visit(ITypeVistor vistor)
        {
            vistor.Visit(this);
        }
    }
}
