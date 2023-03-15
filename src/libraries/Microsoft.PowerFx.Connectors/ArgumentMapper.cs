// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AppMagic.Authoring;
using Microsoft.AppMagic.Authoring.Texl.Builtins;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Connectors.ArgumentMapper;
using Contracts = Microsoft.PowerFx.Core.Utils.Contracts;

namespace Microsoft.PowerFx.Connectors
{
    // Map arguments between a Swagger File and a Function signature. 
    // Deals with: 
    //  - internals, "connectionId"
    //  - optional, required, default value 
    //  - computing signature type
    //
    // Given a swagger definition, handles:
    //  - signature information to create a ServiceFunction
    //  - reverse mapping to map from function signature back to swagger.
    internal class ArgumentMapper
    {
        // All connectors have an internal parameter named connectionId. 
        // This is handled specially and value passed by connector.         
        public const string DefaultBodyParameter = "body";
        public List<OpenApiParameter> OpenApiParameters;
        public List<OpenApiParameter> OpenApiBodyParameters;
        public bool SchemaLessBody = false;

        #region ServiceFunction args

        // Useful for passing into a ServiceFunction
        public readonly ServiceFunctionParameterTemplate[] RequiredParamInfo;
        public readonly ServiceFunctionParameterTemplate[] HiddenRequiredParamInfo;
        public readonly ServiceFunctionParameterTemplate[] OptionalParamInfo;
        public readonly DType[] _parameterTypes; // length of ArityMax        
        public readonly string ContentType;
        public readonly string ReferenceId;
        public readonly int ArityMin;
        public readonly int ArityMax;
        public readonly OpenApiOperation Operation;

        private readonly Dictionary<string, (FormulaValue, DType)> _parameterDefaultValues = new ();
        private readonly Dictionary<TypedName, List<string>> _parameterOptions = new ();
        #endregion // ServiceFunction args

        public bool HasBodyParameter => Operation.RequestBody != null;        

