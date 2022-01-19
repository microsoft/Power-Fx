// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AppMagic.Transport;

namespace Microsoft.PowerFx.Core.Errors
{
    [TransportType(TransportKind.Enum)]
    internal enum DocumentErrorKind
    {
        AXL,
        ClipBoard,
        Intellisense,
        Importer,
        Persistence,
        Publish,
        Rule,
        Entity,
        Migration,
        UnsupportedDocumentTypeOnImport,
        DeletedComponent
    }
}