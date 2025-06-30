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
    public class CDPDelegationTests
    {
        private readonly ITestOutputHelper _output;

        public CDPDelegationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task CDPOdataExecutionTest()
        {
            using var testConnector = new LoggingTestServer(null /* no swagger */, _output);
            var config = new PowerFxConfig(Features.PowerFxV1);
            var engine = new RecalcEngine(config);

            ConsoleLogger logger = new ConsoleLogger(_output);
            using var httpClient = new HttpClient(testConnector);
            string connectionId = "c1a4e9f52ec94d55bb82f319b3e33a6a";
            string jwt = "eyJ0eXAiOiJKV1QiL...";
            using var client = new PowerPlatformConnectorClient("firstrelease-003.azure-apihub.net", "49970107-0806-e5a7-be5e-7c60e2750f01", connectionId, () => jwt, httpClient) { SessionId = "8e67ebdc-d402-455a-b33a-304820832383" };

            testConnector.SetResponseFromFile(@"Responses\SQL GetDatasetsMetadata.json");
            DatasetMetadata dm = await CdpDataSource.GetDatasetsMetadataAsync(client, $"/apim/sql/{connectionId}", CancellationToken.None, logger);

            Assert.NotNull(dm);
            Assert.Null(dm.Blob);

            Assert.Equal("{server},{database}", dm.DatasetFormat);
            Assert.NotNull(dm.Tabular);
            Assert.Equal("dataset", dm.Tabular.DisplayName);
            Assert.Equal("mru", dm.Tabular.Source);
            Assert.Equal("Table", dm.Tabular.TableDisplayName);
            Assert.Equal("Tables", dm.Tabular.TablePluralName);
            Assert.Equal("single", dm.Tabular.UrlEncoding);
            Assert.NotNull(dm.Parameters);
            Assert.Equal(2, dm.Parameters.Count);

            Assert.Equal("Server name.", dm.Parameters.First().Description);
            Assert.Equal("server", dm.Parameters.First().Name);
            Assert.True(dm.Parameters.First().Required);
            Assert.Equal("string", dm.Parameters.First().Type);
            Assert.Equal("double", dm.Parameters.First().UrlEncoding);
            Assert.Null(dm.Parameters.First().XMsDynamicValues);
            Assert.Equal("Server name", dm.Parameters.First().XMsSummary);

            Assert.Equal("Database name.", dm.Parameters.Skip(1).First().Description);
            Assert.Equal("database", dm.Parameters.Skip(1).First().Name);
            Assert.True(dm.Parameters.Skip(1).First().Required);
            Assert.Equal("string", dm.Parameters.Skip(1).First().Type);
            Assert.Equal("double", dm.Parameters.Skip(1).First().UrlEncoding);
            Assert.NotNull(dm.Parameters.Skip(1).First().XMsDynamicValues);
            Assert.Equal("/v2/databases?server={server}", dm.Parameters.Skip(1).First().XMsDynamicValues.Path);
            Assert.Equal("value", dm.Parameters.Skip(1).First().XMsDynamicValues.ValueCollection);
            Assert.Equal("Name", dm.Parameters.Skip(1).First().XMsDynamicValues.ValuePath);
            Assert.Equal("DisplayName", dm.Parameters.Skip(1).First().XMsDynamicValues.ValueTitle);
            Assert.Equal("Database name", dm.Parameters.Skip(1).First().XMsSummary);

            CdpDataSource cds = new CdpDataSource("pfxdev-sql.database.windows.net,connectortest", ConnectorSettings.NewCDPConnectorSettings(maxRows: 101));

            testConnector.SetResponseFromFiles(@"Responses\SQL GetDatasetsMetadata.json", @"Responses\SQL GetTables.json");
            IEnumerable<CdpTable> tables = await cds.GetTablesAsync(client, $"/apim/sql/{connectionId}", CancellationToken.None, logger);

            Assert.NotNull(tables);

            CdpTable connectorTable = tables.First(t => t.DisplayName == "Customers");

            Assert.False(connectorTable.IsInitialized);
            Assert.Equal("Customers", connectorTable.DisplayName);

            testConnector.SetResponseFromFiles(@"Responses\SQL Server Load Customers DB.json", @"Responses\SQL GetRelationships SampleDB.json");
            await connectorTable.InitAsync(client, $"/apim/sql/{connectionId}", CancellationToken.None, logger);
            Assert.True(connectorTable.IsInitialized);

            CdpTableValue sqlTable = connectorTable.GetTableValue();

            // Execute OData.
            var responseFile = @"Responses\BlankTopLevelAggregation.json";
            var oData = "$apply=aggregate%28Bonus%20with%20sum%20as%20result%29";
            var delegationParam = new MockDelegationParameters(DelegationParameterFeatures.ApplyTopLevelAggregation, FormulaType.Decimal, oData);
            testConnector.SetResponseFromFile(responseFile);
            var result = await sqlTable.ExecuteQueryAsync(null, delegationParam, CancellationToken.None);
            Assert.IsAssignableFrom<DecimalType>(result.Type);
            Assert.IsAssignableFrom<BlankValue>(result);

            // Execute OData with $count.
            responseFile = @"Responses\SQL Server Get First Customers.json";    
            oData = "$count=true";
            delegationParam = new MockDelegationParameters(DelegationParameterFeatures.Count, FormulaType.Decimal, oData, true);
            testConnector.SetResponseFromFile(responseFile);
            result = await sqlTable.ExecuteQueryAsync(null, delegationParam, CancellationToken.None);
            Assert.IsAssignableFrom<DecimalType>(result.Type);
            var count = Assert.IsAssignableFrom<DecimalValue>(result);
            Assert.Equal(2, count.Value);

            // Execute Filter OData but receive empty response.
            responseFile = @"Responses\EmptyTopLevelAggregation.json";
            oData = "$apply=aggregate%28Bonus%20with%20sum%20as%20result%29";
            delegationParam = new MockDelegationParameters(DelegationParameterFeatures.ApplyTopLevelAggregation, FormulaType.Decimal, oData);
            testConnector.SetResponseFromFile(responseFile);
            result = await sqlTable.ExecuteQueryAsync(null, delegationParam, CancellationToken.None);
            Assert.IsAssignableFrom<DecimalType>(result.Type);
            Assert.IsAssignableFrom<BlankValue>(result);
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
