// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AppMagic.Transport;

namespace Microsoft.PowerFx.Core.Functions.TransportSchemas
{
    [TransportType(TransportKind.ByValue)]
    internal sealed class ParameterInfo
    {
        public string Label;
        public string Documentation;
    }
}