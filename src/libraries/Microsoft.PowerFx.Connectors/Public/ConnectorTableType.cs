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
            : base(recordType.ToTable()._type.Kind)
        {
            RecordType = recordType;
        }
    }
}
