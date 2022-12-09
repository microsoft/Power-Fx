// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.Json;

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    /// <summary>
    /// JSON-RPC helper class that follows JSON-RPC 2.0 spec https://www.jsonrpc.org/specification.
    /// </summary>
    public static class JsonRpcHelper
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                // Serialize types without accounting for any defined type names
#pragma warning disable CS0618 // Type or member is obsolete. This will be cleaned up when the formula bar is ready to accept the updated schema.
                new FormulaTypeJsonConverter()
#pragma warning restore CS0618
            }
        };

        public enum ErrorCode
        {
            ParseError = -32700,
            InvalidRequest = -32600,
            MethodNotFound = -32601,
            InvalidParams = -32602,
            InternalError = -32603,
            PropertyValueRequired = -32604,
            ServerError = -32000
        }

        public static string CreateErrorResult(string id, ErrorCode code) => CreateErrorResult(id, new
        {
            code = (int)code
        });

        public static string CreateErrorResult(string id, ErrorCode code, string message) => CreateErrorResult(id, new
        {
            code = (int)code,
            message = message
        });

        private static string CreateErrorResult(string id, object error) => JsonSerializer.Serialize(
            new
            {
                jsonrpc = "2.0",
                id,
                error
            }, _jsonSerializerOptions);

        public static string CreateSuccessResult(string id, object result) => JsonSerializer.Serialize(
            new
            {
                jsonrpc = "2.0",
                id,
                result
            }, _jsonSerializerOptions);

        public static string CreateNotification(string method, object @params) => JsonSerializer.Serialize(
            new
            {
                jsonrpc = "2.0",
                method,
                @params
            }, _jsonSerializerOptions);

        public static string Serialize<T>(T data)
        {
            return JsonSerializer.Serialize<T>(data, _jsonSerializerOptions);
        }

        public static T Deserialize<T>(string data)
        {
            return JsonSerializer.Deserialize<T>(data, _jsonSerializerOptions);
        }
    }
}
