// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;

namespace Microsoft.PowerFx.Connectors.Execution
{
    public static class SerializationExtensions
    {
        public static string ToJson(this Dictionary<string, object> dic) => Regex.Replace(JsonSerializer.Serialize(dic), @"""(?<k>\w+)"":", "$1:");

        public static string ToFormUrlEncoded(this Dictionary<string, object> dic) => JsonDocument.Parse(JsonSerializer.Serialize(dic)).RootElement.JsonElementToString(null);

        private static string JsonElementToString(this JsonElement je, string prefix) => je.ValueKind switch
        {
            JsonValueKind.Array => string.Join($"&", je.EnumerateArray().Select(innerJe => innerJe.JsonElementToString(prefix))),
            JsonValueKind.False => $"{prefix}=0",
            JsonValueKind.Null => $"{prefix}=null",
            JsonValueKind.Number => $"{prefix}={je.GetDouble()}",
            JsonValueKind.Object => string.Join("&", je.EnumerateObject().Select(innerJp => innerJp.Value.JsonElementToString($"{(prefix == null ? string.Empty : prefix + ".")}{innerJp.Name}"))),
            JsonValueKind.String => $"{prefix}={HttpUtility.UrlEncode(je.GetString())}",
            JsonValueKind.True => $"{prefix}=0",
            JsonValueKind.Undefined => $"{prefix}=null",
            _ => throw new NotImplementedException($"Unknown ValueKind {je.ValueKind}")
        };

        public static string ToXml(this Dictionary<string, object> dic, string rootName)
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
