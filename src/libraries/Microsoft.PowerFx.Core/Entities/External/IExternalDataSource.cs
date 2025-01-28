﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Entities.Delegation;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Entities
{
    internal interface IExternalDataSource : IExternalEntity, IExternalPageableSymbol
    {
        string Name { get; }

        bool IsSelectable { get; }

        bool IsDelegatable { get; }
        
        bool IsRefreshable { get; }

        bool RequiresAsync { get; }

        bool IsWritable { get; }

        bool IsClearable { get; }

        IExternalDataEntityMetadataProvider DataEntityMetadataProvider { get; }

        DataSourceKind Kind { get; }

        IExternalTableMetadata TableMetadata { get; }

        IDelegationMetadata DelegationMetadata { get; }
    }
}
