// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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
        private static readonly Uri _fakeBaseAddress = new ("http://localhost:5000");
        
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
        [InlineData(1, @"Test.GetKey(""Key1"")",  "GET http://localhost:5000/Keys?keyName=Key1")]
        [InlineData(3, @"Test.PostWeatherWithId({body: 5})", "POST http://localhost:5000/weatherPost\r\n [header] Content-Type: text/json; charset=utf-8\r\n [body] 5")]
        [InlineData(3, @"Test.PostWeatherWithInputObject({x: [1], y:2})", "POST http://localhost:5000/weatherPost2\r\n [header] Content-Type: application/json; charset=utf-8\r\n [body] {x:[1],y:2}")]
        [InlineData(4, @"Test.PostWeatherWithUrlEncodedBody({x: [1, 2], y:3 })", "POST http://localhost:5000/weatherPost5\r\n [header] Content-Type: application/x-www-form-urlencoded; charset=utf-8\r\n [body] X=1&X=2&Y=3")]
        [InlineData(3, @"Test.PostWeatherWithXML({x: [1, 2], y:3 })", "POST http://localhost:5000/weatherPostXML\r\n [header] Content-Type: application/xml; charset=utf-8\r\n [body] <Input><x><e>1</e><e>2</e></x><y>3</y></Input>")]
        public async void ValidateHttpCalls(int apiFileNumber, string fxQuery, string httpQuery)
        {
            var swaggerFile = apiFileNumber switch
            {
                1 => @"Swagger\TestOpenAPI.json",
                2 => @"Swagger\TestOpenAPI2.json",
                3 => @"Swagger\TestOpenAPI3.json",
                4 => @"Swagger\TestOpenAPI4.json",
                _ => throw new ArgumentException("Invalid apiFileNumber"),
            };

            var testConnector = new LoggingTestServer(swaggerFile);
            testConnector.SetResponse("0");
                               
            var config = new PowerFxConfig();
            config.AddService("Test", testConnector._apiDocument, new HttpClient(testConnector) { BaseAddress = _fakeBaseAddress });
            var engine = new RecalcEngine(config);

            var checkResult = engine.Check(fxQuery, options: _optionsPost);
            Assert.True(checkResult.IsSuccess, string.Join("\r\n", checkResult.Errors.Select(er => er.Message)));

            var result = await engine.EvalAsync(fxQuery, CancellationToken.None, options: _optionsPost);
            Assert.NotNull(result);

            var r = (dynamic)result.ToObject();
            Assert.NotNull(r);

            AssertLog(testConnector, httpQuery);
        }

        // Allow side-effects for executing behavior functions (any POST)
        private static readonly ParserOptions _optionsPost = new ()
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
