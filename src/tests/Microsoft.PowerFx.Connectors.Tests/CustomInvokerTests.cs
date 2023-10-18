// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
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

            BaseRuntimeConnectorContext runtimeContext = new CustomInvokerRuntimeContext("Office365Users");
            RuntimeConfig runtimeConfig = new RuntimeConfig().AddRuntimeContext(runtimeContext);

            testConnector.SetResponseFromFile(@"Responses\Office365_UserProfileV2.json");
            var result = await engine.EvalAsync(@"Office365Users.UserProfileV2(""johndoe@microsoft.com"").mobilePhone", CancellationToken.None, runtimeConfig: runtimeConfig).ConfigureAwait(false);

            Assert.IsType<StringValue>(result);
            Assert.Equal("+33 799 999 999", (result as StringValue).Value);
        }
    }

    internal class CustomInvokerRuntimeContext : BaseRuntimeConnectorContext
    {
        private readonly string _namespace;

        public CustomInvokerRuntimeContext(string @namespace)
        {
            _namespace = @namespace;
        }

        public override TimeZoneInfo TimeZoneInfo => TimeZoneInfo.Utc;

        public override IConnectorInvoker GetInvoker(ConnectorFunction function, bool returnRawResults = false)
        {
            if (function.Namespace == _namespace)
            {
                return new CustomFunctionInvoker(function, this);
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
            CustomInvoker invoker = new CustomInvoker();
            CustomResponse response = await invoker.GetResult(invokerElements, cancellationToken).ConfigureAwait(false);

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
