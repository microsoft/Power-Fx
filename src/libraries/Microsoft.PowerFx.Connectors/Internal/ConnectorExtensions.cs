﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.OpenApi.Interfaces;

namespace Microsoft.PowerFx.Connectors
{
    internal class ConnectorExtensions
    {
        internal string Summary;
        internal bool ExplicitInput;

        internal ConnectorExtensions(ISwaggerExtensions extension, ISwaggerExtensions body)
        {
            Summary = (body ?? extension).GetSummary();
            ExplicitInput = (body ?? extension).GetExplicitInput();
        }
    }
}
