// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Transport;

namespace Microsoft.PowerFx.Core.Errors
{
    [TransportType(TransportKind.ByValue)]
    internal interface IRuleError : IDocumentError
    {
    }
}
