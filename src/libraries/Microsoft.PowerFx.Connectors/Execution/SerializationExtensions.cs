// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Xml.Linq;

namespace Microsoft.PowerFx.Connectors.Execution
{
    // $$$ To be removed
    internal static class SerializationExtensions
    {      
        internal static string ToXml(this Dictionary<string, object> dic, string rootName)
        {
            var root = new XElement(rootName);
            JsonDocument.Parse(JsonSerializer.Serialize(dic)).RootElement.JsonElementToXml(root);
            return new XDocument(root).ToString(SaveOptions.DisableFormatting);
        }

        private static void JsonElementToXml(this JsonElement je, XElement parent)
        {
            switch (je.ValueKind)
            {
                case JsonValueKind.Array:
                    foreach (var innerJe in je.EnumerateArray())
                    {
                        var xe = new XElement("e");
                        innerJe.JsonElementToXml(xe);
                        parent.Add(xe);
                    }

                    break;                

                case JsonValueKind.False:
                    parent.Value = "false";
                    break;

                case JsonValueKind.Null:
                    parent.Value = null;
                    break;

                case JsonValueKind.Number:
                    parent.Value = je.ToString();
                    break;

                case JsonValueKind.Object:
                    foreach (var innerJe in je.EnumerateObject())
                    {
                        var xx = new XElement(innerJe.Name);
                        innerJe.Value.JsonElementToXml(xx);
                        parent.Add(xx);
                    }

                    break;

                case JsonValueKind.String:
                    parent.Value = je.GetString();
                    break;

                case JsonValueKind.True:
                    parent.Value = "true";
                    break;

                case JsonValueKind.Undefined:
                    break;

                default:
                    throw new NotImplementedException($"Unknown ValueKind {je.ValueKind}");
            }
        }
    }
}
