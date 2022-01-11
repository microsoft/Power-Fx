// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
        };

        public enum ErrorCode
        {
            ParseError = -32700,
            InvalidRequest = -32600,
            MethodNotFound = -32601,
            InvalidParams = -32602,
            InternalError = -30603,
            ServerError = -32000
        }

        public static string CreateErrorResult(string id, ErrorCode code) => CreateErrorResult(id, new
        {
            code = (int)code
        });

        public static string CreateErrorResult(string id, object error) => JsonSerializer.Serialize(
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
    }
}
