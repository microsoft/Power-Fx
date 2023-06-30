﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // See definitions for x-ms extensions:
    // https://docs.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions#x-ms-visibility
    public static class OpenApiExtensions
    {
        public static string GetBasePath(this OpenApiDocument openApiDocument) => GetUriElement(openApiDocument, (uri) => uri.PathAndQuery);

        public static string GetScheme(this OpenApiDocument openApiDocument) => GetUriElement(openApiDocument, (uri) => uri.Scheme);

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
                            if (obj.TryGetValue("value", out IOpenApiAny value2))
                            {
                                if (value2 is OpenApiString str)
                                {
                                    list.Add(str.Value);
                                    continue;
                                }
                                else if (value2 is OpenApiInteger i)
                                {
                                    list.Add(i.Value.ToString(CultureInfo.InvariantCulture));
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
            // https://learn.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions#x-ms-trigger
            // Identifies whether the current operation is a trigger that produces a single event.
            // The absence of this field means this is an action operation.
            var isTrigger = op.Extensions.ContainsKey("x-ms-trigger");
            return isTrigger;
        }

        public static bool TryGetDefaultValue(this OpenApiSchema schema, FormulaType formulaType, out FormulaValue defaultValue, bool numberIsFloat = false)
        {
            IOpenApiAny openApiDefaultValue = schema.Default;

            if (openApiDefaultValue == null)
            {
                if (formulaType is RecordType rt && schema.Properties != null)
                {
                    List<NamedValue> values = new List<NamedValue>();

                    foreach (NamedFormulaType nft in rt.GetFieldTypes())
                    {
                        string columnName = nft.Name.Value;

                        if (schema.Properties.ContainsKey(columnName))
                        {
                            if (schema.Properties[columnName].TryGetDefaultValue(nft.Type, out FormulaValue innerDefaultValue, numberIsFloat: numberIsFloat))
                            {
                                values.Add(new NamedValue(columnName, innerDefaultValue));
                            }
                        }
                    }

                    if (values.Any())
                    {
                        defaultValue = new InMemoryRecordValue(IRContext.NotInSource(rt), values);
                        return true;
                    }
                }

                defaultValue = null;
                return false;
            }
            
            return TryGetOpenApiValue(openApiDefaultValue, out defaultValue, numberIsFloat);
        }

        internal static bool TryGetOpenApiValue(IOpenApiAny openApiAny, out FormulaValue formulaValue, bool numberIsFloat = false)
        {
            if (openApiAny is OpenApiString str)
            {
                formulaValue = FormulaValue.New(str.Value);
            }
            else if (openApiAny is OpenApiInteger intVal)
            {
                formulaValue = numberIsFloat ? FormulaValue.New(intVal.Value) : FormulaValue.New((decimal)intVal.Value);
            }
            else if (openApiAny is OpenApiDouble dbl)
            {
                formulaValue = numberIsFloat ? FormulaValue.New(dbl.Value) : FormulaValue.New((decimal)dbl.Value);
            }
            else if (openApiAny is OpenApiLong lng)
            {
                formulaValue = numberIsFloat ? FormulaValue.New(lng.Value) : FormulaValue.New((decimal)lng.Value);
            }
            else if (openApiAny is OpenApiBoolean b)
            {
                formulaValue = FormulaValue.New(b.Value);
            }
            else if (openApiAny is OpenApiFloat flt)
            {
                formulaValue = numberIsFloat ? FormulaValue.New(flt.Value) : FormulaValue.New((decimal)flt.Value);
            }
            else if (openApiAny is OpenApiByte by)
            {
                // OpenApi library uses Convert.FromBase64String
                formulaValue = FormulaValue.New(Convert.ToBase64String(by.Value));
            }
            else if (openApiAny is OpenApiDateTime dt)
            {
                // A string like "2022-11-18" is interpreted as a DateTimeOffset
                // https://github.com/microsoft/OpenAPI.NET/issues/533
                throw new PowerFxConnectorException($"Unsupported OpenApiDateTime {dt.Value}");
            }
            else if (openApiAny is OpenApiPassword pw)
            {
                formulaValue = FormulaValue.New(pw.Value);
            }
            else if (openApiAny is OpenApiNull)
            {
                formulaValue = FormulaValue.NewBlank();
            }
            else if (openApiAny is OpenApiArray arr)
            {                
                List<FormulaValue> lst = new List<FormulaValue>();

                foreach (IOpenApiAny element in arr)
                {
                    bool ba = TryGetOpenApiValue(element, out FormulaValue fv, numberIsFloat);
                    if (!ba)
                    {
                        formulaValue = null;
                        return false;
                    }

                    lst.Add(fv);
                }

                RecordType recordType = RecordType.Empty().Add(TableValue.ValueName, FormulaType.String);
                IRContext irContext = IRContext.NotInSource(recordType);
                IEnumerable<InMemoryRecordValue> recordValues = lst.Select(item => new InMemoryRecordValue(irContext, new NamedValue[] { new NamedValue(TableValue.ValueName, item) }));

                formulaValue = FormulaValue.NewTable(recordType, recordValues);
            }
            else if (openApiAny is OpenApiObject o)
            {
                Dictionary<string, FormulaValue> dvParams = new ();

                foreach (KeyValuePair<string, IOpenApiAny> kvp in o)
                {                    
                    if (TryGetOpenApiValue(kvp.Value, out FormulaValue fv, numberIsFloat))
                    {
                        dvParams[kvp.Key] = fv;
                    }
                }

                formulaValue = FormulaValue.NewRecordFromFields(dvParams.Select(dvp => new NamedValue(dvp.Key, dvp.Value)));
            }
            else
            { 
                throw new NotImplementedException($"Unknown default value type {openApiAny.GetType().FullName}");
            }

            return true;
        }

        public static bool HasDefaultValue(this OpenApiParameter param)
        {
            return param.Schema.Default != null;
        }

        // Internal parameters are not showen to the user. 
        // They can have a default value or be special cased by the infrastructure (like "connectionId").
        public static bool IsInternal(this IOpenApiExtensible schema) => schema.Extensions.TryGetValue("x-ms-visibility", out var openApiExt) && openApiExt is OpenApiString openApiStr && openApiStr.Value == "internal";

        // See https://swagger.io/docs/specification/data-models/data-types/
        // numberIsFloat = numbers are stored as C# double when set to true, otherwise they are stored as C# decimal
        public static ConnectorParameterType ToFormulaType(this OpenApiSchema schema, Stack<string> chain = null, int level = 0, bool numberIsFloat = false)
        {
            if (chain == null)
            {
                chain = new Stack<string>();
            }

            if (level == 20)
            {
                throw new Exception("ToFormulaType() excessive recursion");
            }

            // schema.Format is optional and potentially any string
            switch (schema.Type)
            {
                // OpenAPI spec: Format could be <null>, byte, binary, date, date-time, password
                case "string":

                    // We don't want to have OptionSets in connections, we'll only get string/number for now in FormulaType
                    // Anyhow, we'll have schema.Enum content in ConnertorType

                    switch (schema.Format)
                    {                        
                        case "date":
                            return new ConnectorParameterType(schema, FormulaType.Date);
                        case "date-time":
                            return new ConnectorParameterType(schema, FormulaType.DateTime);
                        case "date-no-tz":
                            return new ConnectorParameterType(schema, FormulaType.DateTimeNoTimeZone);

                        case "binary":
                            return new ConnectorParameterType(schema, FormulaType.String);

                        case "enum":                            
                            if (schema.Enum.All(e => e is OpenApiString))
                            {                                
                                OptionSet os = new OptionSet("enum", schema.Enum.Select(e => new DName((e as OpenApiString).Value)).ToDictionary(k => k, e => e).ToImmutableDictionary());
                                return new ConnectorParameterType(schema, os.FormulaType);                            
                            }
                            else
                            {
                                throw new NotImplementedException($"Unsupported enum type {schema.Enum.GetType().Name}");
                            }

                        default:
                            return new ConnectorParameterType(schema, FormulaType.String);
                    }

                // OpenAPI spec: Format could be float, double, or not specified.
                // we assume not specified implies decimal
                // https://swagger.io/docs/specification/data-models/data-types/
                case "number":
                    switch (schema.Format)
                    {
                        case "float":
                        case "double":
                        case "integer":
                        case "byte":
                        case "number":
                        case "int32":
                            return numberIsFloat ? new ConnectorParameterType(schema, FormulaType.Number) : new ConnectorParameterType(schema, FormulaType.Decimal);

                        case null:
                        case "decimal":
                            return new ConnectorParameterType(schema, FormulaType.Decimal);

                        default:
                            throw new NotImplementedException($"Unsupported type of number: {schema.Format}");
                    }

                // Always a boolean (Format not used)                
                case "boolean": return new ConnectorParameterType(schema, FormulaType.Boolean);

                // OpenAPI spec: Format could be <null>, int32, int64
                case "integer":
                    switch (schema.Format)
                    {
                        case null:
                        case "int32":
                            return numberIsFloat ? new ConnectorParameterType(schema, FormulaType.Number) : new ConnectorParameterType(schema, FormulaType.Decimal);

                        case "int64":
                            return new ConnectorParameterType(schema, FormulaType.Decimal);

                        case "unixtime":
                            return new ConnectorParameterType(schema, FormulaType.DateTime);

                        default:
                            throw new NotImplementedException($"Unsupported type of integer: {schema.Format}");
                    }

                case "array":
                    var innerA = GetUniqueIdentifier(schema.Items);

                    if (innerA.StartsWith("R:", StringComparison.Ordinal) && chain.Contains(innerA))
                    {
                        // Here, we have a circular reference and default to a string
                        return new ConnectorParameterType(schema, FormulaType.String);
                    }

                    // Inheritance/Polymorphism - Can't know the exact type
                    // https://github.com/OAI/OpenAPI-Specification/blob/main/versions/2.0.md
                    if (schema.Items.Discriminator != null)
                    {
                        return new ConnectorParameterType(schema, FormulaType.UntypedObject);
                    }

                    chain.Push(innerA);
                    ConnectorParameterType cpt = schema.Items.ToFormulaType(chain, level + 1, numberIsFloat: numberIsFloat);
                    cpt.SetProperties("Array", true);
                    chain.Pop();                    

                    if (cpt.Type is RecordType r)
                    {
                        return new ConnectorParameterType(schema, r.ToTable(), cpt.ConnectorType);
                    }
                    else if (cpt.Type is TableType t)
                    {                        
                        // Array of array 
                        TableType tt = new TableType(TableType.Empty().Add(TableValue.ValueName, t)._type);
                        return new ConnectorParameterType(schema, tt, cpt.ConnectorType);
                    }
                    else if (cpt.Type is not AggregateType)
                    {
                        // Primitives get marshalled as a SingleColumn table.
                        // Make sure this is consistent with invoker. 
                        var r2 = RecordType.Empty().Add(TableValue.ValueName, cpt.Type);
                        return new ConnectorParameterType(schema, r2.ToTable(), cpt.ConnectorType);
                    }
                    else
                    {
                        throw new NotImplementedException("Unsupported type of array");
                    }

                case "object":
                case null: // xml                   

                    // Dictionary - https://swagger.io/docs/specification/data-models/dictionaries/
                    // Key is always a string, Value is in AdditionalProperties
                    if ((schema.AdditionalProperties != null && schema.AdditionalProperties.Properties.Any()) || schema.Discriminator != null)
                    {
                        return new ConnectorParameterType(schema, FormulaType.UntypedObject);
                    }
                    else
                    {
                        RecordType recordType = RecordType.Empty();
                        RecordType hiddenRecordType = null;
                        List<ConnectorType> connectorTypes = new List<ConnectorType>();
                        List<ConnectorType> hiddenConnectorTypes = new List<ConnectorType>();

                        foreach (var kv in schema.Properties)
                        {
                            bool hiddenRequired = false;

                            if (kv.Value.IsInternal())
                            {
                                if (schema.Required.Contains(kv.Key) && kv.Value.Default != null)
                                {
                                    hiddenRequired = true;
                                }
                                else
                                {
                                    continue;
                                }
                            }

                            var propName = kv.Key;
                            var innerO = GetUniqueIdentifier(kv.Value);

                            if (innerO.StartsWith("R:", StringComparison.Ordinal) && chain.Contains(innerO))
                            {
                                // Here, we have a circular reference and default to a string                                
                                return new ConnectorParameterType(schema, FormulaType.String, hiddenRecordType);
                            }

                            chain.Push(innerO);
                            ConnectorParameterType cpt2 = kv.Value.ToFormulaType(chain, level + 1, numberIsFloat: numberIsFloat);                            
                            cpt2.SetProperties(propName, schema.Required.Contains(propName));
                            chain.Pop();

                            if (cpt2.HiddenRecordType != null)
                            {
                                hiddenRecordType = (hiddenRecordType ?? RecordType.Empty()).Add(propName, cpt2.HiddenRecordType);
                                hiddenConnectorTypes.Add(cpt2.HiddenConnectorType);
                            }

                            if (hiddenRequired)
                            {
                                hiddenRecordType = (hiddenRecordType ?? RecordType.Empty()).Add(propName, cpt2.Type);
                                hiddenConnectorTypes.Add(cpt2.ConnectorType);
                            }
                            else
                            {
                                recordType = recordType.Add(propName, cpt2.Type);
                                connectorTypes.Add(cpt2.ConnectorType);
                            }
                        }

                        return new ConnectorParameterType(schema, recordType, hiddenRecordType, connectorTypes.ToArray(), hiddenConnectorTypes.ToArray());
                    }

                default:

                    throw new NotImplementedException($"Unsupported schema type {schema.Type}");
            }
        }

        private static string GetUniqueIdentifier(this OpenApiSchema schema)
        {
            return schema.Reference != null ? "R:" + schema.Reference.Id : "T:" + schema.Type;
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

        public static ConnectorType GetConnectorReturnType(this OpenApiOperation op, bool numberIsFloat)
        {
            return GetConnectorParameterReturnType(op, numberIsFloat).ConnectorType;
        }

        public static FormulaType GetReturnType(this OpenApiOperation op, bool numberIsFloat)
        {
            return GetConnectorParameterReturnType(op, numberIsFloat).Type;
        }

        public static string GetVisibility(this OpenApiOperation op)
        {
            return op.Extensions.TryGetValue("x-ms-visibility", out IOpenApiExtension openExt) && openExt is OpenApiString str ? str.Value : null;
        }

        public static bool GetRequiresUserConfirmation(this OpenApiOperation op)
        { 
            return op.Extensions.TryGetValue("x-ms-require-user-confirmation", out IOpenApiExtension openExt) && openExt is OpenApiBoolean b ? b.Value : false;            
        }

        private static ConnectorParameterType GetConnectorParameterReturnType(OpenApiOperation op, bool numberIsFloat)
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
                return new ConnectorParameterType();
            }

            // Responses is a list by content-type. Find "application/json"
            // Headers are case insensitive.
            foreach (var kv3 in response.Content)
            {
                string mediaType = kv3.Key.Split(';')[0];
                OpenApiMediaType openApiMediaType = kv3.Value;

                if (string.Equals(mediaType, ContentType_ApplicationJson, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(mediaType, ContentType_TextPlain, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(mediaType, ContentType_Any, StringComparison.OrdinalIgnoreCase))
                {
                    if (openApiMediaType.Schema == null)
                    {
                        // Treat as void. 
                        return new ConnectorParameterType();
                    }

                    ConnectorParameterType connectorParameterType = openApiMediaType.Schema.ToFormulaType(numberIsFloat: numberIsFloat);
                    connectorParameterType.SetProperties("response", true);

                    return connectorParameterType;
                }               
            }

            // Returns something, but not json. 
            throw new InvalidOperationException($"Unsupported return type - found {string.Join(", ", response.Content.Select(kv4 => kv4.Key))}");
        }

        // Keep these constants all lower case
        public const string ContentType_TextJson = "text/json";
        public const string ContentType_XWwwFormUrlEncoded = "application/x-www-form-urlencoded";
        public const string ContentType_ApplicationJson = "application/json";
        public const string ContentType_TextPlain = "text/plain";
        public const string ContentType_Any = "*/*";

        private static readonly IReadOnlyList<string> _knownContentTypes = new string[]
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
                    if (ct == ContentType_TextJson && mediaType.Schema.Properties.Any())
                    {
                        continue;
                    }

                    if (ct != ContentType_ApplicationJson)
                    {
                        return (ct, mediaType);
                    }

                    // application/json
                    if (mediaType.Schema.Properties.Any() || mediaType.Schema.Type == "object")
                    {
                        return (ct, mediaType);
                    }

                    if (mediaType.Schema.Type == "string")
                    {
                        return (ContentType_TextPlain, mediaType);
                    }
                }
            }

            // Cannot determine Content-Type
            return (null, null);
        }
    }
}
