// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Dynamic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Readers;
using Microsoft.PowerFx.Connectors;
using Microsoft.PowerFx.Core.Tests;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    // Simulate calling basic REST services, such as ASP.Net + Swashbuckle. 
    public class BasicRestTests : PowerFxTest
    {
        // Must set the BaseAddress on an httpClient, even if we don't actually use it. 
        // All the Send() methods will enforce this. 
        private static readonly Uri _fakeBaseAddress = new Uri("http://localhost:5000");
        
        private static void AssertLog(LoggingTestServer testConnector, string expectedLog)
        {
            var log = testConnector._log.ToString().Trim();
            Assert.Equal(expectedLog, log);

            testConnector._log.Clear();
        }

        [Fact]
        public async Task BasicHttpCall()
        {
            var testConnector = new LoggingTestServer(@"Swagger\TestOpenAPI.json");

            var httpClient = new HttpClient(testConnector)
            {
                BaseAddress = _fakeBaseAddress
            };

            var config = new PowerFxConfig();
            var apiDoc = testConnector._apiDocument;
            
            config.AddService("Test", apiDoc, httpClient);

            var engine = new RecalcEngine(config);

            testConnector.SetResponse("55");
            var r1 = await engine.EvalAsync("Test.GetKey(\"Key1\")", CancellationToken.None);            
            Assert.Equal(55.0, r1.ToObject());

            AssertLog(testConnector, "GET http://localhost:5000/Keys?keyName=Key1");
        }

        // Allow side-effects for executing behavior functions (any POST)
        private static readonly ParserOptions _optionsPost = new ParserOptions
        {
            AllowsSideEffects = true
        };

        [Fact]
        public async Task BasicHttpCallWithCaching()
        {
            var testConnector = new LoggingTestServer(@"Swagger\TestOpenAPI.json");

            var httpClient = new HttpClient(testConnector)
            {
                BaseAddress = _fakeBaseAddress
            };

            var config = new PowerFxConfig();
            var apiDoc = testConnector._apiDocument;

            var cache = new CachingHttpClient();
            config.AddService("Test", apiDoc, httpClient, cache);

            var engine = new RecalcEngine(config);

            testConnector.SetResponse("55");
            var r1 = await engine.EvalAsync("Test.GetKey(\"Key1\")", CancellationToken.None);
            Assert.Equal(55.0, r1.ToObject());

            AssertLog(testConnector, "GET http://localhost:5000/Keys?keyName=Key1");
           
            // hits the cache
            var r2 = await engine.EvalAsync("Test.GetKey(\"Key1\")", CancellationToken.None);
            Assert.Equal(55.0, r2.ToObject()); // Cached value
            AssertLog(testConnector, string.Empty); // not called

            // Post call will clear cache

            // POST call is a behavior function.
            // Must be called in a behavior context 
            testConnector.SetResponse("99");
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await engine.EvalAsync("Test.UpdateKey(\"Key1\", 23)", CancellationToken.None));

            var r3 = await engine.EvalAsync("Test.UpdateKey(\"Key1\", 23)", CancellationToken.None, options: _optionsPost);
            AssertLog(testConnector, "POST http://localhost:5000/Keys?keyName=Key1&newValue=23");
            
            // GET should hit again
            testConnector.SetResponse("99");
            var r4 = await engine.EvalAsync("Test.GetKey(\"Key1\")", CancellationToken.None);
            Assert.Equal(99.0, r4.ToObject()); // Cached value
            AssertLog(testConnector, "GET http://localhost:5000/Keys?keyName=Key1");
        }

        // Invoking a connector with a null client throws a InvalidOperationException exception.
        [Fact]
        public async Task BasicHttpCallNullInvoker()
        {
            var testConnector = new LoggingTestServer(@"Swagger\TestOpenAPI.json");

            var config = new PowerFxConfig();
            var apiDoc = testConnector._apiDocument;

            config.AddService("Test", apiDoc, null);

            var engine = new RecalcEngine(config);

            testConnector.SetResponse("55");

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await engine.EvalAsync("Test.GetKey(\"Key1\")", CancellationToken.None));            
        }

        // We can bind without calling.
        // In this case, w edon't needd a http client at all.
        [Fact]
        public void BasicHttpBinding()
        {
            var config = new PowerFxConfig();
            var apiDoc = Helpers.ReadSwagger(@"Swagger\TestOpenAPI.json");

            // If we don't pass httpClient, we can still bind, we just can't invoke.
            config.AddService("Test", apiDoc, null);

            var engine = new Engine(config);

            var r1 = engine.Check("Test.GetKey(\"Key1\")");
            Assert.True(r1.IsSuccess);
        }

        [Fact]
        public void BasicHttpBindingWithHeader()
        {
            var config = new PowerFxConfig();
            var apiDoc = Helpers.ReadSwagger(@"Swagger\TestOpenAPI2.json");
            
            config.AddService("Test", apiDoc, null);

            var engine = new Engine(config);

            var r1 = engine.Check("Test.GetWeatherWithHeader({ id : 11 })");
            Assert.True(r1.IsSuccess);
        }        
    }
}
