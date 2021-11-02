// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Transport;

namespace Microsoft.PowerFx.Core.App.Components
{
    [TransportType(TransportKind.Enum)]
    internal enum ComponentType
    {
        CanvasComponent = 0,
        DataComponent = 1,
        FunctionComponent = 2,
        CommandComponent = 3,
    }
}