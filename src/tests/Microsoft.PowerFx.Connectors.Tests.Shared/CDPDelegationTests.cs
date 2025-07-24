// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Tests;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PowerFx.Connectors.Tests
{
#pragma warning disable CS0618 // Type or member is obsolete for PowerPlatformConnectorClient
    public sealed class CDPDelegationTests : IAsyncLifetime, IDisposable
    {
        private LoggingTestServer _server;
        private HttpClient _httpClient;
        private ConsoleLogger _logger;
        private PowerPlatformConnectorClient _client;
        private CdpTableValue _sqlValue;
        private bool _disposed;
        private const string ConnectionId = "c1a4e9f52ec94d55bb82f319b3e33a6a";
        private readonly string _basePath = $"/apim/sql/{ConnectionId}";
        private const string Jwt = "eyJ0eXAiOiJKV1QiL...";
        private readonly ITestOutputHelper _output;

        public CDPDelegationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public async Task InitializeAsync()
        {
            // Common setup
            _server = new LoggingTestServer(null, _output);
            _httpClient = new HttpClient(_server);
            _logger = new ConsoleLogger(_output);

            var config = new PowerFxConfig(Features.PowerFxV1);
            var engine = new RecalcEngine(config);

            _client = new PowerPlatformConnectorClient(
                endpoint: "firstrelease-003.azure-apihub.net",
                environmentId: "49970107-0806-e5a7-be5e-7c60e2750f01",
                connectionId: ConnectionId,
                getAuthToken: () => Jwt,
                httpInvoker: _httpClient)
            { SessionId = Guid.NewGuid().ToString() };

            // Prepare table value
            _server.SetResponseFromFile(@"Responses\SQL GetDatasetsMetadata.json");
            await CdpDataSource.GetDatasetsMetadataAsync(_client, _basePath, CancellationToken.None, _logger);

            _server.SetResponseFromFiles(
                @"Responses\SQL GetDatasetsMetadata.json",
                @"Responses\SQL GetTables.json");
            var cds = new CdpDataSource(
                "pfxdev-sql.database.windows.net,connectortest",
                ConnectorSettings.NewCDPConnectorSettings(maxRows: 101));

            var tables = await cds.GetTablesAsync(
                _client, _basePath, CancellationToken.None, _logger);

            var custTable = tables.First(t => t.DisplayName == "Customers");
            _server.SetResponseFromFiles(
                @"Responses\SQL Server Load Customers DB.json",
                @"Responses\SQL GetRelationships SampleDB.json");

            await custTable.InitAsync(
                _client, _basePath, CancellationToken.None, _logger);
            _sqlValue = custTable.GetTableValue();
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            Dispose();
            await Task.CompletedTask;
        }

        // IDisposable implementation
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _sqlValue = null;
                _server?.Dispose();
                _httpClient?.Dispose();
                _client?.Dispose();
            }

            _disposed = true;
        }
     
        [Theory]
        [InlineData(@"Responses\BlankTopLevelAggregation.json", DelegationParameterFeatures.ApplyTopLevelAggregation, "$apply=aggregate(Bonus with sum as result)", true, null)]
        [InlineData(@"Responses\SQL Server Get First Customers.json", DelegationParameterFeatures.Count, "$count=true", false, 2)]
        [InlineData(@"Responses\EmptyTopLevelAggregation.json", DelegationParameterFeatures.ApplyTopLevelAggregation, "$apply=aggregate(Bonus with sum as result)", true, null)]
        public async Task CDPOdataExecutionTest(
                string responseFile,
                DelegationParameterFeatures features,
                string odata,
                bool expectBlank,
                int? expectCount)
            {
                _server.SetResponseFromFile(responseFile);
                var parameters = new MockDelegationParameters(
                    features,
                    FormulaType.Decimal,
                    odata,
                    features == DelegationParameterFeatures.Count);

                var result = await _sqlValue.ExecuteQueryAsync(
                    services: null,
                    parameters: parameters,
                    cancel: CancellationToken.None);

                Assert.IsAssignableFrom<DecimalType>(result.Type);
                if (expectBlank)
                {
                    Assert.IsAssignableFrom<BlankValue>(result);
                }
                else
                {
                    var val = Assert.IsAssignableFrom<DecimalValue>(result);
                    Assert.Equal(expectCount.Value, val.Value);
                }
            }

        private class MockDelegationParameters : DelegationParameters
        {
            private readonly DelegationParameterFeatures _features;

            public override DelegationParameterFeatures Features => _features;

            private readonly FormulaType _expectedType;

            public override FormulaType ExpectedReturnType => _expectedType;

            private readonly string _odata;

            private readonly bool _returnTotalCount = false;

            public MockDelegationParameters(DelegationParameterFeatures allowedFeatures, FormulaType expectedType, string oData, bool returnTotalCount = false)
            {
                _features = allowedFeatures;
                _expectedType = expectedType;
                _odata = oData;
                _returnTotalCount = returnTotalCount;
            }

            public override string GetODataApply()
            {
                throw new NotImplementedException();
            }

            public override string GetOdataFilter()
            {
                throw new NotImplementedException();
            }

            public override string GetODataQueryString()
            {
                if (string.IsNullOrEmpty(_odata))
                {
                    throw new NotImplementedException("OData query string is not implemented.");
                }

                return _odata;
            }

            public override IReadOnlyCollection<(string, bool)> GetOrderBy()
            {
                throw new NotImplementedException();
            }

            public override bool ReturnTotalCount()
            {
                return _returnTotalCount;
            }
        }
    }
}
