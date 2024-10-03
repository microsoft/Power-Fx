// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Connectors;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PowerFx.Tests
{
    public class CdpRecordTypeTests
    {
        private readonly ITestOutputHelper _output;

        public CdpRecordTypeTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void CdpRecordTypeTest()
        {
            CdpRecordType cdpRecordType = GetCdpRecordType(2);

            Assert.NotNull(cdpRecordType);
            Assert.True(cdpRecordType.Equals(cdpRecordType));
            Assert.False(cdpRecordType.Equals(RecordType.Empty()));
            Assert.True(cdpRecordType.Equals(GetCdpRecordType(2)));
            Assert.False(cdpRecordType.Equals(GetCdpRecordType(1)));
        }

        private static CdpRecordType GetCdpRecordType(int i)
        {
            Dictionary<string, OpenApiSchema> properties = new Dictionary<string, OpenApiSchema>();

            for (int j = 0; j < i; j++)
            {
                properties.Add($"prop{j}", new OpenApiSchema() { Type = "string" });
            }

            ConnectorType connectorType = new ConnectorType(SwaggerSchema.New(new OpenApiSchema() { Properties = properties }), ConnectorCompatibility.Default);
            TableParameters tableParameters = new TableParameters()
            {
                TableName = "test"
            };

            CdpRecordType cdpRecordType = new CdpRecordType(connectorType, null, tableParameters);
            return cdpRecordType;
        }
    }
}
