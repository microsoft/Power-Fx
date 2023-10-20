﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors.Execution
{
    public abstract class FunctionInvoker    
    {
        public BaseRuntimeConnectorContext Context { get; }        

        internal IConvertToUTC UtcConverter { get; }

        public ConnectorLogger Logger => Context.ExecutionLogger;

        public ConnectorFunction Function { get; }

        public bool ThrowOnError => Context.ThrowOnError;      

        public FunctionInvoker(ConnectorFunction function, BaseRuntimeConnectorContext runtimeContext)
        {
            Function = function;
            Context = runtimeContext;            
            UtcConverter = new ConvertToUTC(Context.TimeZoneInfo);
        }

        public abstract Task<FormulaValue> SendAsync(InvokerParameters invokerElements, CancellationToken cancellationToken);

        public async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            FunctionInvoker invoker = Context.GetInvoker(Function);
            InvokerParameters invokerParameters = GetRequestElements(Function, args, invoker is HttpFunctionInvoker hfi && hfi._invoker is HttpClient hc ? hc.BaseAddress : null, cancellationToken);

            if (invokerParameters == null)
            {
                Logger?.LogError($"In {nameof(HttpFunctionInvoker)}.{nameof(InvokeAsync)} request is null");
                return new ErrorValue(IRContext.NotInSource(Function.ReturnType), new ExpressionError()
                {
                    Kind = ErrorKind.Internal,
                    Severity = ErrorSeverity.Critical,
                    Message = $"In {nameof(HttpFunctionInvoker)}.{nameof(InvokeAsync)} request is null"
                });
            }

            return await SendAsync(invokerParameters, cancellationToken).ConfigureAwait(false);            
        }

        public async Task<FormulaValue> InvokeAsync(string nextlink, CancellationToken cancellationToken)
        {
            FunctionInvoker invoker = Context.GetInvoker(Function);
            InvokerParameters invokerParameters = new InvokerParameters()
            {
                QueryType = QueryType.NextPage,
                HttpMethod = Function.HttpMethod,
                Url = new Uri(nextlink).PathAndQuery,
                Body = null,
                ContentType = Function._internals.ContentType,
                Headers = new Dictionary<string, string>()
            };

            return await SendAsync(invokerParameters, cancellationToken).ConfigureAwait(false);           
        }

        private InvokerParameters GetRequestElements(ConnectorFunction function, FormulaValue[] args, Uri baseAddress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // https://stackoverflow.com/questions/5258977/are-http-headers-case-sensitive
            // Header names are not case sensitive.
            // From RFC 2616 - "Hypertext Transfer Protocol -- HTTP/1.1", Section 4.2, "Message Headers"
            Dictionary<string, string> headers = new (StringComparer.OrdinalIgnoreCase);            
            Dictionary<string, (OpenApiSchema, FormulaValue)> bodyParts = new ();
            StringBuilder query = new StringBuilder();
            string body = null;
            string path = function.OperationPath;

            Dictionary<string, FormulaValue> map = ConvertToNamedParameters(function, args);

            foreach (OpenApiParameter param in function._internals.OpenApiBodyParameters)
            {
                if (map.TryGetValue(param.Name, out var paramValue))
                {
                    bodyParts.Add(param.Name, (param.Schema, paramValue));
                }
                else if (param.Schema.Default != null)
                {
                    if (OpenApiExtensions.TryGetOpenApiValue(param.Schema.Default, null, out FormulaValue defaultValue))
                    {
                        bodyParts.Add(param.Name, (param.Schema, defaultValue));
                    }
                }
            }

            if (bodyParts.Any())
            {
                body = GetBody(function, function._internals.BodySchemaReferenceId, function._internals.SchemaLessBody, bodyParts, cancellationToken);
            }

            foreach (OpenApiParameter param in function.Operation.Parameters)
            {
                if (map.TryGetValue(param.Name, out var paramValue))
                {
                    var valueStr = paramValue?.ToObject()?.ToString() ?? string.Empty;

                    if (param.GetDoubleEncoding())
                    {
                        valueStr = HttpUtility.UrlEncode(valueStr);
                    }

                    switch (param.In.Value)
                    {
                        case ParameterLocation.Path:
                            path = path.Replace("{" + param.Name + "}", HttpUtility.UrlEncode(valueStr));
                            break;

                        case ParameterLocation.Query:
                            query.Append((query.Length == 0) ? "?" : "&");
                            query.Append(param.Name);
                            query.Append('=');
                            query.Append(HttpUtility.UrlEncode(valueStr));
                            break;

                        case ParameterLocation.Header:
                            headers.Add(param.Name, valueStr);
                            break;

                        case ParameterLocation.Cookie:
                        default:
                            Logger?.LogError($"In {nameof(FunctionInvoker)}.{nameof(GetRequestElements)}, unsupported {param.In.Value}");
                            return null;
                    }
                }
            }

            var url = (OpenApiParser.GetServer(function.Servers, baseAddress) ?? string.Empty) + path + query.ToString();

            return new InvokerParameters()
            {
                QueryType = QueryType.InitialRequest,
                Url = url,
                Headers = headers,
                Body = body,
                HttpMethod = function.HttpMethod,
                ContentType = function._internals.ContentType
            };
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "False positive")]
        private string GetBody(ConnectorFunction function, string referenceId, bool schemaLessBody, Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)> map, CancellationToken cancellationToken)
        {
            FormulaValueSerializer serializer = null;

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                serializer = function._internals.ContentType.ToLowerInvariant() switch
                {
                    OpenApiExtensions.ContentType_XWwwFormUrlEncoded => new OpenApiFormUrlEncoder(UtcConverter, schemaLessBody),
                    OpenApiExtensions.ContentType_TextPlain => new OpenApiTextSerializer(UtcConverter, schemaLessBody),
                    _ => new OpenApiJsonSerializer(UtcConverter, schemaLessBody)
                };

                serializer.StartSerialization(referenceId);
                foreach (var kv in map)
                {
                    serializer.SerializeValue(kv.Key, kv.Value.Schema, kv.Value.Value);
                }

                serializer.EndSerialization();

                return serializer.GetResult();                
            }
            finally
            {
                if (serializer != null && serializer is IDisposable disp)
                {
                    disp.Dispose();
                }
            }
        }

        public FormulaValue DecodeJson(ConnectorFunction function, bool returnRawResults, string text)
        {
            return string.IsNullOrWhiteSpace(text)
                    ? FormulaValue.NewBlank(function.ReturnType)
                    : returnRawResults
                    ? FormulaValue.New(text)
                    : FormulaValueJSON.FromJson(text, function.ReturnType); // $$$ Do we need to check response media type to confirm that the content is indeed json?
        }

        public Dictionary<string, FormulaValue> ConvertToNamedParameters(ConnectorFunction function, FormulaValue[] args)
        {
            // First N are required params. 
            // Last param is a record with each field being an optional.
            Dictionary<string, FormulaValue> map = new (StringComparer.OrdinalIgnoreCase);

            // Seed with default values. This will get overwritten if provided. 
            foreach (KeyValuePair<string, (bool required, FormulaValue fValue, DType dType)> kv in function._internals.ParameterDefaultValues)
            {
                map[kv.Key] = kv.Value.fValue;
            }

            foreach (ConnectorParameter param in function.HiddenRequiredParameters)
            {
                map[param.Name] = param.DefaultValue;
            }

            // Required parameters are always first
            for (int i = 0; i < function.RequiredParameters.Length; i++)
            {
                string parameterName = function.RequiredParameters[i].Name;
                FormulaValue value = args[i];

                // Objects are always flattenned                
                if (value is RecordValue record && !function.RequiredParameters[i].IsBodyParameter)
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
            if (function.OptionalParameters.Length > 0 && args.Length > function.RequiredParameters.Length)
            {
                FormulaValue optionalArg = args[args.Length - 1];

                // Objects are always flattenned
                if (optionalArg is RecordValue record)
                {
                    foreach (NamedValue field in record.Fields)
                    {
                        if (map.ContainsKey(field.Name))
                        {
                            // if optional parameters are defined and a default value is already present
                            map[field.Name] = field.Value;
                        }
                        else
                        {
                            map.Add(field.Name, field.Value);
                        }
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
                    else if (field1.Value is BlankValue)
                    {
                        lst.Remove(field1);
                        lst.Add(field2);
                    }
                    else if (field2.Value is BlankValue)
                    {
                        lst.Remove(field2);
                        lst.Add(field1);
                    }
                    else
                    {
                        throw new ArgumentException($"Cannot merge '{field1.Name}' of type {field1.Value.GetType().Name} with '{field2.Name}' of type {field2.Value.GetType().Name}");
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
    }
}