        public ArgumentMapper(IEnumerable<OpenApiParameter> parameters, OpenApiOperation operation)
        {
            OpenApiParameters = parameters.ToList();
            OpenApiBodyParameters = new List<OpenApiParameter>();
            Operation = operation;
            ContentType = OpenApiExtensions.ContentType_ApplicationJson; // default

            // Hidden-Required parameters exist in the following conditions:
            // 1. required parameter
            // 2. has default value
            // 3. is marked "internal" in schema extension named "x-ms-visibility"

            List<ConnectorParameterInternal> requiredParams = new ();
            List<ConnectorParameterInternal> hiddenRequiredParams = new ();
            List<ConnectorParameterInternal> optionalParams = new ();

            List<KeyValuePair<string, ConnectorSchemaInternal>> requiredBodyParams = new ();
            List<KeyValuePair<string, ConnectorSchemaInternal>> hiddenRequiredBodyParams = new ();
            List<KeyValuePair<string, ConnectorSchemaInternal>> optionalBodyParams = new ();

            foreach (OpenApiParameter param in OpenApiParameters)
            {
                bool hiddenRequired = false;

                if (param.IsInternal())
                {
                    if (param.Required && param.Schema.Default != null)
                    {
                        // Ex: Api-Version (but not ConnectionId as it doesn't have a default value)
                        hiddenRequired = true;
                    }
                    else
                    {
                        // "Internal" params aren't shown in the signature.                     
                        continue;
                    }
                }

                string name = param.Name;
                (FormulaType paramType, RecordType hiddenRecordType) = param.Schema.ToFormulaType();

                // Process x-ms-dynamic-values extension
                ConnectorDynamicValue connectorDynamicValue = GetDynamicValue(param);

                if (hiddenRecordType != null)
                {
                    throw new NotImplementedException("Unexpected value for a parameter");
                }

                HttpFunctionInvoker.VerifyCanHandle(param.In);

                if (param.Schema.TryGetDefaultValue(paramType, out FormulaValue defaultValue))
                {
                    _parameterDefaultValues[name] = (defaultValue, paramType._type);
                }

                if (param.Required)
                {
                    if (hiddenRequired)
                    {
                        hiddenRequiredParams.Add(new ConnectorParameterInternal(param, paramType, connectorDynamicValue));
                    }
                    else
                    {
                        requiredParams.Add(new ConnectorParameterInternal(param, paramType, connectorDynamicValue));
                    }
                }
                else
                {
                    optionalParams.Add(new ConnectorParameterInternal(param, paramType, connectorDynamicValue));
                }

                string[] options = param.GetOptions();
                if (options != null)
                {
                    TypedName typedName = new TypedName(paramType._type, new DName(name));
                    _parameterOptions[typedName] = new List<string>(options);
                }
            }

            if (HasBodyParameter)
            {
                // We don't support x-ms-dynamic-values in "body" parameters for now (is that possible?)
                OpenApiRequestBody requestBody = operation.RequestBody;
                string bodyName = requestBody.GetBodyName() ?? DefaultBodyParameter;
                OpenApiParameter bodyParameter;

                if (requestBody.Content != null && requestBody.Content.Any())
                {
                    (string contentType, OpenApiMediaType mediaType) = requestBody.Content.GetContentTypeAndSchema();

                    if (!string.IsNullOrEmpty(contentType) && mediaType != null)
                    {
                        OpenApiSchema schema = mediaType.Schema;

                        ContentType = contentType;
                        ReferenceId = schema?.Reference?.Id;

                        // Additional properties are ignored for now
                        if (schema.AnyOf.Any() || schema.Not != null || (schema.Items != null && schema.Type != "array"))
                        {
                            throw new NotImplementedException($"OpenApiSchema is not supported - AnyOf, Not, AdditionalProperties or Items not array");
                        }
                        else if (schema.AllOf.Any() || schema.Properties.Any())
                        {
                            // We allow AllOf to be present             
                            foreach (KeyValuePair<string, OpenApiSchema> prop in schema.Properties)
                            {
                                bool required = schema.Required.Contains(prop.Key);
                                bool hiddenRequired = false;

                                if (prop.Value.IsInternal())
                                {
                                    if (required && prop.Value.Default != null)
                                    {
                                        hiddenRequired = true;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }

                                bodyParameter = new OpenApiParameter() { Schema = prop.Value, Name = prop.Key, Description = "Body", Required = required };
                                OpenApiBodyParameters.Add(bodyParameter);

                                (FormulaType formulaType, RecordType hiddenFormulaType) = prop.Value.ToFormulaType();
                                (hiddenRequired ? hiddenRequiredBodyParams : required ? requiredBodyParams : optionalBodyParams).Add(new KeyValuePair<string, ConnectorSchemaInternal>(prop.Key, new ConnectorSchemaInternal(prop.Value, formulaType, null)));

                                if (hiddenFormulaType != null)
                                {
                                    hiddenRequiredBodyParams.Add(new KeyValuePair<string, ConnectorSchemaInternal>(prop.Key, new ConnectorSchemaInternal(prop.Value, hiddenFormulaType, null)));
                                }
                            }
                        }
                        else
                        {                            
                            SchemaLessBody = true;
                            bodyParameter = new OpenApiParameter() { Schema = schema, Name = bodyName, Description = "Body", Required = requestBody.Required };

                            OpenApiBodyParameters.Add(bodyParameter);
                            (FormulaType formulaType, RecordType hiddenRecordType) = schema.ToFormulaType();
                            (requestBody.Required ? requiredParams : optionalParams).Add(new ConnectorParameterInternal(bodyParameter, formulaType, null));

                            if (hiddenRecordType != null)
                            {
                                throw new NotImplementedException("Unexpected value for schema-less body");
                            }
                        }
                    }
                }
                else
                {
                    // If the content isn't specified, we will expect Json in the body
                    ContentType = OpenApiExtensions.ContentType_ApplicationJson;                    
                    bodyParameter = new OpenApiParameter() { Schema = new OpenApiSchema() { Type = "string" }, Name = bodyName, Description = "Body", Required = requestBody.Required };

                    OpenApiBodyParameters.Add(bodyParameter);
                    (requestBody.Required ? requiredParams : optionalParams).Add(new ConnectorParameterInternal(bodyParameter, FormulaType.String, null));
                }
            }

            RequiredParamInfo = requiredParams.ConvertAll(x => Convert(x)).Union(requiredBodyParams.ConvertAll(x => Convert(x))).ToArray();
            HiddenRequiredParamInfo = hiddenRequiredParams.ConvertAll(x => Convert(x)).Union(hiddenRequiredBodyParams.ConvertAll(x => Convert(x))).ToArray();
            OptionalParamInfo = optionalParams.ConvertAll(x => Convert(x)).Union(optionalBodyParams.ConvertAll(x => Convert(x))).ToArray();

            // Required params are first N params in the final list. 
            // Optional params are fields on a single record argument at the end.
            // Hidden required parameters do not count here
            ArityMin = RequiredParamInfo.Length;
            ArityMax = ArityMin + (OptionalParamInfo.Length == 0 ? 0 : 1);

            _parameterTypes = GetParamTypes(RequiredParamInfo, OptionalParamInfo).ToArray();
        }

        // Usfeul for invoking.
        // Arguments are all positional. 
        // Returns a map of name to FormulaValue. 
        // The name map can then be applied back to the swagger definition for invoking. 
        public Dictionary<string, FormulaValue> ConvertToNamedParameters(FormulaValue[] args)
        {
            // Type check should have caught this.
            Contracts.Assert(args.Length >= ArityMin);
            Contracts.Assert(args.Length <= ArityMax);

            // First N are required params. 
            // Last param is a record with each field being an optional.

            Dictionary<string, FormulaValue> map = new (StringComparer.OrdinalIgnoreCase);

            // Seed with default values. This will get over written if provided. 
            foreach (KeyValuePair<string, (FormulaValue, DType)> kv in _parameterDefaultValues)
            {
                map[kv.Key] = kv.Value.Item1;
            }

            foreach (ServiceFunctionParameterTemplate param in HiddenRequiredParamInfo)
            {
                map[param.TypedName.Name] = param.DefaultValue;
            }

            // Required parameters are always first
            for (int i = 0; i < RequiredParamInfo.Length; i++)
            {
                DName parameterName = RequiredParamInfo[i].TypedName.Name;
                FormulaValue value = args[i];

                // Objects are always flattenned                
                if (value is RecordValue record && !(RequiredParamInfo[i].Description == "Body"))
                {
                    foreach (NamedValue field in record.Fields)
                    {
                        map.Add(field.Name, field.Value);
                    }
                }
                else if (!map.ContainsKey(parameterName))
                {
                    map.Add(parameterName, value);
                }
                else if (value is RecordValue r)
                {
                    map[parameterName] = MergeRecords(map[parameterName] as RecordValue, r);
                }
            }

            // Optional parameters are next and stored in a Record
            if (OptionalParamInfo.Length > 0 && args.Length > RequiredParamInfo.Length)
            {
                FormulaValue optionalArg = args[args.Length - 1];

                // Objects are always flattenned
                if (optionalArg is RecordValue record)
                {
                    foreach (NamedValue field in record.Fields)
                    {
                        map.Add(field.Name, field.Value);
                    }
                }
                else
                {
                    // Type check should have caught this. 
                    throw new InvalidOperationException($"Optional arg must be the last arg and a record");
                }
            }

            return map;
        }

        internal static RecordValue MergeRecords(RecordValue rv1, RecordValue rv2)
        {
            if (rv1 == null)
            {
                throw new ArgumentNullException(nameof(rv1));
            }

            if (rv2 == null)
            {
                throw new ArgumentNullException(nameof(rv2));
            }

            List<NamedValue> lst = rv1.Fields.ToList();

            foreach (NamedValue field2 in rv2.Fields)
            {
                NamedValue field1 = lst.FirstOrDefault(f1 => f1.Name == field2.Name);

                if (field1 == null)
                {
                    lst.Add(field2);
                }
                else
                {
                    if (field1.Value is RecordValue r1 && field2.Value is RecordValue r2)
                    {
                        RecordValue rv3 = MergeRecords(r1, r2);
                        lst.Remove(field1);
                        lst.Add(new NamedValue(field1.Name, rv3));
                    }
                    else if (field1.Value.GetType() == field2.Value.GetType())
                    {
                        lst.Remove(field1);
                        lst.Add(field2);
                    }
                    else
                    {
                        throw new ArgumentException($"Cannot merge {field1.Name} of type {field1.Value.GetType().Name} with {field2.Name} of type {field2.Value.GetType().Name}");
                    }
                }
            }

            RecordType rt = RecordType.Empty();

            foreach (NamedValue nv in lst)
            {
                rt = rt.Add(nv.Name, nv.Value.Type);
            }

            return new InMemoryRecordValue(IRContext.NotInSource(rt), lst);
        }

        private static ConnectorDynamicValue GetDynamicValue(OpenApiParameter param)
        {
            // https://learn.microsoft.com/en-us/connectors/custom-connectors/openapi-extensions#use-dynamic-values
            if (param.Extensions != null && param.Extensions.TryGetValue("x-ms-dynamic-values", out IOpenApiExtension ext) && ext is OpenApiObject apiObj)
            {
                // We don't support "capability" parameter which is present in Azure Blob storage swagger
                // There is no "operationId" in this case and we don't manage this for now
                if (apiObj.TryGetValue("capability", out IOpenApiAny cap))
                {
                    return null;
                }

                // Mandatory openrationId for connectors
                if (apiObj.TryGetValue("operationId", out IOpenApiAny op_id) && op_id is OpenApiString opId)
                {
                    if (apiObj.TryGetValue("parameters", out IOpenApiAny op_prms) && op_prms is OpenApiObject opPrms)
                    {
                        Dictionary<string, string> dvParams = new ();

                        foreach (KeyValuePair<string, IOpenApiAny> prm in opPrms)
                        {
                            // dynamic parameter
                            if (prm.Value is OpenApiObject prmStr)
                            {
                                if (prmStr.Count != 1)
                                {
                                    throw new NotImplementedException($"Not expecting more than one parameter string per parameter");
                                }

                                if (prmStr.First().Value is OpenApiString prmStr2)
                                {
                                    dvParams.Add(prm.Key, prmStr2.Value);
                                }
                                else
                                {
                                    throw new NotImplementedException($"Unsupported OpenApi inner type {prmStr.First().Value.GetType().FullName}");
                                }
                            }
                            else
                            {
                                // We do not support static parameters for now
                                throw new NotImplementedException($"Unsupported static param with OpenApi type {prm.Value.GetType().FullName}");
                            }
                        }

                        ConnectorDynamicValue cdv = new ()
                        {
                            OperationId = OpenApiHelperFunctions.NormalizeOperationId(opId.Value),
                            ParameterMap = dvParams
                        };

                        if (apiObj.TryGetValue("value-title", out IOpenApiAny op_valtitle) && op_valtitle is OpenApiString opValTitle)
                        {
                            cdv.ValueTitle = opValTitle.Value;
                        }

                        if (apiObj.TryGetValue("value-path", out IOpenApiAny op_valpath) && op_valpath is OpenApiString opValPath)
                        {
                            cdv.ValuePath = opValPath.Value;
                        }

                        if (apiObj.TryGetValue("value-collection", out IOpenApiAny op_valcoll) && op_valcoll is OpenApiString opValCollection)
                        {
                            cdv.ValueCollection = opValCollection.Value;
                        }

                        return cdv;
                    }
                }
                else
                {
                    throw new NotImplementedException("Missing mandatory parameters operationId and parameters in x-ms-dynamic-values extension");
                }
            }

            // x-ms-dynamic-list :: To be implemented later
            // x-ms-dynamic-schema :: To be implemented later
            // x-ms-dynamic-properties :: To be implemented later

            return null;
        }

        internal class ConnectorDynamicValue
        {
            public string OperationId;

            public Dictionary<string, string> ParameterMap;

            public string ValueTitle = null;

            public string ValuePath = null;

            public string ValueCollection = null;

            public ServiceFunction ServiceFunction;
        }

        private static IEnumerable<DType> GetParamTypes(ServiceFunctionParameterTemplate[] requiredParameters, ServiceFunctionParameterTemplate[] optionalParameters)
        {
            Contracts.AssertValue(requiredParameters);

            IEnumerable<DType> parameterTypes = requiredParameters.Select(parameter => parameter.TypedName.Type);
            if (optionalParameters.Length > 0)
            {
                DType optionalParameterType = DType.CreateRecord(optionalParameters.Select(kvp => kvp.TypedName));
                optionalParameterType.AreFieldsOptional = true;

                return parameterTypes.Append(optionalParameterType);
            }

            return parameterTypes;
        }

        private static ServiceFunctionParameterTemplate Convert(ConnectorParameterInternal internalParameter)
        {
            DType paramType = internalParameter.Type._type;
            TypedName typedName = new TypedName(paramType, new DName(internalParameter.OpenApiParameter.Name));

            internalParameter.OpenApiParameter.Schema.TryGetDefaultValue(internalParameter.Type, out FormulaValue defaultValue);

            return new ServiceFunctionParameterTemplate(internalParameter.Type, typedName, internalParameter.OpenApiParameter.Description, defaultValue, internalParameter.DynamicValue);
        }

        private static ServiceFunctionParameterTemplate Convert(KeyValuePair<string, ConnectorSchemaInternal> internalParameter)
        {
            DType paramType = internalParameter.Value.Type._type;
            TypedName typedName = new TypedName(paramType, new DName(internalParameter.Key));

            internalParameter.Value.Schema.TryGetDefaultValue(internalParameter.Value.Type, out FormulaValue defaultValue);

            return new ServiceFunctionParameterTemplate(internalParameter.Value.Type, typedName, "Body", defaultValue, internalParameter.Value.DynamicValue);
        }
    }

    internal class ConnectorParameterInternal : ConnectorSchemaInternal
    {
        public OpenApiParameter OpenApiParameter { get; }       

        public ConnectorParameterInternal(OpenApiParameter openApiParameter, FormulaType type, ConnectorDynamicValue dynamicValue)
            : base(openApiParameter.Schema, type, dynamicValue)
        {
            OpenApiParameter = openApiParameter;           
        }
    }

    internal class ConnectorSchemaInternal
    {
        public OpenApiSchema Schema { get; set; }

        public FormulaType Type { get; }

        public ConnectorDynamicValue DynamicValue { get; }

        public ConnectorSchemaInternal(OpenApiSchema schema, FormulaType type, ConnectorDynamicValue dynamicValue)
        {
            Schema = schema;
            Type = type;
            DynamicValue = dynamicValue;
        }
    }
}
