// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.Json;

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    /// <summary>
    /// Represents the input to the language server.
    /// </summary>
    public class LanguageServerInput
    {
        /// <summary>
        /// Id of the request. Applicable only for request messages.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Method of the request or notification.
        /// </summary>
        public string Method { get; private set; }

        /// <summary>
        /// The jsonrpc version.
        /// </summary>
        public string Jsonrpc { get; private set; }

        /// <summary>
        /// Parameters of the request or notification.
        /// </summary>
        public string Params { get; private set; }
        
        /// <summary>
        /// Parses the input json payload to <see cref="LanguageServerInput"/>.
        /// </summary>
        /// <param name="jsonRpcPayload">Raw Payload.</param>
        /// <returns>Parsed Input.</returns>
        public static LanguageServerInput Parse(string jsonRpcPayload)
        {
            var input = new LanguageServerInput();
            try
            {
                using (var document = JsonDocument.Parse(jsonRpcPayload))
                {
                    if (document == null)
                    {
                        return input;
                    }

                    var root = document.RootElement;
                    if (root.TryGetProperty("id", out var idElement))
                    {
                        input.Id = idElement.GetString();
                    }

                    if (root.TryGetProperty("method", out var methodElement))
                    {
                        input.Method = methodElement.GetString();
                    }

                    if (root.TryGetProperty("jsonrpc", out var jsonrpcElement))
                    {
                        input.Jsonrpc = jsonrpcElement.GetString();
                    }

                    if (root.TryGetProperty("params", out var paramsElement))
                    {
                        input.Params = paramsElement.GetRawText();
                    }
                }
            }
            catch (JsonException)
            {
                // Ignore the exception and return the input.
            }
            
            return input;
        }
    }
}
