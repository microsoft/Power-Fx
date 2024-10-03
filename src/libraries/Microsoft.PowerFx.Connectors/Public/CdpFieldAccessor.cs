// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // Simple field accessor to retrieve column types, without following relationships
    // Only used in CdpRecordType constructor where relationships must not be followed
    internal class CdpFieldAccessor : ITabularFieldAccessor
    {
        private readonly ConnectorType _connectorType;

        public CdpFieldAccessor(ConnectorType connector)
        {
            _connectorType = connector;
        }

        public bool TryGetFieldType(string fieldName, out FormulaType type)
        {
            ConnectorType field = _connectorType.Fields.FirstOrDefault(ct => ct.Name == fieldName);

            if (field == null)
            {
                type = null;
                return false;
            }

            type = field.FormulaType;
            return true;
        }
    }
}
