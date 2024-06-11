// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // Used in ConnectorTableValue
    internal class TabularTableType : TableType
    {
        public TabularTableType(TableType tableType)
            : base(new TabularDType(tableType))
        {
        }

        public override void Visit(ITypeVisitor vistor)
        {
            throw new System.NotImplementedException();
        }
    }
}
