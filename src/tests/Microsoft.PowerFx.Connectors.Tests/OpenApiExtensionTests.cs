// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
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
        [InlineData("http://www.foo.bar", "www.foo.bar", "/")]
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

            Assert.Equal(expectedAuthority, doc.GetAuthority());
            Assert.Equal(expectedBasePath, doc.GetBasePath());
        }

        [Fact]
        public void OpenApiExtension_Null()
        {
            Assert.Null((null as OpenApiDocument).GetAuthority());
            Assert.Null((null as OpenApiDocument).GetBasePath());
        }

        [Fact]
        public void OpenApiExtension_MultipleServers()
        {
            var doc = new OpenApiDocument();
            var srv1 = new OpenApiServer { Url = "https://server1" };
            var srv2 = new OpenApiServer { Url = "https://server2" };
            doc.Servers.Add(srv1);
            doc.Servers.Add(srv2);

            Assert.Throws<NotImplementedException>(() => doc.GetAuthority());
            Assert.Throws<NotImplementedException>(() => doc.GetBasePath());
        }
    }
}