// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    internal class CdpTableResolver : ICdpTableResolver
    {
        public ConnectorLogger Logger { get; }

        private readonly CdpTable _tabularTable;

        private readonly HttpClient _httpClient;

        private readonly string _uriPrefix;

        private readonly bool _doubleEncoding;

        // Temporary hack to generate ADS
        public bool GenerateADS { get; init; }

    public CdpTableResolver(CdpTable tabularTable, HttpClient httpClient, string uriPrefix, bool doubleEncoding, ConnectorLogger logger = null)
        {
            _tabularTable = tabularTable;
            _httpClient = httpClient;
            _uriPrefix = uriPrefix;
            _doubleEncoding = doubleEncoding;
            
            Logger = logger;
        }

        public async Task<CdpTableDescriptor> ResolveTableAsync(string tableName, CancellationToken cancellationToken)
        {
            // out string name, out string displayName, out ServiceCapabilities tableCapabilities
            cancellationToken.ThrowIfCancellationRequested();

            string uri = (_uriPrefix ?? string.Empty) +
                (_uriPrefix.Contains("/sql/") ? "/v2" : string.Empty) +
                $"/$metadata.json/datasets/{(_doubleEncoding ? CdpServiceBase.DoubleEncode(_tabularTable.DatasetName) : _tabularTable.DatasetName)}/tables/{CdpServiceBase.DoubleEncode(tableName)}?api-version=2015-09-01";

            string text = await CdpServiceBase.GetObject(_httpClient, $"Get table metadata", uri, cancellationToken, Logger).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(text))
            {
                string connectorName = _uriPrefix.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)[1];
                ConnectorType ct = ConnectorFunction.GetConnectorTypeAndTableCapabilities(this, connectorName, "Schema/Items", FormulaValue.New(text), ConnectorCompatibility.SwaggerCompatibility, _tabularTable.DatasetName, out string name, out string displayName, out ServiceCapabilities tableCapabilities);

                return new CdpTableDescriptor() { ConnectorType = ct, Name = name, DisplayName = displayName, TableCapabilities = tableCapabilities };
            }

            return new CdpTableDescriptor();
        }
    }
}
