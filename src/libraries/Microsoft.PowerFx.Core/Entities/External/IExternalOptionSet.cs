// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Entities
{
    internal interface IExternalOptionSet<T> : IExternalEntity, IDisplayMapped<int>
    {
        bool IsBooleanValued { get; }
    }
}
