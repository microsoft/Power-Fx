// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // Used in TabularIRVisitor
    internal class TabularDType : DType
    {
        internal TableType TableType;
        internal TabularProtocol Protocol;

        internal TabularDType(TableType tableType, TabularProtocol protocol)
            : base(DKind.Table, tableType._type.TypeTree, null, tableType._type.DisplayNameProvider)
        {
            TableType = tableType;
            Protocol = protocol;
        }        
    }
}
