// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.OpenApi.Interfaces;

namespace Microsoft.PowerFx.Connectors
{
    internal interface ISwaggerExtensions
    {
        IDictionary<string, IOpenApiExtension> Extensions { get; }
    }
}
