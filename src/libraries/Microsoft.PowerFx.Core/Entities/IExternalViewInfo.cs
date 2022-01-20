// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx.Core.Entities
{
    internal interface IExternalViewInfo : IDisplayMapped<Guid>
    {
        string Name { get; }

        string RelatedEntityName { get; }
    }
}
