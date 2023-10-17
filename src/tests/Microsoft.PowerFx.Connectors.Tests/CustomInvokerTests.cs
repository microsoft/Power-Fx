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

        // This is where we tell the runtime to use our custom invoker (otherwise HttpFunctionInvoker is used)
        internal override IConnectorInvoker GetInvoker(ConnectorFunction function)
        {
            return new TestCustomInvoker(function, this);
        }

        public override TimeZoneInfo TimeZoneInfo => TimeZoneInfo.Utc;

        public override object GetInvoker(string @namespace)
        {
            // No selection per namespace for simplicity
            return new CustomInvoker();
        }
    }

    internal class TestCustomInvoker : FunctionInvoker<CustomInvoker, CustomRequest, CustomResponse>
    {
        public ConnectorFunction Function { get; }

        public TestCustomInvoker(ConnectorFunction function, BaseRuntimeConnectorContext runtimeContext)
            : base(runtimeContext)
        {
            Function = function;
        }

        public override async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            (string url, Dictionary<string, string> headers, string body) = GetRequestElements(Function, args, null, cancellationToken);

            CustomInvoker customInvoker = (CustomInvoker)Context.GetInvoker(Function.Namespace);
            CustomRequest customRequest = new CustomRequest() { Url = url, Headers = headers, Body = body };

            CustomResponse customResponse = await SendAsync(customInvoker, customRequest, cancellationToken).ConfigureAwait(false);

            return RecordValue.NewRecordFromFields(customResponse.Parts.Select(kvp => new NamedValue(kvp.Key, FormulaValue.New(kvp.Value))));
        }

        public override Task<FormulaValue> InvokeAsync(string nextlink, CancellationToken cancellationToken)
        {
            // No need for paging in this example
            throw new NotImplementedException();
        }

        public override async Task<CustomResponse> SendAsync(CustomInvoker invoker, CustomRequest request, CancellationToken cancellationToken)
        {
            return await invoker.GetResult(request, cancellationToken).ConfigureAwait(false);
        }
    }

    internal class CustomInvoker
    {
        public async Task<CustomResponse> GetResult(CustomRequest request, CancellationToken cancellationToken)
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

    internal class CustomRequest
    {
        public string Url;
        public Dictionary<string, string> Headers;
        public string Body;
    }

    internal class CustomResponse
    {
        public Dictionary<string, string> Parts;
    }
}
