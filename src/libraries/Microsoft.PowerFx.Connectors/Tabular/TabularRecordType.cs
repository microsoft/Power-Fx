// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Connectors.ConnectorFunction;

namespace Microsoft.PowerFx.Connectors.Tabular
{
    public class TabularRecordType : RecordType
    {
        internal ConnectorType ConnectorType;
        internal List<ReferencedEntity> ReferencedEntities;

        internal TabularRecordType(ConnectorType connectorType, DType dType)
            : base(dType)
        {
            ConnectorType = connectorType;
        }

        public override bool Equals(object other)
        {
            if (other is not TabularRecordType otherRecordType)
            {
                return false;
            }

            if (_type.IsLazyType && otherRecordType._type.IsLazyType && _type.IsRecord == otherRecordType._type.IsRecord)
            {
                return _type.LazyTypeProvider.BackingFormulaType.Equals(otherRecordType._type.LazyTypeProvider.BackingFormulaType);
            }

            return _type.Equals(otherRecordType._type);
        }

        public override int GetHashCode()
        {
            return _type.GetHashCode();
        }
    }
}
