// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.PowerFx.Connectors.Tabular
{
    internal class GetTables
    {
        [JsonPropertyName("value")]
        public List<RawTable> Value { get; set; }
    }

    internal class RawTable
    {
        [JsonPropertyName("Name")]
        public string Name { get; set; }

        [JsonPropertyName("DisplayName")]
        public string DisplayName { get; set; }
    }

    internal class DatasetMetadata
    {
        [JsonPropertyName("tabular")]
        public RawTabular Tabular { get; set; }

        [JsonPropertyName("blob")]
        public RawBlob Blob { get; set; }

        [JsonPropertyName("datasetFormat")]
        public string DatasetFormat { get; set; }

        [JsonPropertyName("parameters")]
        public List<RawDatasetMetadataParameter> Parameters { get; set; }

        public bool IsDoubleEncoding => Tabular?.UrlEncoding == "double";
    }

    internal class RawTabular
    {
        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        [JsonPropertyName("urlEncoding")]
        public string UrlEncoding { get; set; }

        [JsonPropertyName("tableDisplayName")]
        public string TableDisplayName { get; set; }

        [JsonPropertyName("tablePluralName")]
        public string TablePluralName { get; set; }
    }

    internal class RawBlob
    {
        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        [JsonPropertyName("urlEncoding")]
        public string UrlEncoding { get; set; }
    }

    internal class RawDatasetMetadataParameter
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("urlEncoding")]
        public string UrlEncoding { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("required")]
        public bool Required { get; set; }

        [JsonPropertyName("x-ms-summary")]
        public string XMsSummary { get; set; }

        [JsonPropertyName("x-ms-dynamic-values")]
        public RawDynamicValues XMsDynamicValues { get; set; }
    }

    internal class RawDynamicValues
    {
        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("value-collection")]
        public string ValueCollection { get; set; }

        [JsonPropertyName("value-path")]
        public string ValuePath { get; set; }

        [JsonPropertyName("value-title")]
        public string ValueTitle { get; set; }
    }
}
