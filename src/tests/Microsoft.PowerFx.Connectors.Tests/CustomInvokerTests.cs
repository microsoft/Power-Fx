// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Connectors.Execution;
using Microsoft.PowerFx.Tests;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PowerFx.Connectors.Tests
{
    public class CustomInvokerTests
    {
        private readonly ITestOutputHelper _output;

        public CustomInvokerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task CustomInvokerTest()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\Office_365_Users.json");
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig();

            config.AddActionConnector("Office365Users", apiDoc, new ConsoleLogger(_output));
            var engine = new RecalcEngine(config);

            RuntimeConfig runtimeConfig = new RuntimeConfig();
            CustomInvokerRuntimeContext2 runtimeContext = new CustomInvokerRuntimeContext2(runtimeConfig);
            runtimeContext.AddCustomInvoker("Office365Users", (function) => new CustomFunctionInvoker(function, runtimeContext));
            runtimeConfig.AddRuntimeContext(runtimeContext);

            testConnector.SetResponseFromFile(@"Responses\Office365_UserProfileV2.json");
            var result = await engine.EvalAsync(@"Office365Users.UserProfileV2(""johndoe@microsoft.com"").mobilePhone", CancellationToken.None, runtimeConfig: runtimeConfig).ConfigureAwait(false);

            Assert.IsType<StringValue>(result);
            Assert.Equal("+33 799 999 999", (result as StringValue).Value);
        }

        [Fact]
        public async Task CustomInvokerTest2()
        {
            using var testConnector1 = new LoggingTestServer(@"Swagger\Office_365_Users.json");
            using var testConnector2 = new LoggingTestServer(@"Swagger\MSNWeather.json");            
            var apiDoc1 = testConnector1._apiDocument;
            var apiDoc2 = testConnector2._apiDocument;
            var config = new PowerFxConfig();
            var logger = new ConsoleLogger(_output);
            
            using var httpClient2 = new HttpClient(testConnector2);
            using var client2 = new PowerPlatformConnectorClient(apiDoc2, "839eace6-59ab-4243-97ec-a5b8fcc104e4", "shared-msnweather-8d08e763-937a-45bf-a2ea-c5ed-ecc70ca4", () => "AuthToken1", httpClient2) { SessionId = "MySessionId" };

            config.AddActionConnector("Office365Users", apiDoc1, logger);
            config.AddActionConnector("MSNWeather", apiDoc2, logger);
            var engine = new RecalcEngine(config);

            RuntimeConfig runtimeConfig = new RuntimeConfig();
            CustomInvokerRuntimeContext2 runtimeContext = new CustomInvokerRuntimeContext2(runtimeConfig);
            runtimeContext.AddCustomInvoker("Office365Users", (cf) => new CustomFunctionInvoker(cf, runtimeContext));
            runtimeContext.AddHttpInvoker("MSNWeather", client2);
            runtimeConfig.AddRuntimeContext(runtimeContext);

            testConnector1.SetResponseFromFile(@"Responses\Office365_UserProfileV2.json");
            testConnector2.SetResponseFromFile(@"Responses\MSNWeather_Response.json");                        

            var result = await engine.EvalAsync(@"Office365Users.UserProfileV2(""johndoe@microsoft.com"").mobilePhone & ""/"" & Text(MSNWeather.CurrentWeather(""Redmond"", ""Imperial"").responses.weather.current.temp)", CancellationToken.None, runtimeConfig: runtimeConfig).ConfigureAwait(false);

            Assert.IsType<StringValue>(result);
            Assert.Equal("+33 799 999 999/53", (result as StringValue).Value);
        }
    }

    internal class CustomInvokerRuntimeContext2 : BaseRuntimeConnectorContext
    {
        private readonly Dictionary<string, Func<ConnectorFunction, bool, FunctionInvoker>> _invokers = new ();

        public CustomInvokerRuntimeContext2(RuntimeConfig runtimeConfig) 
            : base(runtimeConfig)
        {
        }

        public void AddHttpInvoker(string @namespace, HttpMessageInvoker client)
        {
            _invokers.Add(@namespace, (function, rawResults) => new HttpFunctionInvoker(function, this, rawResults, client));
        }

        public void AddCustomInvoker(string @namespace, Func<ConnectorFunction, FunctionInvoker> getInvoker)
        {
            _invokers.Add(@namespace, (function, rawResults) => getInvoker(function));
        }        
        
        public override FunctionInvoker GetInvoker(ConnectorFunction function, bool rawResults)
        {
            if (_invokers.TryGetValue(function.Namespace, out var getInvoker))
            {
                return getInvoker(function, rawResults);
            }

            throw new NotImplementedException("Unknown namespace");
        }
    }

    internal class CustomFunctionInvoker : FunctionInvoker
    {
        public CustomFunctionInvoker(ConnectorFunction function, BaseRuntimeConnectorContext runtimeContext)
            : base(function, runtimeContext)
        {
        }

        public override async Task<FormulaValue> SendAsync(InvokerParameters invokerElements, CancellationToken cancellationToken)
        {
            CustomInvoker myInvoker = new CustomInvoker();
            CustomResponse response = await myInvoker.GetResult(invokerElements, cancellationToken).ConfigureAwait(false);

            return RecordValue.NewRecordFromFields(response.Parts.Select(kvp => new NamedValue(kvp.Key, FormulaValue.New(kvp.Value))));
        }
    }

    internal class CustomInvoker 
    {
        public BaseRuntimeConnectorContext Context => throw new NotImplementedException();

        public async Task<CustomResponse> GetResult(InvokerParameters request, CancellationToken cancellationToken)
        {
            return new CustomResponse()
            {
                Parts = new Dictionary<string, string>()
                {
                    { "mobilePhone", "+33 799 999 999" }
                }
            };
        }      
    }

    internal class CustomResponse
    {
        public Dictionary<string, string> Parts;
    }
}
