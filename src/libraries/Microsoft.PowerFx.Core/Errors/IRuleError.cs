// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AppMagic.Transport;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Errors
{
    [TransportType(TransportKind.ByValue)]
    internal interface IRuleError : IDocumentError
    {
        IRuleError Clone(Span span);
    }
}
