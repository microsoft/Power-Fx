// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Entities.Delegation;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Entities
{
    internal interface IExternalDataSource : IExternalEntity
    {
        public DType Schema { get; }

        string Name { get; }

        bool IsSelectable { get; }

        bool IsDelegatable { get; }

        bool RequiresAsync { get; }

        IExternalDataEntityMetadataProvider DataEntityMetadataProvider { get; }

        bool IsPageable { get; }

        DataSourceKind Kind { get; }

        IExternalTableMetadata TableMetadata { get; }

        IDelegationMetadata DelegationMetadata { get; }

        string ScopeId { get; }

        bool IsComponentScoped { get; }
    }
}