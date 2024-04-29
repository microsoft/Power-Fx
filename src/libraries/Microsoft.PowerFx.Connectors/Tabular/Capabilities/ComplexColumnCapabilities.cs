// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Connectors.Tabular
{
    // renamed from PowerApps client ComplexColumnCapabiltiesConverter, ComplexColumnCapablities
    [JsonConverter(typeof(ComplexColumnCapabilitiesConverter))]
    internal sealed class ComplexColumnCapabilities : ColumnCapabilitiesBase, IColumnsCapabilities
    {
        internal Dictionary<string, ColumnCapabilitiesBase> _childColumnsCapabilities;

        public ComplexColumnCapabilities()
        {
            _childColumnsCapabilities = new Dictionary<string, ColumnCapabilitiesBase>();
        }

        public void AddColumnCapability(string name, ColumnCapabilitiesBase capability)
        {
            Contracts.AssertNonEmpty(name);
            Contracts.AssertValue(capability);

            _childColumnsCapabilities.Add(name, capability);
        }
    }
}
