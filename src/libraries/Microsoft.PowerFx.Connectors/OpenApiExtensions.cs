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

        public static string GetBasePath(this OpenApiDocument openApiDocument, SupportsConnectorErrors errors) => GetUriElement(openApiDocument, (uri) => uri.PathAndQuery, errors);

        public static string GetScheme(this OpenApiDocument openApiDocument, SupportsConnectorErrors errors) => GetUriElement(openApiDocument, (uri) => uri.Scheme, errors);

        public static string GetAuthority(this OpenApiDocument openApiDocument, SupportsConnectorErrors errors) => GetUriElement(openApiDocument, (uri) => uri.Authority, errors);

        private static string GetUriElement(this OpenApiDocument openApiDocument, Func<Uri, string> getElement, SupportsConnectorErrors errors)
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
                case 0:
                    // None
                    return null; 
                
                case 1:
                    // This is a full URL that will pull in 'basePath' property from connectors. 
                    // Extract BasePath back out from this. 
                    var fullPath = openApiDocument.Servers[0].Url;
                    var uri = new Uri(fullPath);
                    return getElement(uri);

                default:
                    errors.AddError($"Multiple servers in OpenApiDocument is not supported");
                    return null;                    
            }
        }

        public static string GetBodyName(this OpenApiRequestBody requestBody)
        {
            return requestBody.Extensions.TryGetValue(XMsBodyName, out IOpenApiExtension value) && value is OpenApiString oas ? oas.Value : "body";
        }

        // Get suggested options values.  Returns null if none. 
        internal static (string[] options, ConnectorErrors errors) GetOptions(this OpenApiParameter openApiParameter)
        {
            ConnectorErrors errors = new ConnectorErrors();

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

                        errors.AddError($"Unrecognized {XMsEnumValues} schema ({item.GetType().Name})");                        
                    }

                    return (list.ToArray(), errors);
                }
            }

            return (null, null);
        }

        public static bool IsTrigger(this OpenApiOperation op)
        {
            // https://learn.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions#x-ms-trigger
            // Identifies whether the current operation is a trigger that produces a single event.
            // The absence of this field means this is an action operation.
            return op.Extensions.ContainsKey(XMsTrigger);
        }

        internal static bool TryGetDefaultValue(this OpenApiSchema schema, FormulaType formulaType, out FormulaValue defaultValue, SupportsConnectorErrors errors)
        {            
            if (schema.Type == "array" && formulaType is TableType tableType && schema.Items != null)
            {
                RecordType recordType = tableType.ToRecord();
                bool b = schema.Items.TryGetDefaultValue(recordType, out FormulaValue itemDefaultValue, errors);

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

                        if (schema.Properties.TryGetValue(columnName, out OpenApiSchema innerSchema))
                        {
                            if (innerSchema.TryGetDefaultValue(namedFormulaType.Type, out FormulaValue innerDefaultValue, errors))
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

            return TryGetOpenApiValue(schema.Default, formulaType, out defaultValue, errors);            
        }

        internal static bool TryGetOpenApiValue(IOpenApiAny openApiAny, FormulaType formulaType, out FormulaValue formulaValue, SupportsConnectorErrors errors, bool allowOpenApiDateTime = false)
        {
            formulaValue = null;

            if (openApiAny == null)
            {
                return false;
            }
            else if (openApiAny is OpenApiString str)
            {                
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
                        else
                        {
                            errors.AddError($"Unsupported DateTime format: {str.Value}");
                            return false;                            
                        }
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

                formulaValue ??= FormulaValue.New(str.Value);
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
                     
                    if (!TryGetOpenApiValue(element, newType, out FormulaValue fv, errors))
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
                    if (TryGetOpenApiValue(kvp.Value, null, out FormulaValue fv, errors))
                    {
                        dvParams[kvp.Key] = fv;
                    }
                }

                formulaValue = FormulaValue.NewRecordFromFields(dvParams.Select(dvp => new NamedValue(dvp.Key, dvp.Value)));
            }
            else
            {                
                errors.AddError($"Unknown default value type {openApiAny.GetType().FullName}");
                return false;                
            }

            return true;
        }

        // Internal parameters are not showen to the user. 
        // They can have a default value or be special cased by the infrastructure (like "connectionId").
        public static bool IsInternal(this IOpenApiExtensible schema) => string.Equals(schema.GetVisibility(), "internal", StringComparison.OrdinalIgnoreCase);

        internal static string GetVisibility(this IOpenApiExtensible schema) => schema.Extensions.TryGetValue(XMsVisibility, out IOpenApiExtension openApiExt) && openApiExt is OpenApiString openApiStr ? openApiStr.Value : null;

        internal static string GetMediaKind(this IOpenApiExtensible schema) => schema.Extensions.TryGetValue(XMsMediaKind, out IOpenApiExtension openApiExt) && openApiExt is OpenApiString openApiStr ? openApiStr.Value : null;

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

        internal class ConnectorTypeGetterSettings
        {
            internal readonly ConnectorCompatibility Compatibility;
            internal Stack<string> Chain = new Stack<string>();
            internal int Level = 0;            

            internal ConnectorTypeGetterSettings(ConnectorCompatibility connectorCompatibility)
            {
                Compatibility = connectorCompatibility;
            }          

            internal ConnectorTypeGetterSettings Stack(string identifier)
            {
                Level++;
                Chain.Push(identifier);
                return this;
            }

            internal void UnStack()
            { 
                Chain.Pop();
                Level--;
            }
        }

        internal static ConnectorType GetConnectorType(this OpenApiParameter openApiParameter, ConnectorCompatibility compatibility)
        {
            return openApiParameter.GetConnectorType(new ConnectorTypeGetterSettings(compatibility));
        }

        // See https://swagger.io/docs/specification/data-models/data-types/        
        internal static ConnectorType GetConnectorType(this OpenApiParameter openApiParameter, ConnectorTypeGetterSettings settings)
        {            
            if (openApiParameter == null)
            {
                return new ConnectorType(error: "OpenApiParameter is null, can't determine its schema");
            }

            OpenApiSchema schema = openApiParameter.Schema;

            if (settings.Level == 30)
            {                                
                return new ConnectorType(error: "GetConnectorType() excessive recursion");                
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
                            return new ConnectorType(schema, openApiParameter, FormulaType.String);

                        case "enum":
                            if (schema.Enum.All(e => e is OpenApiString))
                            {
                                OptionSet os = new OptionSet("enum", schema.Enum.Select(e => new DName((e as OpenApiString).Value)).ToDictionary(k => k, e => e).ToImmutableDictionary());
                                return new ConnectorType(schema, openApiParameter, os.FormulaType);
                            }
                            else
                            {
                                return new ConnectorType(error: $"Unsupported enum type {schema.Enum.GetType().Name}");                                
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
                            return new ConnectorType(error: $"Unsupported type of number: {schema.Format}");                            
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
                            return new ConnectorType(error: $"Unsupported type of integer: {schema.Format}");                            
                    }

                case "array":
                    if (schema.Items == null)
                    {
                        // Type of items in unknown
                        return new ConnectorType(schema, openApiParameter, ConnectorType.DefaultType);
                    }

                    var itemIdentifier = GetUniqueIdentifier(schema.Items);

                    if (itemIdentifier.StartsWith("R:", StringComparison.Ordinal) && settings.Chain.Contains(itemIdentifier))
                    {
                        // Here, we have a circular reference and default to a table
                        return new ConnectorType(schema, openApiParameter, TableType.Empty());
                    }

                    // Inheritance/Polymorphism - Can't know the exact type
                    // https://github.com/OAI/OpenAPI-Specification/blob/main/versions/2.0.md
                    if (schema.Items.Discriminator != null)
                    {
                        return new ConnectorType(schema, openApiParameter, ConnectorType.DefaultType);
                    }
                    
                    ConnectorType arrayType = new OpenApiParameter() { Name = "Array", Required = true, Schema = schema.Items, Extensions = schema.Items.Extensions }.GetConnectorType(settings.Stack(itemIdentifier));
                    settings.UnStack();                    

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
                        return new ConnectorType(error: $"Unsupported type of array ({arrayType.FormulaType._type})");                        
                    }

                case "object":
                case null: // xml                   

                    // Dictionary - https://swagger.io/docs/specification/data-models/dictionaries/
                    // Key is always a string, Value is in AdditionalProperties
                    if ((schema.AdditionalProperties != null && schema.AdditionalProperties.Properties.Any()) || schema.Discriminator != null)
                    {
                        return new ConnectorType(schema, openApiParameter, ConnectorType.DefaultType);
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
                                if (schema.Required.Contains(kv.Key))
                                {
                                    if (kv.Value.Default == null)
                                    {
                                        continue;
                                    }

                                    hiddenRequired = true;
                                }
                                else if (settings.Compatibility == ConnectorCompatibility.SwaggerCompatibility)
                                {
                                    continue;
                                }
                            }

                            var propName = kv.Key;
                            var schemaIdentifier = GetUniqueIdentifier(kv.Value);

                            if (schemaIdentifier.StartsWith("R:", StringComparison.Ordinal) && settings.Chain.Contains(schemaIdentifier))
                            {
                                // Here, we have a circular reference and default to a string                                
                                return new ConnectorType(schema, openApiParameter, FormulaType.String, hiddenRecordType);
                            }
                            
                            ConnectorType propertyType = new OpenApiParameter() { Name = propName, Required = schema.Required.Contains(propName), Schema = kv.Value, Extensions = kv.Value.Extensions }.GetConnectorType(settings.Stack(schemaIdentifier));
                            settings.UnStack();                            

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
                    return new ConnectorType(error: $"Unsupported schema type {schema.Type}");                    
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

        public static FormulaType GetReturnType(this OpenApiOperation openApiOperation, ConnectorCompatibility compatibility)
        {
            ConnectorType connectorType = openApiOperation.GetConnectorReturnType(compatibility);
            FormulaType ft = connectorType.HasErrors ? ConnectorType.DefaultType : connectorType?.FormulaType ?? new BlankType();
            return ft;
        }

        public static bool GetRequiresUserConfirmation(this OpenApiOperation op)
        {
            return op.Extensions.TryGetValue(XMsRequireUserConfirmation, out IOpenApiExtension openExt) && openExt is OpenApiBoolean b && b.Value;
        }

        internal static ConnectorType GetConnectorReturnType(this OpenApiOperation openApiOperation, ConnectorCompatibility compatibility)
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
                // Returns UntypedObject by default, without error
                return new ConnectorType(null);
            }

            if (response.Content.Count == 0)
            {
                OpenApiSchema schema = new OpenApiSchema() { Type = "string", Format = "binary" };
                return new OpenApiParameter() { Name = "response", Required = true, Schema = schema, Extensions = response.Extensions }.GetConnectorType(compatibility);                
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
                        return new OpenApiParameter() { Name = "response", Required = true, Schema = schema, Extensions = response.Extensions }.GetConnectorType(compatibility);                        
                    }

                    return new OpenApiParameter() { Name = "response", Required = true, Schema = openApiMediaType.Schema, Extensions = openApiMediaType.Schema.Extensions }.GetConnectorType(compatibility);                    
                }
            }
            
            return new ConnectorType(error: $"Unsupported return type - found {string.Join(", ", response.Content.Select(kv4 => kv4.Key))}");
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

        public static MediaKind ToMediaKind(this string mediaKind)
        {
            return string.IsNullOrEmpty(mediaKind)
                ? MediaKind.File
                : Enum.TryParse(mediaKind, true, out MediaKind mk)
                ? mk
                : MediaKind.Unknown;
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
                if (apiObj.TryGetValue("parameters", out IOpenApiAny op_prms) && op_prms is OpenApiObject opPrms)
                {
                    ConnectorDynamicValue cdv = new (opPrms);

                    // Mandatory operationId for connectors, except when capibility or builtInOperation are defined
                    apiObj.WhenPresent("operationId", (opId) => cdv.OperationId = OpenApiHelperFunctions.NormalizeOperationId(opId));                    
                    apiObj.WhenPresent("value-title", (opValTitle) => cdv.ValueTitle = opValTitle);
                    apiObj.WhenPresent("value-path", (opValPath) => cdv.ValuePath = opValPath);
                    apiObj.WhenPresent("value-collection", (opValCollection) => cdv.ValueCollection = opValCollection);
                     
                    // we don't support BuiltInOperations or capabilities right now
                    // return null to indicate that the call to get suggestions is not needed for this parameter
                    apiObj.WhenPresent("capability", (string op_capStr) => cdv = null);
                    apiObj.WhenPresent("builtInOperation", (string op_bioStr) => cdv = null);

                    return cdv;
                }
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
                        ConnectorDynamicList cdl = new (opPrms)
                        {
                            OperationId = OpenApiHelperFunctions.NormalizeOperationId(opId.Value),                            
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
                    return new ConnectorDynamicList($"Missing mandatory parameters operationId and parameters in {XMsDynamicList} extension");                    
                }
            }

            return null;
        }

        internal static Dictionary<string, IConnectorExtensionValue> GetParameterMap(this OpenApiObject opPrms, SupportsConnectorErrors errors, bool forceString = false)
        {
            Dictionary<string, IConnectorExtensionValue> dvParams = new ();            

            foreach (KeyValuePair<string, IOpenApiAny> prm in opPrms)
            {                
                if (!TryGetOpenApiValue(prm.Value, null, out FormulaValue fv, errors, forceString))
                {                    
                    errors.AddError($"Unsupported param with OpenApi type {prm.Value.GetType().FullName}, key = {prm.Key}");
                    continue;                    
                }

                if (prm.Value is OpenApiDateTime oadt && fv is DateTimeValue dtv)
                {
                    // https://github.com/microsoft/OpenAPI.NET/issues/533
                    // https://github.com/microsoft/Power-Fx/pull/1987 - https://github.com/microsoft/Power-Fx/issues/1982
                    // api-version, x-ms-api-version, X-GitHub-Api-Version...
                    if (forceString && prm.Key.EndsWith("api-version", StringComparison.OrdinalIgnoreCase))
                    {
                        fv = FormulaValue.New(dtv.GetConvertedValue(TimeZoneInfo.Utc).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        // A string like "2022-11-18" is interpreted as a DateTimeOffset
                        // https://github.com/microsoft/OpenAPI.NET/issues/533                        
                        errors.AddError($"Unsupported OpenApiDateTime {oadt.Value}");
                        continue;                        
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
                        errors.AddError($"Invalid dynamic value type for {prm.Value.GetType().FullName}, key = {prm.Key}");                        
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
                        ConnectorDynamicSchema cds = new (opPrms)
                        {
                            OperationId = OpenApiHelperFunctions.NormalizeOperationId(opId.Value),                            
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
                    return new ConnectorDynamicSchema(error: $"Missing mandatory parameters operationId and parameters in {XMsDynamicSchema} extension");                    
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
                        ConnectorDynamicProperty cdp = new (opPrms)
                        {
                            OperationId = OpenApiHelperFunctions.NormalizeOperationId(opId.Value),                            
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
                    return new ConnectorDynamicProperty(error: $"Missing mandatory parameters operationId and parameters in {XMsDynamicProperties} extension");                    
                }
            }

            return null;
        }
    }
}
