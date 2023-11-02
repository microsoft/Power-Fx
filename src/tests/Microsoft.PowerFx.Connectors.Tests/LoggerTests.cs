// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PowerFx.Connectors.Tests
{
    public class LoggerTests
    {
        private readonly ITestOutputHelper _output;

        public LoggerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ConnectorLogger_Test1()
        {            
            ConsoleLogger logger = new ConsoleLogger(_output);
            IReadOnlyList<ConnectorFunction> functions = (null as PowerFxConfig).AddActionConnector(null as string, null, logger);

            Assert.Null(functions);
            Assert.Equal(
                @"[INFO ] Entering in ConfigExtensions.AddActionConnector, with ConnectorSettings Namespace <namespace is null>|" +
                @"[ERROR] PowerFxConfig is null, cannot add functions", logger.GetLogs());
        }

        [Fact]
        public void ConnectorLogger_Test2()
        {
            ConsoleLogger logger = new ConsoleLogger(_output);
            PowerFxConfig config = new PowerFxConfig(Features.PowerFxV1);
            IReadOnlyList<ConnectorFunction> functions = config.AddActionConnector(null as string, null, logger);

            Assert.Empty(functions);
            Assert.Equal(
                @"[INFO ] Entering in ConfigExtensions.AddActionConnector, with ConnectorSettings Namespace <namespace is null>|" +
                @"[ERROR] connectorSettings.Namespace is null|" +
                @"[INFO ] Exiting ConfigExtensions.AddActionConnector, returning 0 functions", logger.GetLogs());
        }

        [Fact]
        public void ConnectorLogger_Test3()
        {
            ConsoleLogger logger = new ConsoleLogger(_output);
            PowerFxConfig config = new PowerFxConfig(Features.PowerFxV1);
            IReadOnlyList<ConnectorFunction> functions = config.AddActionConnector(null as ConnectorSettings, null, logger);

            Assert.Empty(functions);           
            Assert.Equal(
                @"[INFO ] Entering in ConfigExtensions.AddActionConnector, with ConnectorSettings <null>|" +
                @"[ERROR] connectorSettings is null|" +
                @"[INFO ] Exiting ConfigExtensions.AddActionConnector, returning 0 functions", logger.GetLogs());
        }

        [Fact]
        public void ConnectorLogger_Test4()
        {
            ConsoleLogger logger = new ConsoleLogger(_output);
            PowerFxConfig config = new PowerFxConfig(Features.PowerFxV1);
            IReadOnlyList<ConnectorFunction> functions = config.AddActionConnector(new ConnectorSettings(null), null, logger);

            Assert.Empty(functions);
            Assert.Equal(
                @"[INFO ] Entering in ConfigExtensions.AddActionConnector, with ConnectorSettings Namespace <Namespace is null>, MaxRows 1000, FailOnUnknownExtension False, AllowUnsupportedFunctions False, |" +
                @"[ERROR] connectorSettings.Namespace is null|" +
                @"[INFO ] Exiting ConfigExtensions.AddActionConnector, returning 0 functions", logger.GetLogs());            
        }

        [Fact]
        public void ConnectorLogger_Test5()
        {
            ConsoleLogger logger = new ConsoleLogger(_output);
            PowerFxConfig config = new PowerFxConfig(Features.PowerFxV1);
            IReadOnlyList<ConnectorFunction> functions = config.AddActionConnector(new ConnectorSettings("Test"), null, logger);

            Assert.Empty(functions);
            Assert.Equal(
                @"[INFO ] Entering in ConfigExtensions.AddActionConnector, with ConnectorSettings Namespace Test, MaxRows 1000, FailOnUnknownExtension False, AllowUnsupportedFunctions False, |" +
                @"[ERROR] openApiDocument is null|" +
                @"[INFO ] Exiting ConfigExtensions.AddActionConnector, returning 0 functions", logger.GetLogs());            
        }

        [Fact]
        public void ConnectorLogger_Test6()
        {
            ConsoleLogger logger = new ConsoleLogger(_output);
            PowerFxConfig config = new PowerFxConfig(Features.PowerFxV1);
            IReadOnlyList<ConnectorFunction> functions = config.AddActionConnector(new ConnectorSettings("Test"), new OpenApiDocument(), logger);

            Assert.Empty(functions);
            Assert.Equal(
                @"[INFO ] Entering in ConfigExtensions.AddActionConnector, with ConnectorSettings Namespace Test, MaxRows 1000, FailOnUnknownExtension False, AllowUnsupportedFunctions False, |" +
                @"[ERROR] OpenApiDocument is invalid, it has no Path|" +
                @"[INFO ] Exiting ConfigExtensions.AddActionConnector, returning 0 functions", logger.GetLogs());            
        }

        [Fact]
        public void ConnectorLogger_Test7()
        {
            ConsoleLogger logger = new ConsoleLogger(_output);
            PowerFxConfig config = new PowerFxConfig(Features.PowerFxV1);
            IReadOnlyList<ConnectorFunction> functions = config.AddActionConnector(new ConnectorSettings(string.Empty), null, logger);

            Assert.Empty(functions);
            Assert.Equal(
                @"[INFO ] Entering in ConfigExtensions.AddActionConnector, with ConnectorSettings Namespace , MaxRows 1000, FailOnUnknownExtension False, AllowUnsupportedFunctions False, |" +
                @"[ERROR] connectorSettings.Namespace is not a valid DName|" +
                @"[INFO ] Exiting ConfigExtensions.AddActionConnector, returning 0 functions", logger.GetLogs());            
        }
        
        [Fact]
        public void ConnectorLogger_Test8()
        {
            ConsoleLogger logger = new ConsoleLogger(_output);
            PowerFxConfig config = new PowerFxConfig(Features.PowerFxV1);
            using LoggingTestServer testConnector = new LoggingTestServer(@"Swagger\SQL Server.json");
            IReadOnlyList<ConnectorFunction> functions = config.AddActionConnector(new ConnectorSettings("SQL"), testConnector._apiDocument, logger);

            Assert.NotEmpty(functions);
            Assert.Equal(
                "[INFO ] Entering in ConfigExtensions.AddActionConnector, with ConnectorSettings Namespace SQL, MaxRows 1000, FailOnUnknownExtension False, AllowUnsupportedFunctions False, |" +
                "[INFO ] Operation GET /{connectionId}/datasets({dataset})/tables({table})/onnewitems is trigger|" +
                "[INFO ] Operation GET /{connectionId}/datasets({dataset})/tables({table})/onupdateditems is trigger|" +
                "[INFO ] Operation GET /{connectionId}/datasets/{server},{database}/tables/{table}/onupdateditems is trigger|" +
                "[WARN ] OperationId ExecutePassThroughNativeQuery is deprecated|" +
                "[WARN ] OperationId GetTables is deprecated|" +
                "[WARN ] OperationId GetItems is deprecated|" +
                "[WARN ] OperationId PostItem is deprecated|" +
                "[WARN ] OperationId GetItem is deprecated|" +
                "[WARN ] OperationId DeleteItem is deprecated|" +
                "[WARN ] OperationId PatchItem is deprecated|" +
                "[INFO ] Operation GET /{connectionId}/datasets/default/tables/{table}/onnewitems is trigger|" +
                "[INFO ] Operation GET /{connectionId}/datasets/default/tables/{table}/onupdateditems is trigger|" +
                "[INFO ] Operation GET /{connectionId}/v2/datasets/{server},{database}/tables/{table}/onnewitems is trigger|" +
                "[INFO ] Namespace SQL: 'SQL Server' version 1.0 - 64 functions found|" +
                "[INFO ] Exiting ConfigExtensions.AddActionConnector, returning 64 functions", logger.GetLogs());
        }
    }
}
