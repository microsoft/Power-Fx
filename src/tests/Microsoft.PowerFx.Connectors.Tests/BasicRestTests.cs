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
using Microsoft.PowerFx.Types;
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
               
        [Theory]
        [InlineData(2, @"Test.GetWeatherWithHeader({ id : 11 })", "GET http://localhost:5000/weather/header\r\n id: 11")]
        [InlineData(2, @"Test.GetWeatherWithHeader()", "GET http://localhost:5000/weather/header")]
        [InlineData(2, @"Test.GetWeatherWithHeaderStr({ str : ""a b"" })", "GET http://localhost:5000/weather/headerStr\r\n str: a b")]
        [InlineData(2, @"Test.GetWeatherWithHeaderStr()", "GET http://localhost:5000/weather/headerStr")]
        [InlineData(2, @"Test.GetWeatherWithHeader2({ id : 11, id2 : 12 })", "GET http://localhost:5000/weather/header2\r\n id: 11\r\n id2: 12")]
        [InlineData(2, @"Test.GetWeatherWithHeader2({ id : 11 })", "GET http://localhost:5000/weather/header2\r\n id: 11")]
        [InlineData(2, @"Test.GetWeatherWithHeader2({ id2 : 12 })", "GET http://localhost:5000/weather/header2\r\n id2: 12")]
        [InlineData(2, @"Test.GetWeatherWithHeader2()", "GET http://localhost:5000/weather/header2")]
        [InlineData(2, @"Test.GetWeather3(4, 8, 10, { i : 7, j : 9, k : 11 })", "GET http://localhost:5000/weather3?i=7&ir=4&k=11&kr=10\r\n j: 9\r\n jr: 8")]
        [InlineData(2, @"Test.GetWeather3(4, 8, 10, { i : 5 })", "GET http://localhost:5000/weather3?i=5&ir=4&kr=10\r\n jr: 8")]
        [InlineData(1, @"Test.GetKey(""Key1"")", "GET http://localhost:5000/Keys?keyName=Key1")]

        public async void ValidateHttpCalls(int apiFileNumber, string fxQuery, string httpQuery)
        {
            string swaggerFile;

            switch (apiFileNumber)
            {
                case 1:
                    swaggerFile = @"Swagger\TestOpenAPI.json";
                    break;
                case 2:
                    swaggerFile = @"Swagger\TestOpenAPI2.json";
                    break;
                default:
                    throw new ArgumentException("Invalid apiFileNumber");                    
            }

            var testConnector = new LoggingTestServer(swaggerFile);
            testConnector.SetResponse("0");
                               
            var config = new PowerFxConfig();
            config.AddService("Test", testConnector._apiDocument, new HttpClient(testConnector) { BaseAddress = _fakeBaseAddress });
            var engine = new RecalcEngine(config);

            Assert.True(engine.Check(fxQuery).IsSuccess);

            var result = await engine.EvalAsync(fxQuery, CancellationToken.None);
            Assert.NotNull(result);

            var r = (dynamic)result.ToObject();
            Assert.NotNull(r);

            AssertLog(testConnector, httpQuery);
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
    }
}
