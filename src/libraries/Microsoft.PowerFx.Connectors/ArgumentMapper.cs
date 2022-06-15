// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AppMagic.Authoring;
using Microsoft.OpenApi.Models;
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
        public const string BodyParameter = "body";

        public List<FxOpenApiParameter> OpenApiParameters;

        #region ServiceFunction args

        // Useful for passing into a ServiceFunction
        public readonly ServiceFunctionParameterTemplate[] RequiredParamInfo;
        public readonly ServiceFunctionParameterTemplate[] OptionalParamInfo;
        public readonly DType[] _parameterTypes; // length of ArityMax        
        public readonly string ContentType;
        public readonly int ArityMin;
        public readonly int ArityMax;

        private readonly Dictionary<string, Tuple<string, DType>> _parameterDefaultValues = new ();
        private readonly Dictionary<TypedName, List<string>> _parameterOptions = new ();        
        #endregion // ServiceFunction args

        public ArgumentMapper(IEnumerable<OpenApiParameter> parameters, OpenApiRequestBody requestBody)
        {
            OpenApiParameters = parameters.Select(p => p.ToFxOpenApiParameter()).ToList();
            ContentType = "application/json"; // default

            var requiredParams = new List<FxOpenApiParameter>();
            var optionalParams = new List<FxOpenApiParameter>();

            foreach (var param in OpenApiParameters)
            {
                var name = param.Name;

                HttpFunctionInvoker.VerifyCanHandle(param.In);

                var paramType = param.Schema.ToFormulaType();

                if (param.TryGetDefaultValue(out var defaultValue))
                {
                    _parameterDefaultValues[name] = Tuple.Create(defaultValue, paramType._type);
                }

                if (param.IsInternal())
                {
                    // "Internal" params aren't shown in the signature. So we need some way of knowing what they are.
                    // connectorId is a special-cases internal parameter, the channel will stamp it.
                    // Else it has a default value. 
                    if (name == ConnectionIdParamName || param.HasDefaultValue())
                    {
                        continue;
                    }
                    else
                    {
                        throw new NotImplementedException($"Unexpected internal param {name}");
                    }
                }

                if (param.Required)
                {
                    requiredParams.Add(param);
                }
                else
                {
                    optionalParams.Add(param);
                }

                var options = param.GetOptions();
                if (options != null)
                {
                    var typedName = new TypedName(paramType._type, new DName(name));
                    _parameterOptions[typedName] = new List<string>(options);
                }
            }

            if (requestBody != null)
            {
                var bodyName = requestBody.GetBodyName() ?? BodyParameter;
                FxOpenApiParameter bodyParameter;

                if (requestBody.Content != null && requestBody.Content.Any())
                {
                    var ct = requestBody.Content.GetContentTypeAndSchema();
                    var schema = ct.Value.Schema;

                    ContentType = ct.Key;

                    if (schema.AllOf.Any() || schema.AnyOf.Any() || schema.Not != null || schema.Items != null || schema.AdditionalProperties != null)
                    {
                        throw new NotImplementedException($"OpenApiSchema is not supported");
                    }
                    else
                    {                        
                        var bodyType = schema.ToFormulaType();

                        bodyParameter = new FxOpenApiParameter(schema, bodyName, string.Empty, FxParameterLocation.Body, requestBody.Required);
                    }
                }
                else
                {
                    // If the content isn't specified, we will expect a string in the body                    
                    bodyParameter = new FxOpenApiParameter(new OpenApiSchema() { Type = "string" }, bodyName, string.Empty, FxParameterLocation.Body, requestBody.Required);                    
                }

                OpenApiParameters.Add(bodyParameter);
                (requestBody.Required ? requiredParams : optionalParams).Add(bodyParameter);
            }

            OptionalParamInfo = optionalParams.ConvertAll(x => Convert(x)).ToArray();
            RequiredParamInfo = requiredParams.ConvertAll(x => Convert(x)).ToArray();

            // Required params are first N params in the final list. 
            // Optional params are fields on a single record argument at the end.
            ArityMin = requiredParams.Count;
            ArityMax = ArityMin + (optionalParams.Count == 0 ? 0 : 1);

            _parameterTypes = GetParamTypes(RequiredParamInfo, OptionalParamInfo).ToArray();
        }

        // Usfeul for invoking.
        // Arguments are all positional. 
        // Returns a map of name to FormulaValue. 
        // The name map can then be applied back to the swagger definition for invoking. 
        public Dictionary<string, FormulaValue> ConvertToSwagger(FormulaValue[] args)
        {
            // Type check should have caught this.
            Contracts.Assert(args.Length >= ArityMin);
            Contracts.Assert(args.Length <= ArityMax);

            // First N are required params. 
            // Last param is a record with each field being an optional.

            var map = new Dictionary<string, FormulaValue>(StringComparer.OrdinalIgnoreCase);

            // Seed with default values. This will get over written if provided. 
            foreach (var kv in _parameterDefaultValues)
            {
                var name = kv.Key;
                var value = kv.Value.Item1.ToString();
                map[name] = FormulaValue.New(value);
            }

            for (var i = 0; i < RequiredParamInfo.Length; i++)
            {
                var name = RequiredParamInfo[i].TypedName.Name;
                var value = args[i];

                map[name] = value;
            }

            if (OptionalParamInfo.Length > 0 && args.Length > 0)
            {
                var optionalArg = args[args.Length - 1];

                if (optionalArg is RecordValue record)
                {
                    foreach (var field in record.Fields)
                    {
                        map[field.Name] = field.Value;
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

        private static ServiceFunctionParameterTemplate Convert(FxOpenApiParameter apiParam)
        {
            var paramType = apiParam.Schema.ToFormulaType()._type;
            var typedName = new TypedName(paramType, new DName(apiParam.Name));

            apiParam.TryGetDefaultValue(out var defaultValue);

            return new ServiceFunctionParameterTemplate(
                typedName,
                apiParam.Description,
                defaultValue);
        }
    }
}
