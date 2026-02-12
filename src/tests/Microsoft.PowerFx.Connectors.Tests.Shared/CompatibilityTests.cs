// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Net.Http;
using Microsoft.PowerFx.Connectors;
using Microsoft.PowerFx.Connectors.Tests;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PowerFx.Tests
{
    public class CompatibilityTests : PowerFxTest
    {
        private readonly ITestOutputHelper _output;

        public CompatibilityTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void CompatibilityTest()
        {
            using LoggingTestServer loggingTestServer = new LoggingTestServer(null, _output);
            using HttpClient httpClient = new HttpClient(loggingTestServer);

            ConnectorLogger connectorLogger = new ConsoleLogger(_output);
            CdpTable tabularTable = new CdpTable("dataset", "table", new List<RawTable>() { }, null, tableMetadataCache: null);
            CdpTableResolver tableResolver = new CdpTableResolver(tabularTable, httpClient, "prefix", true, ConnectorSettings.NewCDPConnectorSettings(), tableMetadataCache: null, connectorLogger);

            string text = (string)LoggingTestServer.GetFileText(@"Responses\Compatibility GetSchema.json");

            ConnectorType ctCdp = ConnectorFunction.GetCdpTableType(tableResolver, "name", null, "schema/items", StringValue.New(text), new ConnectorSettings(null) { Compatibility = ConnectorCompatibility.CdpCompatibility }, "dataset", out _, out _);
            ConnectorType ctPa = ConnectorFunction.GetCdpTableType(tableResolver, "name", null, "schema/items", StringValue.New(text), new ConnectorSettings(null) { Compatibility = ConnectorCompatibility.PowerAppsCompatibility }, "dataset", out _, out _);
            ConnectorType ctSw = ConnectorFunction.GetCdpTableType(tableResolver, "name", null, "schema/items", StringValue.New(text), new ConnectorSettings(null) { Compatibility = ConnectorCompatibility.SwaggerCompatibility }, "dataset", out _, out _);

            string cdp = ctCdp.FormulaType.ToStringWithDisplayNames();
            string pa = ctPa.FormulaType.ToStringWithDisplayNames();
            string sw = ctSw.FormulaType.ToStringWithDisplayNames();

            // CDP compatibility: priority is an enum, when "format": "enum" isn't present
            Assert.Equal<object>("r![Id1`'User ID 1':s, Id3`'User ID 3':s, Id4`'User ID 4':s, priority`Priority:l, priority2`'Priority 2':l]", cdp);

            // Swagger compatibility: priority is a string as "format": "enum" isn't present
            Assert.Equal<object>("r![Id1`'User ID 1':s, Id3`'User ID 3':s, Id4`'User ID 4':s, priority`Priority:s, priority2`'Priority 2':l]", sw);

            // PA compatibility: Id2 is internal and present (not the case for CDP/Swagger compatibilities)
            Assert.Equal<object>("r![Id1`'User ID 1':s, Id2`'User ID 2':s, Id3`'User ID 3':s, Id4`'User ID 4':s, priority`Priority:s, priority2`'Priority 2':l]", pa);            
        }
    }
}
