// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Types
{
    [DebuggerDisplay("{_type}:tzi")]
    public class DateTimeNoTimeZoneType : FormulaType
    {
        internal DateTimeNoTimeZoneType()
            : base(DType.DateTimeNoTimeZone)
        {
        }

        public override void Visit(ITypeVisitor vistor)
        {
            vistor.Visit(this);
        }
    }
}
