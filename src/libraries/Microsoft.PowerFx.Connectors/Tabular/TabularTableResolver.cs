// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    internal class TabularTableResolver : ITabularTableResolver
    {
        public ConnectorLogger Logger { get; }

        private readonly TabularTable _tabularTable;

        private readonly HttpClient _httpClient;

        private readonly string _uriPrefix;

        private readonly bool _doubleEncoding;

        public TabularTableResolver(TabularTable tabularTable, HttpClient httpClient, string uriPrefix, bool doubleEncoding, ConnectorLogger logger = null)
        {
            _tabularTable = tabularTable;
            _httpClient = httpClient;
            _uriPrefix = uriPrefix;
            _doubleEncoding = doubleEncoding;
            Logger = logger;
        }

        public async Task<TabularTableDescriptor> ResolveTableAsync(string tableName, CancellationToken cancellationToken)
        {
            // out string name, out string displayName, out ServiceCapabilities tableCapabilities
            cancellationToken.ThrowIfCancellationRequested();

            string uri = (_uriPrefix ?? string.Empty) +
                (_uriPrefix.Contains("/sql/") ? "/v2" : string.Empty) +
                $"/$metadata.json/datasets/{(_doubleEncoding ? TabularServiceBase.DoubleEncode(_tabularTable.DatasetName) : _tabularTable.DatasetName)}/tables/{TabularServiceBase.DoubleEncode(tableName)}?api-version=2015-09-01";

            string text = await TabularServiceBase.GetObject(_httpClient, $"Get table metadata", uri, cancellationToken, Logger).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(text))
            {
                string connectorName = _uriPrefix.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)[1];
                ConnectorType ct = ConnectorFunction.GetConnectorTypeAndTableCapabilities(this, connectorName, "Schema/Items", FormulaValue.New(text), ConnectorCompatibility.SwaggerCompatibility, _tabularTable.DatasetName, out string name, out string displayName, out ServiceCapabilities tableCapabilities);

                return new TabularTableDescriptor() { ConnectorType = ct, Name = name, DisplayName = displayName, TableCapabilities = tableCapabilities };
            }

            return new TabularTableDescriptor();
        }
    }
}
