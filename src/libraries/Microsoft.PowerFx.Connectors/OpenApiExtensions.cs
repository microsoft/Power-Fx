// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // See definitions for x-ms extensions:
    // https://docs.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions#x-ms-visibility
    public static class OpenApiExtensions
    {
        public static string GetBasePath(this OpenApiDocument openApiDocument) => GetUriElement(openApiDocument, (uri) => uri.PathAndQuery);        

        public static string GetAuthority(this OpenApiDocument openApiDocument) => GetUriElement(openApiDocument, (uri) => uri.Authority);        

        private static string GetUriElement(this OpenApiDocument openApiDocument, Func<Uri, string> getElement)
        {
            if (openApiDocument?.Servers == null)
            {
                return null;
            }

            // Exclude unsecure servers
            var servers = openApiDocument.Servers.Where(srv => srv.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)).ToArray();

            // See https://spec.openapis.org/oas/v3.1.0#server-object
            var count = servers.Length;
            switch (count)
            {
                case 0: return null; // None
                case 1:
                    // This is a full URL that will pull in 'basePath' property from connectors. 
                    // Extract BasePath back out from this. 
                    var fullPath = openApiDocument.Servers[0].Url;
                    var uri = new Uri(fullPath);
                    return getElement(uri);
                default:
                    throw new NotImplementedException($"Multiple servers not supported");
            }
        }

        public static string GetBodyName(this OpenApiRequestBody requestBody)
        {
            if (requestBody.Extensions.TryGetValue("x-bodyName", out IOpenApiExtension value) && value is OpenApiString oas)
            {
                return oas.Value;
            }

            return null;
        }

        // Get suggested options values.  Returns null if none. 
        public static string[] GetOptions(this OpenApiParameter param)
        {
            // x-ms-enum-values is: array of { value :string, displayName:string}.
            if (param.Extensions.TryGetValue("x-ms-enum-values", out var value))
            {
                if (value is OpenApiArray array)
                {
                    var list = new List<string>(array.Capacity);

                    foreach (var item in array)
                    {
                        if (item is OpenApiObject obj)
                        {
                            // has keys, "value", and "displayName"
                            if (obj.TryGetValue("value", out var value2))
                            {
                                if (value2 is OpenApiString str)
                                {
                                    list.Add(str.Value);
                                    continue;
                                }
                            }
                        }

                        throw new NotImplementedException($"Unrecognized x-ms-enum-values schema");
                    }

                    return list.ToArray();
                }
            }

            return null;
        }

        public static bool IsTrigger(this OpenApiOperation op)
        {
            var isTrigger = op.Extensions.ContainsKey("x-ms-trigger");
            return isTrigger;
        }

        public static bool TryGetDefaultValue(this OpenApiSchema schema, out string defaultValue)
        {
            var x = schema.Default;

            if (x == null)
            {
                defaultValue = null;
                return false;
            }

            if (x is OpenApiString str)
            {
                defaultValue = str.Value;
                return true;
            }

            if (x is OpenApiInteger intVal)
            {
                defaultValue = intVal.Value.ToString();
                return true;
            }

            if (x is OpenApiDouble dbl)
            {
                defaultValue = dbl.Value.ToString();
                return true;
            }

            if (x is OpenApiBoolean b)
            {
                defaultValue = b.Value.ToString();
                return true;
            }

            throw new NotImplementedException($"Unknown default value type {x.GetType().FullName}");
        }

        public static bool HasDefaultValue(this OpenApiParameter param)
        {
            return param.Schema.Default != null;
        }

        // Internal parameters are not showen to the user. 
        // They can have a default value or be special cased by the infrastructure (like "connectionId").
        public static bool IsInternal(this OpenApiParameter param) => param.Extensions.TryGetValue("x-ms-visibility", out var openApiExt) && openApiExt is OpenApiString openApiStr && openApiStr.Value == "internal";                    

        // See https://swagger.io/docs/specification/data-models/data-types/
        public static FormulaType ToFormulaType(this OpenApiSchema schema)
        {
            switch (schema.Type)
            {
                case "string": return FormulaType.String;
                case "number": return FormulaType.Number;
                case "boolean": return FormulaType.Boolean;
                case "integer": return FormulaType.Number;
                case "array":
                    var elementType = schema.Items.ToFormulaType();
                    if (elementType is RecordType r)
                    {
                        return r.ToTable();
                    }
                    else if (elementType is not AggregateType)
                    {
                        // Primitives get marshalled as a SingleColumn table.
                        // Make sure this is consistent with invoker. 
                        var r2 = RecordType.Empty().Add(TableValue.ValueName, elementType);
                        return r2.ToTable();
                    }
                    else
                    {
                        throw new NotImplementedException("Unsupported type of array");
                    }

                case "object":
                case null: // xml
                    var obj = RecordType.Empty();
                    foreach (var kv in schema.Properties)
                    {
                        var propName = kv.Key;
                        var propType = kv.Value.ToFormulaType();

                        obj = obj.Add(propName, propType);
                    }

                    return obj;

                default:

                    throw new NotImplementedException($"{schema.Type}");
            }
        }

        public static HttpMethod ToHttpMethod(this OperationType key)
        {
            return key switch
            {
                OperationType.Get => HttpMethod.Get,
                OperationType.Put => HttpMethod.Put,
                OperationType.Post => HttpMethod.Post,
                OperationType.Delete => HttpMethod.Delete,
                OperationType.Options => HttpMethod.Options,
                OperationType.Head => HttpMethod.Head,
                OperationType.Trace => HttpMethod.Trace,
                _ => new HttpMethod(key.ToString())
            };
        }

        public static FormulaType GetReturnType(this OpenApiOperation op)
        {
            var responses = op.Responses;
            if (!responses.TryGetValue("200", out OpenApiResponse response))
            {
                // If no 200, but "default", use that. 
                if (!responses.TryGetValue("default", out response))
                {
                    // If no default, use the first one we find
                    response = responses.FirstOrDefault().Value;
                }
            }

            if (response == null || response.Content.Count == 0)
            {
                // No return type. Void() method. 
                return FormulaType.Blank;
            }

            // Responses is a list by content-type. Find "application/json"
            // Headers are case insensitive.
            foreach (var kv3 in response.Content)
            {
                var mediaType = kv3.Key;
                var openApiMediaType = kv3.Value;

                if (string.Equals(mediaType, ContentType_ApplicationJson, StringComparison.OrdinalIgnoreCase))
                {
                    if (openApiMediaType.Schema == null)
                    {
                        // Treat as void. 
                        return FormulaType.Blank;
                    }

                    var responseType = openApiMediaType.Schema.ToFormulaType();
                    return responseType;
                }
            }

            // Returns something, but not json. 
            throw new InvalidOperationException($"Unsupported return type - must have {ContentType_ApplicationJson}");
        }

        // Keep these constants all lower case
        public const string ContentType_TextJson = "text/json";
        public const string ContentType_XWwwFormUrlEncoded = "application/x-www-form-urlencoded";
        public const string ContentType_ApplicationJson = "application/json";
        public const string ContentType_TextPlain = "text/plain";

        private static readonly List<string> _knownContentTypes = new ()
        {
            ContentType_ApplicationJson,
            ContentType_XWwwFormUrlEncoded,
            ContentType_TextJson
        };

        /// <summary>
        /// Identifies which ContentType and Schema to use.
        /// </summary>
        /// <param name="content"></param>
        /// <returns>RequestBody content dictionary of possible content types and associated schemas.</returns>
        /// <exception cref="NotImplementedException">When we cannot determine the content type to use.</exception>
        public static (string ContentType, OpenApiMediaType MediaType) GetContentTypeAndSchema(this IDictionary<string, OpenApiMediaType> content)
        {
            Dictionary<string, OpenApiMediaType> list = new ();

            foreach (var ct in _knownContentTypes)
            {
                if (content.TryGetValue(ct, out var mediaType))
                {
                    if ((ct == ContentType_ApplicationJson && !mediaType.Schema.Properties.Any()) ||
                        (ct == ContentType_TextJson && mediaType.Schema.Properties.Any()))
                    {
                        continue;
                    }

                    return (ct, mediaType);
                }
            }

            // Cannot determine Content-Type
            return (null, null);
        }
    }
}
