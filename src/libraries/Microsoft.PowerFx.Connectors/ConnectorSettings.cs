// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Connectors
{
    public class ConnectorSettings 
    {        
        public ConnectorSettings(string @namespace)
        {
            Namespace = @namespace;
        }

        public string Namespace { get; }

        public bool NumberIsFloat { get; init; } = false;

        public int MaxRows { get; init; } = 1000;

        public bool IgnoreUnknownExtensions { get; init; } = false;

        public bool AllowUnsupportedFunctions { get; init; } = false;

        public bool ThrowOnError { get; init; } = false;

        internal bool ReturnRawResult { get; init; } = false;              
    }
}
