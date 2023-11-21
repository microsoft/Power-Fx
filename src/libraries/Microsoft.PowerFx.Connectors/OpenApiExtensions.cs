// Copyright (c) Microsoft Corporation.
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
using static Microsoft.PowerFx.Connectors.Constants;

namespace Microsoft.PowerFx.Connectors
{
    // See definitions for x-ms extensions:
    // https://docs.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions#x-ms-visibility
    public static class OpenApiExtensions
    {
        // Keep these constants all lower case
        public const string ContentType_TextJson = "text/json";
        public const string ContentType_XWwwFormUrlEncoded = "application/x-www-form-urlencoded";
        public const string ContentType_ApplicationJson = "application/json";
        public const string ContentType_ApplicationOctetStream = "application/octet-stream";
        public const string ContentType_TextCsv = "text/csv";
        public const string ContentType_TextPlain = "text/plain";
        public const string ContentType_Any = "*/*";

        private static readonly IReadOnlyList<string> _knownContentTypes = new string[]
        {
            ContentType_ApplicationJson,
            ContentType_XWwwFormUrlEncoded,
            ContentType_TextJson
        };

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
            return requestBody.Extensions.TryGetValue(XMsBodyName, out IOpenApiExtension value) && value is OpenApiString oas ? oas.Value : "body";
        }

