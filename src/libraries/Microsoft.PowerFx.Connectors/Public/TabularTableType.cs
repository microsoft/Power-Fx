// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // Used in ConnectorTableValue
    internal class TabularTableType : TableType
    {        
        public TabularTableType(TableType tableType, TabularProtocol protocol)
            : base(new TabularDType(tableType, protocol))
        {            
        }

        public override void Visit(ITypeVisitor vistor)
        {
            throw new System.NotImplementedException();
        }
    }
}
