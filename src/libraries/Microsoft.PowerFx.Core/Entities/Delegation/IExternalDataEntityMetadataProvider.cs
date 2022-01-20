// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Functions.Delegation;

namespace Microsoft.PowerFx.Core.Entities.Delegation
{
    internal interface IExternalDataEntityMetadataProvider
    {
        bool TryGetEntityMetadata(string expandInfoIdentity, out IDataEntityMetadata entityMetadata);
    }
}