// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Connectors
{
    public class ConnectorSettings
    {
        public ICachingHttpClient Cache { get; set; } = null;

        public bool NumberIsFloat { get; set; } = false;

        public int MaxRows { get; set; } = 1000;
    }
}
