// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Transport;

namespace Microsoft.PowerFx.Core.Functions.TransportSchemas
{
    [TransportType(TransportKind.ByValue)]
    internal sealed class FunctionSignature
    {
        public string Label;
        public ParameterInfo[] Parameters;
    }
}