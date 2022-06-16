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
    internal static class OpenApiExtensions
    {
        public static string GetBasePath(this OpenApiDocument openApiDocument)
        {            
            if (openApiDocument.Servers == null)
            {
                return null;
            }

            // See https://spec.openapis.org/oas/v3.1.0#server-object
            var count = openApiDocument.Servers.Count;
            switch (count)
            {
                case 0: return null; // None
                case 1:
                    // This is a full URL that will pull in 'basePath' property from connectors. 
                    // Extract BasePath back out from this. 
                    var fullPath = openApiDocument.Servers[0].Url;
                    var uri = new Uri(fullPath);
                    var basePath = uri.PathAndQuery;
                    return basePath;
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

        public static bool TryGetDefaultValue(this OpenApiParameter param, out string defaultValue)
        {
            var x = param.Schema.Default;
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
        public static bool IsInternal(this OpenApiParameter param) => param.Extensions.TryGetValue("x-ms-visibility", out _);

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
                        var r2 = new RecordType().Add(TableValue.ValueName, elementType);
                        return r2.ToTable();
                    }
                    else
                    {
                        throw new NotImplementedException("Unsupported type of array");
                    }

                case "object":
                case null: // xml
                    var obj = new RecordType();
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

        private const string _applicationJson = "application/json";

        public static FormulaType GetReturnType(this OpenApiOperation op)
        {
            var responses = op.Responses;
            if (!responses.TryGetValue("200", out OpenApiResponse response200))
            {
                // If no 200, but "default", use that. 
                if (!responses.TryGetValue("default", out response200))
                {
                    throw new NotImplementedException($"Operation must have 200 response ({op.OperationId}).");
                }
            }

            if (response200.Content.Count == 0)
            {
                // No return type. Void() method. 
                return FormulaType.Blank;
            }

            // Responses is a list by content-type. Find "application/json"
            // Headers are case insensitive.
            foreach (var kv3 in response200.Content)
            {
                var mediaType = kv3.Key;
                var response = kv3.Value;

                if (string.Equals(mediaType, _applicationJson, StringComparison.OrdinalIgnoreCase))
                {
                    if (response.Schema == null)
                    {
                        // Treat as void. 
                        return FormulaType.Blank;
                    }

                    var responseType = response.Schema.ToFormulaType();
                    return responseType;                    
                }
            }

            // Returns something, but not json. 
            throw new InvalidOperationException($"Unsupported return type - must have {_applicationJson}");
        }

        private static readonly List<string> _knownContentTypes = new () { "text/json", "application/xml", "application/x-www-form-urlencoded", "application/json" };

        public static KeyValuePair<string, OpenApiMediaType> GetContentTypeAndSchema(this IDictionary<string, OpenApiMediaType> content)
        {
            List<KeyValuePair<string, OpenApiMediaType>> list = new ();

            foreach (var ct in _knownContentTypes)
            {
                var kvp = content.FirstOrDefault(c => c.Key == ct);

                if (kvp.Key != null)
                {
                    if ((kvp.Key == "application/json" && !kvp.Value.Schema.Properties.Any()) ||
                        (kvp.Key == "text/json" && kvp.Value.Schema.Properties.Any()))
                    {
                        continue;
                    }

                    list.Add(kvp);
                }
            }

            if (list.Any())
            {
                return list.First();
            }

            throw new NotImplementedException($"Cannot determine Content-Type {string.Join(", ", content.Keys)}");
        }
    }
}
