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
        public string Id { get; init; }

        /// <summary>
        /// Method of the request or notification.
        /// </summary>
        public string Method { get; init; }

        /// <summary>
        /// The jsonrpc version.
        /// </summary>
        public string Jsonrpc { get; init; }

        /// <summary>
        /// Parameters of the request or notification.
        /// </summary>
        public JsonElement Params { get; init; }

        /// <summary>
        /// Raw parameters of the request or notification.
        /// </summary>
        public string RawParams
        {
            get
            {
                try
                {
                    return Params.GetRawText();
                }
                catch
                {
                    return string.Empty;
                }
            }
        }
        
        /// <summary>
        /// Parses the input json payload to <see cref="LanguageServerInput"/>.
        /// </summary>
        /// <param name="jsonRpcPayload">Raw Payload.</param>
        /// <returns>Parsed Input.</returns>
        public static LanguageServerInput Parse(string jsonRpcPayload)
        {
            if (LanguageServerHelper.TryParseParams(jsonRpcPayload, out LanguageServerInput input))
            {
                return input;
            }

            return null;
        }
    }
}
