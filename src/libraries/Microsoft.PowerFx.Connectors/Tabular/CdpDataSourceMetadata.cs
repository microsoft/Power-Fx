// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Entities.Delegation;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.UtilityDataStructures;

namespace Microsoft.PowerFx.Connectors
{
    internal class CdpDataSourceMetadata : IDataEntityMetadata
    {
        public string EntityName { get; private set; }

        public bool IsConvertingDisplayNameMapping { get; set; }

        public string DatasetName { get; private set; }

        public bool IsValid { get; private set; }

        public string OriginalDataDescriptionJson => throw new NotImplementedException();

        public string InternalRepresentationJson => throw new NotImplementedException();

        public DType Schema { get; private set; } = DType.Invalid;

        public BidirectionalDictionary<string, string> DisplayNameMapping { get; internal set; }

        public BidirectionalDictionary<string, string> PreviousDisplayNameMapping { get; internal set; }

        public IDelegationMetadata DelegationMetadata => throw new NotImplementedException();

        public IExternalTableDefinition EntityDefinition => throw new NotImplementedException();

        public CdpDataSourceMetadata(string entityName, string datasetName, BidirectionalDictionary<string, string> displayNameMapping = null)
        {
            EntityName = entityName;
            DatasetName = datasetName;
            DisplayNameMapping = displayNameMapping ?? new BidirectionalDictionary<string, string>();
            PreviousDisplayNameMapping = null;
            IsConvertingDisplayNameMapping = false;
            IsValid = false;
            Schema = DType.Unknown;
        }

        public void ActualizeTemplate(string datasetName)
        {
            DatasetName = datasetName;
            LoadClientSemantics();
        }

        public void ActualizeTemplate(string datasetName, string entityName)
        {
            EntityName = entityName;
            ActualizeTemplate(datasetName);
        }

        public void LoadClientSemantics(bool isPrimaryTable = false)
        {
            IsValid = true;
        }

        public string ToJsonDefinition()
        {
            throw new NotImplementedException();
        }

        public void SetClientSemantics(IExternalTableDefinition tableDefinition)
        {
            throw new NotImplementedException();
        }
    }
}
