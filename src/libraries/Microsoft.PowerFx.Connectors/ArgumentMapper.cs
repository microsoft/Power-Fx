// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AppMagic.Authoring;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
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
        private const string ConnectionIdParamName = "connectionId";
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

        private readonly Dictionary<string, (FormulaValue, DType)> _parameterDefaultValues = new();
        private readonly Dictionary<TypedName, List<string>> _parameterOptions = new();
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

            var requiredParams = new List<(OpenApiParameter, FormulaType)>();
            var hiddenRequiredParams = new List<(OpenApiParameter, FormulaType)>();
            var optionalParams = new List<(OpenApiParameter, FormulaType)>();

            var requiredBodyParams = new List<KeyValuePair<string, (OpenApiSchema, FormulaType)>>();
            var hiddenRequiredBodyParams = new List<KeyValuePair<string, (OpenApiSchema, FormulaType)>>();
            var optionalBodyParams = new List<KeyValuePair<string, (OpenApiSchema, FormulaType)>>();

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

                var name = param.Name;
                (FormulaType paramType, RecordType hiddenRecordType) = param.Schema.ToFormulaType();

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
                        hiddenRequiredParams.Add((param, paramType));
                    }
                    else
                    {
                        requiredParams.Add((param, paramType));
                    }
                }
                else
                {
                    optionalParams.Add((param, paramType));
                }

                var options = param.GetOptions();
                if (options != null)
                {
                    var typedName = new TypedName(paramType._type, new DName(name));
                    _parameterOptions[typedName] = new List<string>(options);
                }
            }

            if (HasBodyParameter)
            {
                var requestBody = operation.RequestBody;
                var bodyName = requestBody.GetBodyName() ?? DefaultBodyParameter;
                OpenApiParameter bodyParameter;

                if (requestBody.Content != null && requestBody.Content.Any())
                {
                    var ct = requestBody.Content.GetContentTypeAndSchema();

                    if (!string.IsNullOrEmpty(ct.ContentType) && ct.MediaType != null)
                    {
                        var schema = ct.MediaType.Schema;

                        ContentType = ct.ContentType;
                        ReferenceId = schema?.Reference?.Id;

                        if (schema.AnyOf.Any() || schema.Not != null || schema.AdditionalProperties != null || (schema.Items != null && schema.Type != "array"))
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
                                (hiddenRequired ? hiddenRequiredBodyParams : required ? requiredBodyParams : optionalBodyParams).Add(new KeyValuePair<string, (OpenApiSchema, FormulaType)>(prop.Key, (prop.Value, formulaType)));

                                if (hiddenFormulaType != null)
                                {
                                    hiddenRequiredBodyParams.Add(new KeyValuePair<string, (OpenApiSchema, FormulaType)>(prop.Key, (prop.Value, hiddenFormulaType)));
                                }
                            }
                        }
                        else
                        {
                            SchemaLessBody = true;
                            bodyParameter = new OpenApiParameter() { Schema = schema, Name = bodyName, Description = "Body", Required = requestBody.Required };

                            OpenApiBodyParameters.Add(bodyParameter);
                            (FormulaType formulaType, RecordType hiddenRecordType) = schema.ToFormulaType();
                            (requestBody.Required ? requiredParams : optionalParams).Add((bodyParameter, formulaType));

                            if (hiddenRecordType != null)
                            {
                                throw new NotImplementedException("Unexpected value for schema-less body");
                            }
                        }
                    }
                }
                else
                {
                    // If the content isn't specified, we will expect a string in the body
                    ContentType = OpenApiExtensions.ContentType_TextPlain;
                    bodyParameter = new OpenApiParameter() { Schema = new OpenApiSchema() { Type = "string" }, Name = bodyName, Description = "Body", Required = requestBody.Required };

                    OpenApiBodyParameters.Add(bodyParameter);
                    (requestBody.Required ? requiredParams : optionalParams).Add((bodyParameter, FormulaType.String));
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

            var map = new Dictionary<string, FormulaValue>(StringComparer.OrdinalIgnoreCase);

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
            for (var i = 0; i < RequiredParamInfo.Length; i++)
            {
                var parameterName = RequiredParamInfo[i].TypedName.Name;
                var value = args[i];

                // Objects are always flattenned
                if (value is RecordValue record && !(RequiredParamInfo[i].Description == "Body"))
                {
                    foreach (var field in record.Fields)
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
                var optionalArg = args[args.Length - 1];

                // Objects are always flattenned
                if (optionalArg is RecordValue record)
                {
                    foreach (var field in record.Fields)
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

        private static IEnumerable<DType> GetParamTypes(ServiceFunctionParameterTemplate[] requiredParameters, ServiceFunctionParameterTemplate[] optionalParameters)
        {
            Contracts.AssertValue(requiredParameters);

            var parameters = requiredParameters.Select(parameter => parameter.TypedName.Type);
            if (optionalParameters.Length > 0)
            {
                var optionalParameterType = DType.CreateRecord(optionalParameters.Select(kvp => kvp.TypedName));
                optionalParameterType.AreFieldsOptional = true;
                return parameters.Append(optionalParameterType);
            }

            return parameters;
        }

        private static ServiceFunctionParameterTemplate Convert((OpenApiParameter apiParam, FormulaType fType) param)
        {
            var paramType = param.fType._type;
            var typedName = new TypedName(paramType, new DName(param.apiParam.Name));

            param.apiParam.Schema.TryGetDefaultValue(param.fType, out FormulaValue defaultValue);

            return new ServiceFunctionParameterTemplate(param.fType, typedName, param.apiParam.Description, defaultValue);
        }

        private static ServiceFunctionParameterTemplate Convert(KeyValuePair<string, (OpenApiSchema apiParam, FormulaType fType)> apiProperty)
        {
            var paramType = apiProperty.Value.fType._type;
            var typedName = new TypedName(paramType, new DName(apiProperty.Key));

            apiProperty.Value.apiParam.TryGetDefaultValue(apiProperty.Value.fType, out FormulaValue defaultValue);

            return new ServiceFunctionParameterTemplate(apiProperty.Value.fType, typedName, "Body", defaultValue);
        }
    }
}
