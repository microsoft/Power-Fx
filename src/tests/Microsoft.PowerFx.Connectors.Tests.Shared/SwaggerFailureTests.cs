// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PowerFx.Connectors.Tests
{
    public class SwaggerFailureTests
    {
        public readonly ITestOutputHelper _output;

        public SwaggerFailureTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Swagger_NoEndpoint()
        {
            // Smallest possible swagger file
            OpenApiDocument doc = GetOpenApiDocument(@"{
  ""swagger"": ""2.0"",
  ""info"": {
    ""version"": ""1.0.1"",
    ""title"": ""Some title"",
  },
  ""paths"": {
  }
}");
            
            ConsoleLogger logger = new ConsoleLogger(_output);
            PowerFxConfig config = new PowerFxConfig();

            // No error here as we don't call GetAuthority
            IReadOnlyList<ConnectorFunction> funcs = config.AddActionConnector(new ConnectorSettings("X"), doc, logger);

            // Throws when no authority (Hosts in swagger)
            PowerFxConnectorException ex = Assert.Throws<PowerFxConnectorException>(() => new PowerPlatformConnectorClient(doc, "environmentId", "connectionId", () => "Token"));
            Assert.Contains("Swagger document doesn't contain an endpoint", ex.Message);
        }

        [Fact]
        public void Swagger_WithEndpoint()
        {
            OpenApiDocument doc = GetOpenApiDocument(@"{
  ""swagger"": ""2.0"",
  ""info"": {
    ""version"": ""1.0.1"",
    ""title"": ""Some title"",
  },
  ""host"": ""server1"",
  ""schemes"": [
    ""https""
  ],
  ""paths"": {
  }
}");

            ConsoleLogger logger = new ConsoleLogger(_output);
            PowerFxConfig config = new PowerFxConfig();

            // No error here as we don't call GetAuthority
            IReadOnlyList<ConnectorFunction> funcs = config.AddActionConnector(new ConnectorSettings("X"), doc, logger);

            // No exception as both 'host' and 'schemes' are defined (https is the only one supported)
            using PowerPlatformConnectorClient ppcc = new PowerPlatformConnectorClient(doc, "environmentId", "connectionId", () => "Token");

            Assert.Equal("https://server1/", ppcc.BaseAddress.ToString());
        }

        [Fact]
        public void Swagger_InvalidDynamicValue()
        {
            // The error in the below document is "parameter": 22 where 22 is not a string (for a valid dynamic reference, it needs to be a string)
            OpenApiDocument doc = GetOpenApiDocument(@"{
  ""swagger"": ""2.0"",
  ""info"": {
    ""version"": ""1.0.1"",
    ""title"": ""Some title"",
  },
  ""host"": ""server1"",
  ""schemes"": [
    ""https""
  ],
  ""paths"": {
    ""/v4/{param1}"": {
      ""post"": {
        ""summary"": ""Does something (V4)"",
        ""description"": ""Some description"",
        ""operationId"": ""SetV4"",    
        ""parameters"": [
          {
            ""name"": ""param1"",
            ""in"": ""path"",
            ""description"": ""Param description"",
            ""type"": ""number"",
            ""x-ms-url-encoding"": ""double"",
            ""x-ms-dynamic-values"": {
              ""operationId"": ""Action2"",
              ""parameters"": {
                ""isFolder"": true,
                ""fileFilter"": [],
                ""dataset"": {
                  ""parameter"": 22
                }
              },
              ""value-collection"": ""value"",
              ""value-path"": ""Name"",
              ""value-title"": ""DisplayName""
            },
           ""required"": true
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""OK"",
            ""schema"": {
              ""type"": ""object"",
              ""properties"": {}        
            }
          }
        }
      }
    }
  }
}");

            using PowerPlatformConnectorClient ppcc = new PowerPlatformConnectorClient(doc, "EnvironmentId", "ConnectionId", () => "Token");
            ConsoleLogger logger = new ConsoleLogger(_output);
            PowerFxConfig config = new PowerFxConfig();            

            // **** TEST 1: Classic evaluation with default settings ****
            logger.WriteLine("-- PowerFxConfig.AddActionConnector, default settings");
            IReadOnlyList<ConnectorFunction> funcs = config.AddActionConnector(new ConnectorSettings("X"), doc, logger);

            // No function should be returned as the only one declared in the swagger file is unsupported
            Assert.Empty(funcs);
            
            RecalcEngine engine = new RecalcEngine(config);
            BaseRuntimeConnectorContext runtimeContext = new TestConnectorRuntimeContext("X", ppcc, console: _output);
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(runtimeContext);
            AggregateException ae = Assert.Throws<AggregateException>(() => engine.EvalAsync("X.SetV4(0)", CancellationToken.None, runtimeConfig: runtimeConfig).Result);

            // If we try using the function, it doesn't exist
            Assert.Equal("Errors: Error 0-10: 'SetV4' is an unknown or unsupported function in namespace 'X'.", ae.InnerException.Message);

            // **** TEST 2: SDK level with default settings ****
            logger.WriteLine("-- OpenApiParser.GetFunctions, default settings");
            IEnumerable<ConnectorFunction> funcs2 = OpenApiParser.GetFunctions(new ConnectorSettings("X"), doc, logger);
            Assert.Empty(funcs2);

            // **** TEST 3: Classic evaluation with AllowUnsupportedFunctions flag ****
            logger.WriteLine("-- PowerFxConfig.AddActionConnector, with AllowUnsupportedFunctions");
            config = new PowerFxConfig();
            funcs = config.AddActionConnector(new ConnectorSettings("X") { AllowUnsupportedFunctions = true }, doc, logger);
           
            // With AllowUnsupportedFunctions, the function becomes visible
            Assert.Single(funcs);
            Assert.False(funcs.First().IsSupported);
            Assert.Equal("Invalid dynamic value type for Microsoft.OpenApi.Any.OpenApiObject, key = dataset", funcs.First().NotSupportedReason);

            engine = new RecalcEngine(config);
            ae = Assert.Throws<AggregateException>(() => engine.EvalAsync("X.SetV4(0)", CancellationToken.None, runtimeConfig: runtimeConfig).Result);

            // If we try using the function, even if it is present, we fail as it is unsupported
            Assert.Equal("Errors: Error 0-10: In namespace X, function SetV4 is not supported.", ae.InnerException.Message);

            // **** TEST 4: SDK level with AllowUnsupportedFunctions flag ****
            logger.WriteLine("-- OpenApiParser.GetFunctions, with AllowUnsupportedFunctions");
            funcs2 = OpenApiParser.GetFunctions(new ConnectorSettings("X") { AllowUnsupportedFunctions = true }, doc, logger);
            Assert.Single(funcs2);
            Assert.False(funcs2.First().IsSupported);
            Assert.Equal("Invalid dynamic value type for Microsoft.OpenApi.Any.OpenApiObject, key = dataset", funcs2.First().NotSupportedReason);

            // If we try using this unsupported function at a lower level, we still fail
            ae = Assert.Throws<AggregateException>(() => funcs2.First().InvokeAsync(new FormulaValue[] { FormulaValue.New(0) }, runtimeContext, CancellationToken.None).Result);            
            Assert.Equal("In namespace X, function SetV4 is not supported.", ae.InnerException.Message);
        }

        private OpenApiDocument GetOpenApiDocument(string str)
        {           
            OpenApiReaderSettings oars = new OpenApiReaderSettings() { RuleSet = ConnectorFunction.DefaultValidationRuleSet };
            OpenApiDocument doc = new OpenApiStringReader(oars).Read(str, out OpenApiDiagnostic diag);

            if (doc == null || (diag != null && diag.Errors.Count > 0))
            {
                throw new InvalidDataException($"Unable to parse Swagger file: {string.Join(", ", diag.Errors.Select(err => err.Message))}");
            }

            return doc;
        }
    }        
}
