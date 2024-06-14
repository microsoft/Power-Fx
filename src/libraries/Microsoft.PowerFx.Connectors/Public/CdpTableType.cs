// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // Used in ConnectorTableValue
    internal class CdpTableType : TableType
    {
        public CdpTableType(TableType tableType)
            : base(new CdpDtype(tableType))
        {
        }

        public override void Visit(ITypeVisitor vistor)
        {
            throw new System.NotImplementedException();
        }
    }
}
