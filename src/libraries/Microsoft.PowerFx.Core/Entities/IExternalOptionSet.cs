// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.Core.Entities
{
    internal interface IExternalOptionSet<T> : IExternalEntity, IDisplayMapped<int>
    {
        string Name { get; }
        bool IsBooleanValued { get; }
        
        string RelatedEntityName { get; }
        string RelatedColumnInvariantName { get; }
        bool IsGlobal { get; }
    }
}