// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Connectors
{
    public class PowerPlatformConnectorClient2DiagnosticOptions
    {
        public string ClientSessionId { get; set; }

        public string ClientRequestId { get; set; }

        public string ClientTenantId { get; set; }

        public string ClientObjectId { get; set; }

        public string CorrelationId { get; set; }

        public string UserAgent { get; set; }
    }
}
