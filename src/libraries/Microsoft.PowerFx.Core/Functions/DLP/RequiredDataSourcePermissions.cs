// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.AppMagic.Transport;

namespace Microsoft.PowerFx.Core.Functions.DLP
{
    [Flags]
    [TransportType(TransportKind.Enum)]
    internal enum RequiredDataSourcePermissions
    {
        None = 0x0,
        Create = 0x1,
        Read = 0x2,
        Update = 0x4,
        Delete = 0x8
    }
}
