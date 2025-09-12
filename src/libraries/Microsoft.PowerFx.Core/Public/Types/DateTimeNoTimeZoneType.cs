﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    [DebuggerDisplay("{_type}:tzi")]
    public class DateTimeNoTimeZoneType : FormulaType
    {
        public override DName Name => new DName("DateTimeTZInd");

        internal DateTimeNoTimeZoneType()
            : base(DType.DateTimeNoTimeZone)
        {
        }

        public override void Visit(ITypeVisitor vistor)
        {
            vistor.Visit(this);
        }

        public override string ToString()
        {
            return "DateTimeNoTimeZone";
        }
    }
}
