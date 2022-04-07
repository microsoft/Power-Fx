// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Entities.Delegation;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.UtilityDataStructures;

namespace Microsoft.PowerFx.Core.Functions.Delegation
{
    /// <summary>
    /// Metadata information about data entity types.
    /// </summary>
    internal interface IDataEntityMetadata
    {
        string EntityName { get; }

        DType Schema { get; }

        void LoadClientSemantics(bool isPrimaryTable = false);

        void SetClientSemantics(IExternalTableDefinition tableDefinition);

        BidirectionalDictionary<string, string> DisplayNameMapping { get; }

        BidirectionalDictionary<string, string> PreviousDisplayNameMapping { get; }

        bool IsConvertingDisplayNameMapping { get; set; }

        IDelegationMetadata DelegationMetadata { get; }

        IExternalTableDefinition EntityDefinition { get; }

        string DatasetName { get; }

        bool IsValid { get; }

        string OriginalDataDescriptionJson { get; }

        string InternalRepresentationJson { get; }

        void ActualizeTemplate(string datasetName);

        void ActualizeTemplate(string datasetName, string entityName);

        string ToJsonDefinition();
    }
}
