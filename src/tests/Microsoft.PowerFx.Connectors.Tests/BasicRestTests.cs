// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
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
        private static readonly Uri _fakeBaseAddress = new ("http://localhost:5000");

        private static void AssertLog(LoggingTestServer testConnector, string expectedLog)
        {
            var log = testConnector._log.ToString().Trim();
            Assert.Equal(expectedLog, log);

            testConnector._log.Clear();
        }

        [Theory]
        [InlineData(1, @"Test.GetKey(""abc"")", "GET http://localhost:5000/Keys?keyName=abc")]
        [InlineData(2, @"Test.GetWeather3b(4, 8, 10, { i : 5 })", "GET http://localhost:5000/weather3b?i=5&ir=4&kr=10\r\n jr: 8")]
        [InlineData(3, @"Test.GetWeather3b(4, 8, 10, { i : 7, j : 9, k : 11 })", "GET http://localhost:5000/weather3b?i=7&ir=4&k=11&kr=10\r\n j: 9\r\n jr: 8")]
        [InlineData(4, @"Test.GetWeatherWithHeader()", "GET http://localhost:5000/weather/header")]
        [InlineData(5, @"Test.GetWeatherWithHeader({ id : 11 })", "GET http://localhost:5000/weather/header\r\n id: 11")]
        [InlineData(6, @"Test.GetWeatherWithHeader2()", "GET http://localhost:5000/weather/header2")]
        [InlineData(7, @"Test.GetWeatherWithHeader2({ id : 11 })", "GET http://localhost:5000/weather/header2\r\n id: 11")]
        [InlineData(8, @"Test.GetWeatherWithHeader2({ id : 11, id2 : 12 })", "GET http://localhost:5000/weather/header2\r\n id: 11\r\n id2: 12")]
        [InlineData(9, @"Test.GetWeatherWithHeader2({ id2 : 12 })", "GET http://localhost:5000/weather/header2\r\n id2: 12")]
        [InlineData(10, @"Test.GetWeatherWithHeaderStr()", "GET http://localhost:5000/weather/headerStr")]
        [InlineData(11, @"Test.GetWeatherWithHeaderStr({ str : ""a b"" })", "GET http://localhost:5000/weather/headerStr\r\n str: a b")]
        [InlineData(12, @"Test.PostWeatherWithId({body: 5})", "POST http://localhost:5000/weatherPost\r\n [content-header] Content-Type: text/json; charset=utf-8\r\n [body] 5")]
        [InlineData(13, @"Test.PostWeatherWithInputObject({x: [1], y:2})", "POST http://localhost:5000/weatherPost2\r\n [content-header] Content-Type: application/json; charset=utf-8\r\n [body] {\"x\":[1],\"y\":2}")]
        [InlineData(14, @"Test.GetT5(4, 6, {Id_A:1, Name_A: ""def"", Count:14, 'Object_B.Id_B': 2, 'Object_B.Name_B': ""ghi"", 'Object_B.Count': 7})", "GET http://localhost:5000/weather/t5?Id_A=1&Name_A=def&Count=14&Object_B.Id_B=2&Object_B.Name_B=ghi&d=4&Name_B=6")]
        [InlineData(15, @"Test.GetT6({d: 11, Name_B: 12, Id_A:1, Name_A: ""def"", Count:14, 'Object_B.Id_B': 2, 'Object_B.Name_B': ""ghi"", 'Object_B.Count': 7})", "GET http://localhost:5000/weather/t6?Id_A=1&Name_A=def&Count=14&Object_B.Id_B=2&Object_B.Name_B=ghi&d=11&Name_B=12")]
        [InlineData(16, @"Test.PostWeather8({z: {x: [1, 2], y:3 }, dt: DateTime(2022,6,16,13,26,24,900), db:0, str:""str"" })", "POST http://localhost:5000/weatherPost8\r\n [content-header] Content-Type: application/json; charset=utf-8\r\n [body] {\"z\":{\"x\":[1,2],\"y\":3},\"dt\":\"2022-06-16T13:26:24.9000000\",\"db\":0,\"str\":\"str\"}")]
        [InlineData(17, @"Test.PostWeatherWithUrlEncodedBody({x: [1, 2], y:3})", "POST http://localhost:5000/weatherPost5\r\n [content-header] Content-Type: application/x-www-form-urlencoded; charset=utf-8\r\n [body] X=1&X=2&Y=3")]
        [InlineData(18, @"Test.GetT7(1, ""abc"", 5, { id_B: 4, name_B: ""foo"", countB: 44 })", "POST http://localhost:5000/weather/t7\r\n [content-header] Content-Type: application/json; charset=utf-8\r\n [body] {\"id_A\":1,\"name_A\":\"abc\",\"count\":5,\"object_B\":{\"id_B\":4,\"name_B\":\"foo\",\"countB\":44}}")]
        [InlineData(19, @"Test.GetT8({body: Table({Value: 1}, {Value: 3})})", "POST http://localhost:5000/weather/t8\r\n [content-header] Content-Type: text/json; charset=utf-8\r\n [body] [1,3]")]
        [InlineData(20, @"Test.GetT8a(Table({Value: 1}, {Value: 444}))", "POST http://localhost:5000/weather/t8a\r\n [content-header] Content-Type: text/json; charset=utf-8\r\n [body] [1,444]")]
        [InlineData(21, @"Test.PostWeatherWithComplexInput({children: [{parent: { someNumbers: [123] }, otherString: ""xyz""}], someNumbers: [1,2,3]})", "POST http://localhost:5000/weatherPost4\r\n [content-header] Content-Type: application/json; charset=utf-8\r\n [body] {\"complexInput\":{\"children\":[{\"parent\":{\"someNumbers\":[123]},\"otherString\":\"xyz\"}],\"someNumbers\":[1,2,3]}}")]
        public async void ValidateHttpCalls(int i /* used for debugging */, string fxQuery, string httpQuery)
        {
            var swaggerFile = @"Swagger\TestOpenAPI.json";
            Console.Write(i);

            using var testConnector = new LoggingTestServer(swaggerFile);
            testConnector.SetResponseFromFile(@"Responses\HttpCall_1.json");

            List<ConnectorFunction> functions = OpenApiParser.GetFunctions(testConnector._apiDocument).OrderBy(cf => cf.Name).ToList();
            string funcName = new Regex(@"Test.([^(]+)").Match(fxQuery).Groups[1].Value;
            Assert.Equal("*[date:d, index:w, summary:s, temperatureC:w, temperatureF:w]", functions.First(f => funcName == f.Name).ReturnType.ToStringWithDisplayNames());

            var config = new PowerFxConfig(Features.PowerFxV1);
            using var httpClient = new HttpClient(testConnector) { BaseAddress = _fakeBaseAddress };
            config.AddService("Test", testConnector._apiDocument, httpClient);

            var engine = new RecalcEngine(config);

            var checkResult = engine.Check(fxQuery, options: _optionsPost);
            Assert.True(checkResult.IsSuccess, string.Join("\r\n", checkResult.Errors.Select(er => er.Message)));

            var rConfig = new RuntimeConfig();
            rConfig.SetTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time"));
            var result = await engine.EvalAsync(fxQuery, CancellationToken.None, options: _optionsPost, runtimeConfig: rConfig).ConfigureAwait(false);
            Assert.NotNull(result);

            var r = (dynamic)result.ToObject();
            Assert.NotNull(r);

            AssertLog(testConnector, httpQuery);
        }

        // Allow side-effects for executing behavior functions (any POST)
        private static readonly ParserOptions _optionsPost = new ()
        {
            AllowsSideEffects = true,
        };

        // Invoking a connector with a null client throws a InvalidOperationException exception.
        [Fact]
        public async Task BasicHttpCallNullInvoker()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\TestOpenAPI.json");

            var config = new PowerFxConfig();
            var apiDoc = testConnector._apiDocument;

            config.AddService("Test", apiDoc, null);

            var engine = new RecalcEngine(config);

            testConnector.SetResponse("55");

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await engine.EvalAsync("Test.GetKey(\"Key1\")", CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
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
        public async Task PetStore_MultiServer()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\PetStore.json");
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig();

            // Verify we can load the service
            config.AddService("PetStore", apiDoc);

            // Ensure we use HTTPS protocol
            Assert.Equal("https", apiDoc.GetScheme().Substring(0, 5));
        }

        [Fact]
        public async Task RealTest()
        {
            var config = new PowerFxConfig();
            
            // Note that these APIs are rate limited and HTTP error 429 is possible
            var swaggerUrl = "https://api.math.tools/yaml/math.tools.numbers.openapi.yaml";

            // Other intersting files:
            // "https://api.apis.guru/v2/specs/weatherbit.io/2.0.0/swagger.json"
            // "https://www.weatherbit.io/static/swagger.json"

            OpenApiDocument doc = await ReadSwaggerFromUrl(swaggerUrl).ConfigureAwait(false);

            // No BaseAdress specified, we'll use the 1st one found in the swagger file
            using var client = new HttpClient(); // public auth             
            var funcs = config.AddService("Math", doc, client);

            var engine = new RecalcEngine(config);
            var expr = "Math.numberscardinal({number: 1791941})";
            var check = engine.Check(expr);
            var ok = check.IsSuccess;

            Assert.True(ok);

            FormulaValue result = engine.Eval(expr);

            if (result is ErrorValue ev)
            {
                Assert.True(false, string.Join(", ", ev.Errors.Select(er => er.Message)));
            }

            // To read the complete result
            var sb = new StringBuilder();
            result.ToExpression(sb, new FormulaValueSerializerSettings() { UseCompactRepresentation = true });
            string resultExpr = sb.ToString();

            // Create an object from the result and access it
            dynamic result2 = result.ToObject();
            string cardinal = result2.contents.cardinal;

            Assert.Equal("one million seven hundred ninety-one thousand nine hundred forty-one", cardinal);
        }

        [Fact]
        public async Task RealTest2()
        {
            var config = new PowerFxConfig();

            // https://math.tools/api/numbers/
            // Note that these APIs are rate limited and HTTP error 429 is possible
            var swaggerUrl = "https://api.apis.guru/v2/specs/math.tools/1.5/openapi.json";

            OpenApiDocument doc = await ReadSwaggerFromUrl(swaggerUrl).ConfigureAwait(false);

            // Here we specify the BaseAddress
            using var client = new HttpClient() { BaseAddress = new Uri("https://api.math.tools") };

            // Set IgnoreUnknownExtensions to true as this swagger uses some extensions we don't honnor like x-apisguru-categories, x-origin, x-providerName
            var funcs = config.AddService("Math", doc, client, new ConnectorSettings() { IgnoreUnknownExtensions = true });

            var engine = new RecalcEngine(config);
            var expr = "Math.numbersbasebinary(632506623)";
            var check = engine.Check(expr);
            var ok = check.IsSuccess;

            Assert.True(ok, string.Join(", ", check.Errors.Select(er => er.Message)));

            FormulaValue result = engine.Eval(expr);

            if (result is ErrorValue ev)
            {
                Assert.True(false, string.Join(", ", ev.Errors.Select(er => er.Message)));
            }

            // To read the complete result
            var sb = new StringBuilder();
            result.ToExpression(sb, new FormulaValueSerializerSettings() { UseCompactRepresentation = true });
            string resultExpr = sb.ToString();

            // Create an object from the result and access it
            dynamic dResult = result.ToObject();
            string bin = dResult.contents.answer;

            Assert.Equal("100101101100110100100011111111", bin);

            // Try same number but in base 5 (why not??)
            var expr2 = @"Math.numbersbasebinary(2243410202443, {from: 35})";
            result = engine.Eval(expr2);

            if (result is ErrorValue ev2)
            {
                Assert.True(false, string.Join(", ", ev2.Errors.Select(er => er.Message)));
            }

            dynamic dResult2 = result.ToObject();
            string bin2 = dResult.contents.answer;

            Assert.Equal("100101101100110100100011111111", bin2);
        }        

        [Fact]
        public async Task RealTest3()
        {
            var config = new PowerFxConfig();

            // https://date.nager.at/       
            var swaggerUrl = "https://date.nager.at/swagger/v3/swagger.json";

            OpenApiDocument doc = await ReadSwaggerFromUrl(swaggerUrl).ConfigureAwait(false);
            using var client = new HttpClient() { BaseAddress = new Uri("https://date.nager.at") };            
            var funcs = config.AddService("Holiday", doc, client);

            var engine = new RecalcEngine(config);            
            var expr = @"Index(Holiday.PublicHolidayPublicHolidaysV3(2023, ""US""), 8)";

            // Validate expression
            var check = engine.Check(expr);

            var ok = check.IsSuccess;
            Assert.True(ok, string.Join(", ", check.Errors.Select(er => er.Message)));

            FormulaValue result = engine.Eval(expr);

            if (result is ErrorValue ev)
            {
                Assert.True(false, string.Join(", ", ev.Errors.Select(er => er.Message)));
            }

            // To read the complete result
            var sb = new StringBuilder();
            result.ToExpression(sb, new FormulaValueSerializerSettings() { UseCompactRepresentation = true });
            string resultExpr = sb.ToString();

            // Create an object from the result and access it
            dynamic dResult = result.ToObject();
            DateTime independanceDay = dResult.date;
            string independanceDayName = dResult.name;

            Assert.Equal("Independence Day", independanceDayName);
            Assert.Equal(new DateTime(2023, 7, 4), independanceDay);
        }

        // Get a swagger file from the embedded resources. 
        private static async Task<OpenApiDocument> ReadSwaggerFromUrl(string url)
        {
            using var http = new HttpClient();
            using (var stream = await http.GetStreamAsync(new Uri(url)).ConfigureAwait(false))
            {
                var doc = new OpenApiStreamReader().Read(stream, out OpenApiDiagnostic diag);

                if ((doc == null || doc.Paths == null || doc.Paths.Count == 0) && diag != null && diag.Errors.Count > 0)
                {
                    throw new InvalidDataException($"Unable to parse Swagger file: {string.Join(", ", diag.Errors.Select(err => err.Message))}");
                }

                return doc;
            }
        }
    }
}
