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

        [Fact]
        public async void BasicHttpCallWithHeader()
        {
            var testConnector = new LoggingTestServer(@"Swagger\TestOpenAPI2.json");
            testConnector.SetResponse(@"[{""date"":""2022-06-09T17:43:33.6483791+02:00"",""temperatureC"":-15,""temperatureF"":6,""summary"":""Bracing"",""index"":121},{""date"":""2022-06-10T17:43:33.6483939+02:00"",""temperatureC"":46,""temperatureF"":114,""summary"":""Freezing"",""index"":121},{""date"":""2022-06-11T17:43:33.6483941+02:00"",""temperatureC"":3,""temperatureF"":37,""summary"":""Bracing"",""index"":121},{""date"":""2022-06-12T17:43:33.6483943+02:00"",""temperatureC"":34,""temperatureF"":93,""summary"":""Warm"",""index"":121},{""date"":""2022-06-13T17:43:33.6483945+02:00"",""temperatureC"":27,""temperatureF"":80,""summary"":""Mild"",""index"":121}]");

            var config = new PowerFxConfig();
            config.AddService("Test", testConnector._apiDocument, new HttpClient(testConnector) { BaseAddress = _fakeBaseAddress });

            var engine = new RecalcEngine(config);

            var r1 = await engine.EvalAsync("Test.GetWeatherWithHeader({ id : 11 })", CancellationToken.None);
            dynamic i1 = r1.ToObject();
            Assert.Equal(121, i1[0].index);

            AssertLog(testConnector, "GET http://localhost:5000/weather/header\r\n id: 11");
        }

        [Fact]
        public async void BasicHttpCallWithTwoHeaders()
        {
            var testConnector = new LoggingTestServer(@"Swagger\TestOpenAPI2.json");
            testConnector.SetResponse(@"[{""date"":""2022-06-09T17:43:33.6483791+02:00"",""temperatureC"":-15,""temperatureF"":6,""summary"":""Bracing"",""index"":121},{""date"":""2022-06-10T17:43:33.6483939+02:00"",""temperatureC"":46,""temperatureF"":114,""summary"":""Freezing"",""index"":121},{""date"":""2022-06-11T17:43:33.6483941+02:00"",""temperatureC"":3,""temperatureF"":37,""summary"":""Bracing"",""index"":121},{""date"":""2022-06-12T17:43:33.6483943+02:00"",""temperatureC"":34,""temperatureF"":93,""summary"":""Warm"",""index"":121},{""date"":""2022-06-13T17:43:33.6483945+02:00"",""temperatureC"":27,""temperatureF"":80,""summary"":""Mild"",""index"":121}]");

            var config = new PowerFxConfig();
            config.AddService("Test", testConnector._apiDocument, new HttpClient(testConnector) { BaseAddress = _fakeBaseAddress });

            var engine = new RecalcEngine(config);

            var r1 = await engine.EvalAsync("Test.GetWeatherWithHeader2({ id : 11, id2 : 12 })", CancellationToken.None);
            dynamic i1 = r1.ToObject();
            Assert.Equal(121, i1[0].index);

            AssertLog(testConnector, "GET http://localhost:5000/weather/header2\r\n id: 11\r\n id2: 12");
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
    }
}
