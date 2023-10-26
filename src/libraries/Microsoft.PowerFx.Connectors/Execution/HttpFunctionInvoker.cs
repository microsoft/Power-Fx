// Copyright (c) Microsoft Corporation.
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
using Microsoft.CodeAnalysis;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Connectors.Execution;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // Given Power Fx arguments, translate into a HttpRequestMessage and invoke.
    internal class HttpFunctionInvoker
    {
        private readonly HttpMessageInvoker _httpClient;
        private readonly ConnectorFunction _function;
        private readonly bool _returnRawResults;
        private readonly ConnectorLogger _logger;

        public HttpFunctionInvoker(ConnectorFunction function, BaseRuntimeConnectorContext runtimeContext)
        {
            _function = function;
            _httpClient = runtimeContext.GetInvoker(function.Namespace);
            _returnRawResults = runtimeContext.ReturnRawResults;
            _logger = runtimeContext.ExecutionLogger;
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "False positive")]
        public HttpRequestMessage BuildRequest(FormulaValue[] args, IConvertToUTC utcConverter, CancellationToken cancellationToken)
        {
            HttpContent body = null;
            var path = _function.OperationPath;
            var query = new StringBuilder();

            cancellationToken.ThrowIfCancellationRequested();

            // Function couldn't be initialized properly, let's stop immediately
            if (_function._internals == null)
            {
                _logger?.LogError($"In {nameof(HttpFunctionInvoker)}.{nameof(BuildRequest)}, _function._internals is null");
                return null;
            }

            // https://stackoverflow.com/questions/5258977/are-http-headers-case-sensitive
            // Header names are not case sensitive.
            // From RFC 2616 - "Hypertext Transfer Protocol -- HTTP/1.1", Section 4.2, "Message Headers"
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, (OpenApiSchema, FormulaValue)> bodyParts = new ();
            Dictionary<string, FormulaValue> map = ConvertToNamedParameters(args);

            foreach (OpenApiParameter param in _function._internals.OpenApiBodyParameters)
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
                body = GetBody(_function._internals.BodySchemaReferenceId, _function._internals.SchemaLessBody, bodyParts, utcConverter, cancellationToken);
            }

            foreach (OpenApiParameter param in _function.Operation.Parameters)
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
                            _logger?.LogError($"In {nameof(HttpFunctionInvoker)}.{nameof(BuildRequest)}, unsupported {param.In.Value}");
                            return null;
                    }
                }
            }

            var url = (OpenApiParser.GetServer(_function.Servers, _httpClient) ?? string.Empty) + path + query.ToString();            
            var request = new HttpRequestMessage(_function.HttpMethod, url);

            foreach (var kv in headers)
            {
                request.Headers.Add(kv.Key, kv.Value);
            }

            if (body != null)
            {
                request.Content = body;
            }

            return request;
        }

        public Dictionary<string, FormulaValue> ConvertToNamedParameters(FormulaValue[] args)
        {
            // First N are required params. 
            // Last param is a record with each field being an optional.

            Dictionary<string, FormulaValue> map = new (StringComparer.OrdinalIgnoreCase);

            // Seed with default values. This will get overwritten if provided. 
            foreach (KeyValuePair<string, (bool required, FormulaValue fValue, DType dType)> kv in _function._internals.ParameterDefaultValues)
            {
                map[kv.Key] = kv.Value.fValue;
            }

            foreach (ConnectorParameter param in _function.HiddenRequiredParameters)
            {
                map[param.Name] = param.DefaultValue;
            }

            // Required parameters are always first
            for (int i = 0; i < _function.RequiredParameters.Length; i++)
            {
                string parameterName = _function.RequiredParameters[i].Name;
                FormulaValue value = args[i];

                // Objects are always flattenned                
                if (value is RecordValue record && !_function.RequiredParameters[i].IsBodyParameter)
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
            if (_function.OptionalParameters.Length > 0 && args.Length > _function.RequiredParameters.Length)
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

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "False positive")]
        private HttpContent GetBody(string referenceId, bool schemaLessBody, Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)> map, IConvertToUTC utcConverter, CancellationToken cancellationToken)
        {
            FormulaValueSerializer serializer = null;

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                serializer = _function._internals.ContentType.ToLowerInvariant() switch
                {
                    OpenApiExtensions.ContentType_XWwwFormUrlEncoded => new OpenApiFormUrlEncoder(utcConverter, schemaLessBody),
                    OpenApiExtensions.ContentType_TextPlain => new OpenApiTextSerializer(utcConverter, schemaLessBody),
                    _ => new OpenApiJsonSerializer(utcConverter, schemaLessBody)
                };

                serializer.StartSerialization(referenceId);
                foreach (var kv in map)
                {
                    serializer.SerializeValue(kv.Key, kv.Value.Schema, kv.Value.Value);
                }

                serializer.EndSerialization();

                string body = serializer.GetResult();
                return new StringContent(body, Encoding.Default, _function._internals.ContentType);
            }
            finally
            {
                if (serializer != null && serializer is IDisposable disp)
                {
                    disp.Dispose();
                }
            }
        }

        public async Task<FormulaValue> DecodeResponseAsync(HttpResponseMessage response, bool throwOnError = false)
        {
            var text = response?.Content == null
                            ? string.Empty
                            : await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var statusCode = (int)response.StatusCode;

            if (statusCode < 300)
            {
                return string.IsNullOrWhiteSpace(text)
                    ? FormulaValue.NewBlank(_function.ReturnType)
                    : _returnRawResults
                    ? FormulaValue.New(text)
                    : FormulaValueJSON.FromJson(text, _function.ReturnType); // $$$ Do we need to check response media type to confirm that the content is indeed json?
            }

            if (throwOnError)
            {
                throw new HttpRequestException($"Http Status Error {statusCode}: {text}");
            }

            return FormulaValue.NewError(
                    new ExpressionError()
                    {
                        Kind = ErrorKind.Network,
                        Severity = ErrorSeverity.Critical,
                        Message = $"The server returned an HTTP error with code {statusCode}. Response: {text}"
                    },
                    _function.ReturnType);
        }

        public async Task<FormulaValue> InvokeAsync(IConvertToUTC utcConverter, string cacheScope, FormulaValue[] args, HttpMessageInvoker localInvoker, CancellationToken cancellationToken, bool throwOnError = false)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using HttpRequestMessage request = BuildRequest(args, utcConverter, cancellationToken);

            if (request == null)
            {
                _logger?.LogError($"In {nameof(HttpFunctionInvoker)}.{nameof(InvokeAsync)} request is null");
                return new ErrorValue(IRContext.NotInSource(_function.ReturnType), new ExpressionError()
                {
                    Kind = ErrorKind.Internal,
                    Severity = ErrorSeverity.Critical,
                    Message = $"In {nameof(HttpFunctionInvoker)}.{nameof(InvokeAsync)} request is null"
                });
            }

            return await ExecuteHttpRequest(cacheScope, throwOnError, request, localInvoker, cancellationToken).ConfigureAwait(false);
        }

        public async Task<FormulaValue> InvokeAsync(string url, string cacheScope, HttpMessageInvoker localInvoker, CancellationToken cancellationToken, bool throwOnError = false)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using HttpRequestMessage request = new HttpRequestMessage(_function.HttpMethod, new Uri(url).PathAndQuery);
            return await ExecuteHttpRequest(cacheScope, throwOnError, request, localInvoker, cancellationToken).ConfigureAwait(false);
        }

        private async Task<FormulaValue> ExecuteHttpRequest(string cacheScope, bool throwOnError, HttpRequestMessage request, HttpMessageInvoker localInvoker, CancellationToken cancellationToken)
        {
            var client = localInvoker ?? _httpClient;
            var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if ((int)response.StatusCode >= 300)
            {
                _logger?.LogError($"In {nameof(HttpFunctionInvoker)}.{nameof(ExecuteHttpRequest)}, response status code: {(int)response.StatusCode} {response.StatusCode}");
            }
            else
            {
                _logger?.LogInformation($"In {nameof(HttpFunctionInvoker)}.{nameof(ExecuteHttpRequest)}, response status code: {(int)response.StatusCode} {response.StatusCode}");
            }

            return await DecodeResponseAsync(response, throwOnError).ConfigureAwait(false);
        }
    }

    // Closure over a HttpFunctionInvoker, but scoped to a cacheScope.
    internal class ScopedHttpFunctionInvoker
    {
        private readonly string _cacheScope;
        private readonly HttpFunctionInvoker _invoker;
        private readonly bool _throwOnError;

        public ScopedHttpFunctionInvoker(DPath ns, string name, string cacheScope, HttpFunctionInvoker invoker, bool throwOnError = false)
        {
            Namespace = ns;
            Name = name;

            _cacheScope = cacheScope;
            _invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
            _throwOnError = throwOnError;
        }

        public DPath Namespace { get; }

        public string Name { get; }

        internal HttpFunctionInvoker Invoker => _invoker;

        public Task<FormulaValue> InvokeAsync(FormulaValue[] args, BaseRuntimeConnectorContext runtimeContext, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var localInvoker = runtimeContext.GetInvoker(this.Namespace.Name);            
            return _invoker.InvokeAsync(new ConvertToUTC(runtimeContext.TimeZoneInfo), _cacheScope, args, localInvoker, cancellationToken, _throwOnError);
        }

        public Task<FormulaValue> InvokeAsync(string url, BaseRuntimeConnectorContext runtimeContext, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var localInvoker = runtimeContext.GetInvoker(this.Namespace.Name);            
            return _invoker.InvokeAsync(url, _cacheScope, localInvoker, cancellationToken, _throwOnError);
        }
    }

    internal interface IConvertToUTC
    {
        DateTime ToUTC(DateTimeValue d);
    }

    internal class ConvertToUTC : IConvertToUTC
    {
        private readonly TimeZoneInfo _tzi;

        public ConvertToUTC(TimeZoneInfo tzi)
        {
            _tzi = tzi;
        }
        
        public DateTime ToUTC(DateTimeValue dtv)
        {
            return dtv.GetConvertedValue(_tzi);
        }
    }
}
