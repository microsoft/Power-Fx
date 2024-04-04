// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    public class ConnectorTableType : FormulaType
    {
        internal RecordType RecordType;

        public ConnectorTableType(RecordType recordType)
            : base(new ConnectorDType(recordType))
        {
            RecordType = recordType;
        }

        public override void Visit(ITypeVisitor vistor)
        {
            throw new System.NotImplementedException();
        }
    }

    // Used in TabularIRVisitor
    internal class ConnectorDType : DType
    {
        internal RecordType RecordType;

        internal ConnectorDType(RecordType recordType)            
            : base(DKind.Table, recordType._type.ToTable().TypeTree) // 2nd param: key part that allows Texl functions to get the right return type (otherwise, we get ![] or *[])
        {
            RecordType = recordType;
        }
    }
}
