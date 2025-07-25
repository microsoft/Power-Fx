﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Connectors;
using Xunit;

namespace Microsoft.PowerFx.Connector.Tests
{
    public class PublicSurfaceTests
    {
        [Fact]
        public void PublicSurfaceTest_Connectors()
        {
            var asm = typeof(PowerPlatformConnectorClient2).Assembly;

            // The goal for public namespaces is to make the SDK easy for the consumer.
            // Namespace principles for public classes:            //
            // - prefer fewer namespaces. See C# for example: https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis
            // - For easy discovery, but Engine in "Microsoft.PowerFx".
            // - For sub areas with many related classes, cluster into a single subnamespace.
            // - Avoid nesting more than 1 level deep

            var allowed = new HashSet<string>()
            {
              "Microsoft.PowerFx.ConfigExtensions",
              "Microsoft.PowerFx.Connectors.AiSensitivity",
              "Microsoft.PowerFx.Connectors.BaseRuntimeConnectorContext",
              "Microsoft.PowerFx.Connectors.CdpDataSource",
              "Microsoft.PowerFx.Connectors.CdpExtensions",
              "Microsoft.PowerFx.Connectors.CdpService",
              "Microsoft.PowerFx.Connectors.CdpServiceBase",
              "Microsoft.PowerFx.Connectors.CdpTable",
              "Microsoft.PowerFx.Connectors.CdpTableValue",
              "Microsoft.PowerFx.Connectors.ConnectorCompatibility",
              "Microsoft.PowerFx.Connectors.ConnectorEnhancedSuggestions",
              "Microsoft.PowerFx.Connectors.ConnectorFunction",
              "Microsoft.PowerFx.Connectors.ConnectorKeyType",
              "Microsoft.PowerFx.Connectors.ConnectorLog",
              "Microsoft.PowerFx.Connectors.ConnectorLogger",
              "Microsoft.PowerFx.Connectors.ConnectorParameter",
              "Microsoft.PowerFx.Connectors.ConnectorParameters",
              "Microsoft.PowerFx.Connectors.ConnectorParameterWithSuggestions",
              "Microsoft.PowerFx.Connectors.ConnectorPermission",
              "Microsoft.PowerFx.Connectors.ConnectorSchema",
              "Microsoft.PowerFx.Connectors.ConnectorSettings",
              "Microsoft.PowerFx.Connectors.ConnectorType",
              "Microsoft.PowerFx.Connectors.Constants",
              "Microsoft.PowerFx.Connectors.DatasetMetadata",
              "Microsoft.PowerFx.Connectors.LogCategory",
              "Microsoft.PowerFx.Connectors.MediaKind",
              "Microsoft.PowerFx.Connectors.MetadataBlob",
              "Microsoft.PowerFx.Connectors.MetadataDynamicValues",
              "Microsoft.PowerFx.Connectors.MetadataParameter",
              "Microsoft.PowerFx.Connectors.MetadataTabular",
              "Microsoft.PowerFx.Connectors.OpenApiExtensions",
              "Microsoft.PowerFx.Connectors.OpenApiParser",
              "Microsoft.PowerFx.Connectors.PowerFxConnectorException",
              "Microsoft.PowerFx.Connectors.PowerPlatformConnectorClient",
              "Microsoft.PowerFx.Connectors.RuntimeConfigExtensions",
              "Microsoft.PowerFx.Connectors.RuntimeConnectorContextExtensions",
              "Microsoft.PowerFx.Connectors.SupportsConnectorErrors",
              "Microsoft.PowerFx.Connectors.Visibility",
              "Microsoft.PowerFx.Connectors.CDPSensitivityLabelInfo",
              "Microsoft.PowerFx.Connectors.CDPMetadataItem",
              "Microsoft.PowerFx.Connectors.ICDPAggregateMetadata",
              "Microsoft.PowerFx.Connectors.PowerPlatformConnectorHelper"
            };

            var sb = new StringBuilder();
            var count = 0;
            foreach (var type in asm.GetTypes().Where(t => t.IsPublic))
            {
                var name = type.FullName;
                if (!allowed.Contains(name))
                {
                    sb.AppendLine(name);
                    count++;
                }

                allowed.Remove(name);
            }

            Assert.True(count == 0, $"Unexpected public types: {sb}");

            // Types we expect to be in the assembly are all there.
            Assert.Empty(allowed);
        }
    }
}
