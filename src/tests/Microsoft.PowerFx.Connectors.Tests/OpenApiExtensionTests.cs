// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Core.Tests;
using Xunit;

namespace Microsoft.PowerFx.Connectors.Tests
{
    public class OpenApiExtensionTests : PowerFxTest
    {
        [Theory]
        [InlineData("https://www.foo.bar", "www.foo.bar", "/")]
        [InlineData("https://www.FOO.bar", "www.foo.bar", "/")]
        [InlineData("http://www.foo.bar", null, null)]
        [InlineData("https://www.foo.bar:117", "www.foo.bar:117", "/")]
        [InlineData("https://www.foo.bar/xyz", "www.foo.bar", "/xyz")]
        [InlineData("https://www.FOO.BAR:2883/xyz/ABC", "www.foo.bar:2883", "/xyz/ABC")]
        [InlineData("https://localhost:44/efgh", "localhost:44", "/efgh")]
        [InlineData(null, null, null)]
        public void OpenApiExtension_Get(string url, string expectedAuthority, string expectedBasePath)
        {
            var doc = new OpenApiDocument();

            if (!string.IsNullOrEmpty(url))
            {
                var srv = new OpenApiServer { Url = url };
                doc.Servers.Add(srv);
            }

            Assert.Equal(expectedAuthority, doc.GetAuthority(null));
            Assert.Equal(expectedBasePath, doc.GetBasePath(null));
        }

        [Fact]
        public void OpenApiExtension_Null()
        {
            Assert.Null((null as OpenApiDocument).GetAuthority(null));
            Assert.Null((null as OpenApiDocument).GetBasePath(null));
        }

        [Fact]
        public void OpenApiExtension_MultipleServers()
        {
            var doc = new OpenApiDocument();
            var srv1 = new OpenApiServer { Url = "https://server1" };
            var srv2 = new OpenApiServer { Url = "https://server2" };
            doc.Servers.Add(srv1);
            doc.Servers.Add(srv2);

            // string str = doc.SerializeAsJson(OpenApi.OpenApiSpecVersion.OpenApi3_0);

            //{
            //    "openapi": "3.0.1",
            //      "info": { },
            //      "servers": [
            //        {
            //           "url": "https://server1"
            //        },
            //        {
            //           "url": "https://server2"
            //        }
            //      ],
            //      "paths": { }
            //}

            ConnectorErrors errors = new ConnectorErrors();

            doc.GetAuthority(errors);
            Assert.True(errors.HasErrors);
            Assert.Equal("Multiple servers in OpenApiDocument is not supported", errors.Errors.First());

            errors = new ConnectorErrors();
            doc.GetBasePath(errors);
            Assert.True(errors.HasErrors);
            Assert.Equal("Multiple servers in OpenApiDocument is not supported", errors.Errors.First());
        }
    }
}
