// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Xml.Linq;

namespace Microsoft.PowerFx.Connectors
{
    public class ConnectorSettings 
    {
        public ICachingHttpClient Cache { get; set; } = null;

        public bool NumberIsFloat { get; set; } = false;

        public int MaxRows { get; set; } = 1000;

        public bool IgnoreUnknownExtensions { get; set; } = false;

        public bool ThrowOnError { get; set; } = true;

        public bool ReturnRawResult { get; set; } = false;

        public string Namespace { get; set; } = null;

        public ConnectorSettings Clone(string @namespace = null, bool? throwOnError = null, bool? returnRawResult = null)
        {
            return new ConnectorSettings()
            {
                Cache = Cache,
                NumberIsFloat = NumberIsFloat,
                MaxRows = MaxRows,
                IgnoreUnknownExtensions = IgnoreUnknownExtensions,
                ThrowOnError = throwOnError ?? ThrowOnError,
                ReturnRawResult = returnRawResult ?? ReturnRawResult,
                Namespace = @namespace ?? Namespace
            };
        }
    }
}
