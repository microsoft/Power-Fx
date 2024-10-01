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
            CdpTable tabularTable = new CdpTable("dataset", "table", new List<RawTable>() { });
            CdpTableResolver tableResolver = new CdpTableResolver(tabularTable, httpClient, "prefix", true, connectorLogger);

            string text = (string)LoggingTestServer.GetFileText(@"Responses\Compatibility GetSchema.json");

            ConnectorType ctCdp = ConnectorFunction.GetConnectorTypeAndTableCapabilities(tableResolver, "name", "Schema/Items", StringValue.New(text), null, ConnectorCompatibility.CdpCompatibility, "dataset", out _, out _, out _);
            ConnectorType ctPa = ConnectorFunction.GetConnectorTypeAndTableCapabilities(tableResolver, "name", "Schema/Items", StringValue.New(text), null, ConnectorCompatibility.PowerAppsCompatibility, "dataset", out _, out _, out _);
            ConnectorType ctSw = ConnectorFunction.GetConnectorTypeAndTableCapabilities(tableResolver, "name", "Schema/Items", StringValue.New(text), null, ConnectorCompatibility.SwaggerCompatibility, "dataset", out _, out _, out _);

            string cdp = ctCdp.FormulaType._type.ToString();
            string pa = ctPa.FormulaType._type.ToString();
            string sw = ctSw.FormulaType._type.ToString();

            // CDP compatibility: priority is an enum, when "format": "enum" isn't present
            Assert.Equal("![Id1:s, Id3:s, Id4:s, priority:l, priority2:l]", cdp);

            // Swagger compatibility: priority is a string as "format": "enum" isn't present
            Assert.Equal("![Id1:s, Id3:s, Id4:s, priority:s, priority2:l]", sw);

            // PA compatibility: Id2 is internal and present (not the case for CDP/Swagger compatibilities)
            Assert.Equal("![Id1:s, Id2:s, Id3:s, Id4:s, priority:s, priority2:l]", pa);            
        }
    }
}
