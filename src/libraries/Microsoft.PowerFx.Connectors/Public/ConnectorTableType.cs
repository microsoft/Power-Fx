// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    internal class ConnectorTableType : FormulaType
    {
        internal RecordType RecordType;

        public ConnectorTableType(RecordType recordType)
            : base(new TabularDType(recordType))
        {
            RecordType = recordType;
        }

        public override void Visit(ITypeVisitor vistor)
        {
            throw new System.NotImplementedException();
        }
    }    
}
