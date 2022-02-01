// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.App;

namespace Microsoft.PowerFx.Core.Entities
{
    internal interface IExternalColumnMetadata
    {
        DataFormat? DataFormat { get; }
    }
}