        // Get suggested options values.  Returns null if none. 
        public static string[] GetOptions(this OpenApiParameter openApiParameter)
        {
            // x-ms-enum-values is: array of { value :string, displayName:string}.
            if (openApiParameter.Extensions.TryGetValue(XMsEnumValues, out var enumValues))
            {
                if (enumValues is OpenApiArray array)
                {
                    var list = new List<string>(array.Capacity);

                    foreach (var item in array)
                    {
                        if (item is OpenApiObject obj)
                        {
                            // has keys, "value", and "displayName"
                            if (obj.TryGetValue("value", out IOpenApiAny value))
                            {
                                if (value is OpenApiString str)
                                {
                                    list.Add(str.Value);
                                    continue;
                                }
                                else if (value is OpenApiInteger i)
                                {
                                    list.Add(i.Value.ToString(CultureInfo.InvariantCulture));
                                    continue;
                                }
                            }
                        }

                        throw new NotImplementedException($"Unrecognized {XMsEnumValues} schema");
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
            return op.Extensions.ContainsKey(XMsTrigger);
        }

        public static bool TryGetDefaultValue(this OpenApiSchema schema, FormulaType formulaType, out FormulaValue defaultValue)
        {
            if (schema.Type == "array" && formulaType is TableType tableType && schema.Items != null)
            {
                RecordType recordType = tableType.ToRecord();
                bool b = schema.Items.TryGetDefaultValue(recordType, out FormulaValue itemDefaultValue);

                if (!b || itemDefaultValue is BlankValue)
                {
                    defaultValue = FormulaValue.NewTable(recordType);
                    return true;
                }

                if (itemDefaultValue is RecordValue itemDefaultRecordValue)
                {
                    defaultValue = FormulaValue.NewTable(recordType, itemDefaultRecordValue);
                    return true;
                }

                defaultValue = FormulaValue.NewTable(recordType, FormulaValue.NewRecordFromFields(new NamedValue[] { new NamedValue(TableValue.ValueName, itemDefaultValue) }));
                return true;
            }

            if (schema.Type == "object" || schema.Default == null)
            {
                if (formulaType is RecordType recordType2 && schema.Properties != null)
                {
                    List<NamedValue> values = new List<NamedValue>();

                    foreach (NamedFormulaType namedFormulaType in recordType2.GetFieldTypes())
                    {
                        string columnName = namedFormulaType.Name.Value;

                        if (schema.Properties.ContainsKey(columnName))
                        {
                            if (schema.Properties[columnName].TryGetDefaultValue(namedFormulaType.Type, out FormulaValue innerDefaultValue))
                            {
                                values.Add(new NamedValue(columnName, innerDefaultValue));
                            }
                            else
                            {
                                values.Add(new NamedValue(columnName, FormulaValue.NewBlank(namedFormulaType.Type)));
                            }
                        }
                    }

                    defaultValue = values.Any(v => v.Value is not BlankValue) ? FormulaValue.NewRecordFromFields(values) : FormulaValue.NewBlank(recordType2);
                    return true;
                }

                if (schema.Default == null)
                {
                    defaultValue = null;
                    return false;
                }
            }

            return TryGetOpenApiValue(schema.Default, formulaType, out defaultValue);
        }

        internal static bool TryGetOpenApiValue(IOpenApiAny openApiAny, FormulaType formulaType, out FormulaValue formulaValue, bool allowOpenApiDateTime = false)
        {
            if (openApiAny is OpenApiString str)
            {
                formulaValue = null;

                if (formulaType != null && formulaType is not StringType && formulaType is not RecordType)
                {
                    if (formulaType is BooleanType && bool.TryParse(str.Value, out bool b))
                    {
                        formulaValue = FormulaValue.New(b);
                    }
                    else if (formulaType is DecimalType && decimal.TryParse(str.Value, out decimal d))
                    {
                        formulaValue = FormulaValue.New(d);
                    }
                    else if (formulaType is DateTimeType)
                    {
                        if (DateTime.TryParse(str.Value, out DateTime dt))
                        {
                            formulaValue = FormulaValue.New(dt);
                        }

                        throw new PowerFxConnectorException($"Unsupported DateTime format: {str.Value}");
                    }
                    else if (formulaType is NumberType && double.TryParse(str.Value, out double dbl))
                    {
                        formulaValue = FormulaValue.New(dbl);
                    }
                    else if (formulaType is OptionSetValueType osvt && osvt.TryGetValue(new DName(str.Value), out OptionSetValue osv))
                    {
                        formulaValue = osv;
                    }
                }

                if (formulaValue == null)
                {
                    formulaValue = FormulaValue.New(str.Value);
                }
            }
            else if (openApiAny is OpenApiInteger intVal)
            {
                formulaValue = FormulaValue.New((decimal)intVal.Value);
            }
            else if (openApiAny is OpenApiDouble dbl)
            {
                formulaValue = FormulaValue.New((decimal)dbl.Value);
            }
            else if (openApiAny is OpenApiLong lng)
            {
                formulaValue = FormulaValue.New((decimal)lng.Value);
            }
            else if (openApiAny is OpenApiBoolean b)
            {
                formulaValue = FormulaValue.New(b.Value);
            }
            else if (openApiAny is OpenApiFloat flt)
            {
                formulaValue = FormulaValue.New((decimal)flt.Value);
            }
            else if (openApiAny is OpenApiByte by)
            {
                // OpenApi library uses Convert.FromBase64String
                formulaValue = FormulaValue.New(Convert.ToBase64String(by.Value));
            }
            else if (openApiAny is OpenApiDateTime dt && allowOpenApiDateTime)
            {
                formulaValue = FormulaValue.New(new DateTime(dt.Value.Ticks, DateTimeKind.Utc));
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
                    FormulaType newType = null;

                    if (formulaType != null)
                    {
                        if (formulaType is TableType tableType)
                        {
                            newType = tableType.ToRecord();
                        }
                    }

                    bool ba = TryGetOpenApiValue(element, newType, out FormulaValue fv);
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
                    if (TryGetOpenApiValue(kvp.Value, null, out FormulaValue fv))
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

        // Internal parameters are not showen to the user. 
        // They can have a default value or be special cased by the infrastructure (like "connectionId").
        public static bool IsInternal(this IOpenApiExtensible schema) => string.Equals(schema.GetVisibility(), "internal", StringComparison.OrdinalIgnoreCase);

        internal static string GetVisibility(this IOpenApiExtensible schema) => schema.Extensions.TryGetValue(XMsVisibility, out IOpenApiExtension openApiExt) && openApiExt is OpenApiString openApiStr ? openApiStr.Value : null;

        internal static (bool IsPresent, string Value) GetString(this OpenApiObject apiObj, string str) => apiObj.TryGetValue(str, out IOpenApiAny openApiAny) && openApiAny is OpenApiString openApiStr ? (true, openApiStr.Value) : (false, null);

        internal static void WhenPresent(this OpenApiObject apiObj, string propName, Action<string> action)
        {
            var (isPresent, value) = apiObj.GetString(propName);
            if (isPresent)
            {
                action(value);
            }
        }

        internal static void WhenPresent(this OpenApiObject apiObj, string str, Action<OpenApiObject> action)
        {
            if (apiObj.TryGetValue(str, out IOpenApiAny openApiAny) && openApiAny is OpenApiObject openApiObj)
            {
                action(openApiObj);
            }
        }

        // See https://swagger.io/docs/specification/data-models/data-types/        
        internal static ConnectorType ToConnectorType(this OpenApiParameter openApiParameter, Stack<string> chain = null, int level = 0)
        {
            chain ??= new Stack<string>();

            if (openApiParameter == null)
            {
                return new ConnectorType();
            }

            OpenApiSchema schema = openApiParameter.Schema;

            if (level == 30)
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
                            return new ConnectorType(schema, openApiParameter, FormulaType.Date);
                        case "date-time":
                        case "date-no-tz":
                            return new ConnectorType(schema, openApiParameter, FormulaType.DateTime);

                        case "binary":
                            return new ConnectorType(schema, openApiParameter, FormulaType.String, binary: true);

                        case "enum":
                            if (schema.Enum.All(e => e is OpenApiString))
                            {
                                OptionSet os = new OptionSet("enum", schema.Enum.Select(e => new DName((e as OpenApiString).Value)).ToDictionary(k => k, e => e).ToImmutableDictionary());
                                return new ConnectorType(schema, openApiParameter, os.FormulaType);
                            }
                            else
                            {
                                throw new NotImplementedException($"Unsupported enum type {schema.Enum.GetType().Name}");
                            }

                        default:
                            return new ConnectorType(schema, openApiParameter, FormulaType.String);
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
                            return new ConnectorType(schema, openApiParameter, FormulaType.Decimal);

                        case null:
                        case "decimal":
                        case "currency":
                            return new ConnectorType(schema, openApiParameter, FormulaType.Decimal);

                        default:
                            throw new NotImplementedException($"Unsupported type of number: {schema.Format}");
                    }

                // Always a boolean (Format not used)                
                case "boolean": return new ConnectorType(schema, openApiParameter, FormulaType.Boolean);

                // OpenAPI spec: Format could be <null>, int32, int64
                case "integer":
                    switch (schema.Format)
                    {
                        case null:
                        case "integer":
                        case "int32":
                            return new ConnectorType(schema, openApiParameter, FormulaType.Decimal);

                        case "int64":
                        case "unixtime":
                            return new ConnectorType(schema, openApiParameter, FormulaType.Decimal);

                        default:
                            throw new NotImplementedException($"Unsupported type of integer: {schema.Format}");
                    }

                case "array":
                    if (schema.Items == null)
                    {
                        // Type of items in unknown
                        return new ConnectorType(schema, openApiParameter, FormulaType.UntypedObject);
                    }

                    var innerA = GetUniqueIdentifier(schema.Items);

                    if (innerA.StartsWith("R:", StringComparison.Ordinal) && chain.Contains(innerA))
                    {
                        // Here, we have a circular reference and default to a string
                        return new ConnectorType(schema, openApiParameter, FormulaType.String);
                    }

                    // Inheritance/Polymorphism - Can't know the exact type
                    // https://github.com/OAI/OpenAPI-Specification/blob/main/versions/2.0.md
                    if (schema.Items.Discriminator != null)
                    {
                        return new ConnectorType(schema, openApiParameter, FormulaType.UntypedObject);
                    }

                    chain.Push(innerA);
                    ConnectorType arrayType = new OpenApiParameter() { Name = "Array", Required = true, Schema = schema.Items, Extensions = schema.Items.Extensions }.ToConnectorType(chain, level + 1);

                    chain.Pop();

                    if (arrayType.FormulaType is RecordType connectorRecordType)
                    {
                        return new ConnectorType(schema, openApiParameter, connectorRecordType.ToTable(), arrayType);
                    }
                    else if (arrayType.FormulaType is TableType tableType)
                    {
                        // Array of array 
                        TableType newTableType = new TableType(TableType.Empty().Add(TableValue.ValueName, tableType)._type);
                        return new ConnectorType(schema, openApiParameter, newTableType, arrayType);
                    }
                    else if (arrayType.FormulaType is not AggregateType)
                    {
                        // Primitives get marshalled as a SingleColumn table.
                        // Make sure this is consistent with invoker. 
                        var recordType3 = RecordType.Empty().Add(TableValue.ValueName, arrayType.FormulaType);
                        return new ConnectorType(schema, openApiParameter, recordType3.ToTable(), arrayType);
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
                        return new ConnectorType(schema, openApiParameter, FormulaType.UntypedObject);
                    }
                    else
                    {
                        RecordType recordType = RecordType.Empty();
                        RecordType hiddenRecordType = null;
                        List<ConnectorType> connectorTypes = new List<ConnectorType>();
                        List<ConnectorType> hiddenConnectorTypes = new List<ConnectorType>();

                        foreach (KeyValuePair<string, OpenApiSchema> kv in schema.Properties)
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
                                return new ConnectorType(schema, openApiParameter, FormulaType.String, hiddenRecordType);
                            }

                            chain.Push(innerO);
                            ConnectorType propertyType = new OpenApiParameter() { Name = propName, Required = schema.Required.Contains(propName), Schema = kv.Value, Extensions = kv.Value.Extensions }.ToConnectorType(chain, level + 1);
                            chain.Pop();

                            if (propertyType.HiddenRecordType != null)
                            {
                                hiddenRecordType = (hiddenRecordType ?? RecordType.Empty()).Add(propName, propertyType.HiddenRecordType);
                                hiddenConnectorTypes.Add(propertyType); // Hidden
                            }

                            if (hiddenRequired)
                            {
                                hiddenRecordType = (hiddenRecordType ?? RecordType.Empty()).Add(propName, propertyType.FormulaType);
                                hiddenConnectorTypes.Add(propertyType);
                            }
                            else
                            {
                                recordType = recordType.Add(propName, propertyType.FormulaType);
                                connectorTypes.Add(propertyType);
                            }
                        }

                        return new ConnectorType(schema, openApiParameter, recordType, hiddenRecordType, connectorTypes.ToArray(), hiddenConnectorTypes.ToArray());
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

        public static FormulaType GetReturnType(this OpenApiOperation openApiOperation)
        {
            (ConnectorType connectorType, string unsupportedReason) = openApiOperation.GetConnectorReturnType();
            FormulaType ft = connectorType?.FormulaType ?? new BlankType();
            return ft;
        }

        public static bool GetRequiresUserConfirmation(this OpenApiOperation op)
        {
            return op.Extensions.TryGetValue(XMsRequireUserConfirmation, out IOpenApiExtension openExt) && openExt is OpenApiBoolean b && b.Value;
        }

        internal static (ConnectorType ConnectorType, string UnsupportedReason) GetConnectorReturnType(this OpenApiOperation openApiOperation)
        {
            OpenApiResponses responses = openApiOperation.Responses;
            OpenApiResponse response = responses.Where(kvp => kvp.Key?.Length == 3 && kvp.Key.StartsWith("2", StringComparison.Ordinal)).OrderBy(kvp => kvp.Key).FirstOrDefault().Value;

            if (response == null)
            {
                // If no 200, but "default", use that. 
                if (!responses.TryGetValue("default", out response))
                {
                    // If no default, use the first one we find
                    response = responses.FirstOrDefault().Value;
                }
            }

            if (response == null)
            {
                // No return type. Void() method. 
                return (new ConnectorType(), null);
            }

            if (response.Content.Count == 0)
            {
                OpenApiSchema schema = new OpenApiSchema() { Type = "string", Format = "binary" };
                ConnectorType connectorType = new OpenApiParameter() { Name = "response", Required = true, Schema = schema, Extensions = response.Extensions }.ToConnectorType();
                return (connectorType, null);
            }

            // Responses is a list by content-type. Find "application/json"
            // Headers are case insensitive.
            foreach (KeyValuePair<string, OpenApiMediaType> contentKvp in response.Content)
            {
                string mediaType = contentKvp.Key.Split(';')[0];
                OpenApiMediaType openApiMediaType = contentKvp.Value;

                if (string.Equals(mediaType, ContentType_ApplicationJson, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(mediaType, ContentType_TextPlain, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(mediaType, ContentType_Any, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(mediaType, ContentType_ApplicationOctetStream, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(mediaType, ContentType_TextCsv, StringComparison.OrdinalIgnoreCase))
                {
                    if (openApiMediaType.Schema == null)
                    {
                        // Treat as void.                         
                        OpenApiSchema schema = new OpenApiSchema() { Type = "string", Format = "binary" };
                        ConnectorType cType = new OpenApiParameter() { Name = "response", Required = true, Schema = schema, Extensions = response.Extensions }.ToConnectorType();
                        return (cType, null);
                    }

                    ConnectorType connectorType = new OpenApiParameter() { Name = "response", Required = true, Schema = openApiMediaType.Schema, Extensions = openApiMediaType.Schema.Extensions }.ToConnectorType();

                    return (connectorType, null);
                }
            }

            // Returns something, but not json. 
            return (null, $"Unsupported return type - found {string.Join(", ", response.Content.Select(kv4 => kv4.Key))}");
        }

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

        public static Visibility ToVisibility(this string visibility)
        {
            return string.IsNullOrEmpty(visibility)
                ? Visibility.None
                : Enum.TryParse(visibility, true, out Visibility vis)
                ? vis
                : Visibility.Unknown;
        }

        internal static string PageLink(this OpenApiOperation op)
            => op.Extensions.TryGetValue(XMsPageable, out IOpenApiExtension ext) &&
               ext is OpenApiObject oao &&
               oao.Any() &&
               oao.First().Key == "nextLinkName" &&
               oao.First().Value is OpenApiString oas
            ? oas.Value
            : null;

        internal static string GetSummary(this IOpenApiExtensible param)
        {
            // https://learn.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions
            return param.Extensions != null && param.Extensions.TryGetValue(XMsSummary, out IOpenApiExtension ext) && ext is OpenApiString apiStr ? apiStr.Value : null;
        }

        internal static bool GetExplicitInput(this IOpenApiExtensible param)
        {
            return param.Extensions != null && param.Extensions.TryGetValue(XMsExplicitInput, out IOpenApiExtension ext) && ext is OpenApiBoolean apiBool && apiBool.Value;
        }

        // Get string content of x-ms-url-encoding parameter extension
        // Values can be "double" or "single" - https://learn.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions#x-ms-url-encoding
        internal static bool GetDoubleEncoding(this IOpenApiExtensible param)
        {
            return param.Extensions != null && param.Extensions.TryGetValue(XMsUrlEncoding, out IOpenApiExtension ext) && ext is OpenApiString apiStr && apiStr.Value.Equals("double", StringComparison.OrdinalIgnoreCase);
        }

        internal static ConnectorDynamicValue GetDynamicValue(this IOpenApiExtensible param)
        {
            // https://learn.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions#use-dynamic-values
            if (param?.Extensions != null && param.Extensions.TryGetValue(XMsDynamicValues, out IOpenApiExtension ext) && ext is OpenApiObject apiObj)
            {
                ConnectorDynamicValue cdv = new ();

                // Mandatory openrationId for connectors, except when capibility or builtInOperation are defined
                apiObj.WhenPresent("operationId", (opId) => cdv.OperationId = OpenApiHelperFunctions.NormalizeOperationId(opId));
                apiObj.WhenPresent("parameters", (opPrms) => cdv.ParameterMap = GetOpenApiObject(opPrms, true));
                apiObj.WhenPresent("value-title", (opValTitle) => cdv.ValueTitle = opValTitle);
                apiObj.WhenPresent("value-path", (opValPath) => cdv.ValuePath = opValPath);
                apiObj.WhenPresent("value-collection", (opValCollection) => cdv.ValueCollection = opValCollection);
                apiObj.WhenPresent("capability", (op_capStr) => cdv.Capability = op_capStr);
                apiObj.WhenPresent("builtInOperation", (op_bioStr) => cdv.BuiltInOperation = op_bioStr);

                if (!string.IsNullOrEmpty(cdv.BuiltInOperation) || !string.IsNullOrEmpty(cdv.Capability))
                {
                    // we don't support BuiltInOperations or capabilities right now
                    // return null to indicate that the call to get suggestions is not needed for this parameter
                    return null;
                }

                return cdv;
            }

            return null;
        }

        internal static ConnectorDynamicList GetDynamicList(this IOpenApiExtensible param)
        {
            // https://learn.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions#use-dynamic-values
            if (param?.Extensions != null && param.Extensions.TryGetValue(XMsDynamicList, out IOpenApiExtension ext) && ext is OpenApiObject apiObj)
            {
                // Mandatory openrationId for connectors
                if (apiObj.TryGetValue("operationId", out IOpenApiAny op_id) && op_id is OpenApiString opId)
                {
                    if (apiObj.TryGetValue("parameters", out IOpenApiAny op_prms) && op_prms is OpenApiObject opPrms)
                    {
                        ConnectorDynamicList cdl = new ()
                        {
                            OperationId = OpenApiHelperFunctions.NormalizeOperationId(opId.Value),
                            ParameterMap = GetOpenApiObject(opPrms)
                        };

                        if (apiObj.TryGetValue("itemTitlePath", out IOpenApiAny op_valtitle) && op_valtitle is OpenApiString opValTitle)
                        {
                            cdl.ItemTitlePath = opValTitle.Value;
                        }

                        if (apiObj.TryGetValue("itemsPath", out IOpenApiAny op_valpath) && op_valpath is OpenApiString opValPath)
                        {
                            cdl.ItemPath = opValPath.Value;
                        }

                        if (apiObj.TryGetValue("itemValuePath", out IOpenApiAny op_valcoll) && op_valcoll is OpenApiString opValCollection)
                        {
                            cdl.ItemValuePath = opValCollection.Value;
                        }

                        return cdl;
                    }
                }
                else
                {
                    throw new NotImplementedException($"Missing mandatory parameters operationId and parameters in {XMsDynamicList} extension");
                }
            }

            return null;
        }

        internal static Dictionary<string, IConnectorExtensionValue> GetOpenApiObject(this OpenApiObject opPrms, bool forceString = false)
        {
            Dictionary<string, IConnectorExtensionValue> dvParams = new ();

            foreach (KeyValuePair<string, IOpenApiAny> prm in opPrms)
            {
                if (!OpenApiExtensions.TryGetOpenApiValue(prm.Value, null, out FormulaValue fv, forceString))
                {
                    throw new NotImplementedException($"Unsupported param with OpenApi type {prm.Value.GetType().FullName}, key = {prm.Key}");
                }

                if (prm.Value is OpenApiDateTime oadt && fv is DateTimeValue dtv)
                {
                    if (forceString && prm.Key == "api-version")
                    {
                        fv = FormulaValue.New(dtv.GetConvertedValue(TimeZoneInfo.Utc).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        // A string like "2022-11-18" is interpreted as a DateTimeOffset
                        // https://github.com/microsoft/OpenAPI.NET/issues/533
                        throw new PowerFxConnectorException($"Unsupported OpenApiDateTime {oadt.Value}");
                    }
                }

                if (fv is not RecordValue rv)
                {
                    dvParams.Add(prm.Key, new StaticConnectorExtensionValue() { Value = fv });
                }
                else
                {
                    FormulaValue staticValue = rv.GetField("value");

                    if (staticValue is not BlankValue)
                    {
                        dvParams.Add(prm.Key, new StaticConnectorExtensionValue() { Value = staticValue });
                        continue;
                    }

                    FormulaValue dynamicValue = rv.GetField("parameter");

                    if (dynamicValue is BlankValue)
                    {
                        dynamicValue = rv.GetField("parameterReference");
                    }

                    if (dynamicValue is StringValue dynamicStringValue)
                    {
                        dvParams.Add(prm.Key, new DynamicConnectorExtensionValue() { Reference = dynamicStringValue.Value });
                    }
                    else
                    {
                        throw new Exception("Invalid dynamic value type for {prm.Value.GetType().FullName}, key = {prm.Key}");
                    }
                }
            }

            return dvParams;
        }

        internal static ConnectorDynamicSchema GetDynamicSchema(this IOpenApiExtensible param)
        {
            // https://learn.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions#use-dynamic-values
            if (param?.Extensions != null && param.Extensions.TryGetValue(XMsDynamicSchema, out IOpenApiExtension ext) && ext is OpenApiObject apiObj)
            {
                // Mandatory openrationId for connectors
                if (apiObj.TryGetValue("operationId", out IOpenApiAny op_id) && op_id is OpenApiString opId)
                {
                    if (apiObj.TryGetValue("parameters", out IOpenApiAny op_prms) && op_prms is OpenApiObject opPrms)
                    {
                        ConnectorDynamicSchema cds = new ()
                        {
                            OperationId = OpenApiHelperFunctions.NormalizeOperationId(opId.Value),
                            ParameterMap = GetOpenApiObject(opPrms)
                        };

                        if (apiObj.TryGetValue("value-path", out IOpenApiAny op_valpath) && op_valpath is OpenApiString opValPath)
                        {
                            cds.ValuePath = opValPath.Value;
                        }

                        return cds;
                    }
                }
                else
                {
                    throw new NotImplementedException($"Missing mandatory parameters operationId and parameters in {XMsDynamicSchema} extension");
                }
            }

            return null;
        }

        internal static ConnectorDynamicProperty GetDynamicProperty(this IOpenApiExtensible param)
        {
            // https://learn.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions#use-dynamic-values
            if (param?.Extensions != null && param.Extensions.TryGetValue(XMsDynamicProperties, out IOpenApiExtension ext) && ext is OpenApiObject apiObj)
            {
                // Mandatory openrationId for connectors
                if (apiObj.TryGetValue("operationId", out IOpenApiAny op_id) && op_id is OpenApiString opId)
                {
                    if (apiObj.TryGetValue("parameters", out IOpenApiAny op_prms) && op_prms is OpenApiObject opPrms)
                    {
                        ConnectorDynamicProperty cdp = new ()
                        {
                            OperationId = OpenApiHelperFunctions.NormalizeOperationId(opId.Value),
                            ParameterMap = GetOpenApiObject(opPrms)
                        };

                        if (apiObj.TryGetValue("itemValuePath", out IOpenApiAny op_valpath) && op_valpath is OpenApiString opValPath)
                        {
                            cdp.ItemValuePath = opValPath.Value;
                        }

                        return cdp;
                    }
                }
                else
                {
                    throw new NotImplementedException($"Missing mandatory parameters operationId and parameters in {XMsDynamicProperties} extension");
                }
            }

            return null;
        }
    }
}
