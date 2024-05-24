// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Entities.Delegation;
using Microsoft.PowerFx.Core.Functions.Delegation;

namespace Microsoft.PowerFx.Connectors.Tabular
{
    internal class CdpEntityMetadataProvider : IExternalDataEntityMetadataProvider
    {
        private readonly Dictionary<string, IDataEntityMetadata> _entityIdToMetadataMap;

        public CdpEntityMetadataProvider()
        {
            _entityIdToMetadataMap = new Dictionary<string, IDataEntityMetadata>();
        }
        
        public void AddSource(string sourceName, IDataEntityMetadata tabularDataSource)
        {           
            _entityIdToMetadataMap.Add(sourceName, tabularDataSource);
        }

        public bool TryGetEntityMetadata(string expandInfoIdentity, out IDataEntityMetadata entityMetadata)
        {
            return _entityIdToMetadataMap.TryGetValue(expandInfoIdentity, out entityMetadata);
        }
    }
}
