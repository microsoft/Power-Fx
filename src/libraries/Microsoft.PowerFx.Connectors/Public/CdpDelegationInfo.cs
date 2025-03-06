// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Connectors;

namespace Microsoft.PowerFx.Core.Entities
{
    // Used by ServiceCapabilities.ToDelegationInfo for managing CDP x-ms-capabilities
    internal class CdpDelegationInfo : TableDelegationInfo
    {
        private readonly ConnectorType _connectorType;

        public CdpDelegationInfo(ConnectorType connectorType)
            : base()
        {
            _connectorType = connectorType;
        }

        public override ColumnCapabilitiesDefinition GetColumnCapability(string fieldName)
        {
            if (ColumnsCapabilities.TryGetValue(fieldName, out ColumnCapabilitiesBase columnCapabilitiesBase))
            {
                return columnCapabilitiesBase switch
                {
                    ColumnCapabilities columnCapabilities => columnCapabilities.Definition,
                    _ => throw new NotImplementedException($"{columnCapabilitiesBase.GetType().Name} not supported yet")
                };
            }

            return null;
        }

        public override IReadOnlyList<INavigationProperty> GetNavigationProperties(string fieldName)
        {
            return _connectorType.GetNavigationProperties(fieldName);
        }
    }
}
