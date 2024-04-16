// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Specialized;
using System.Text.Json;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    /// <summary>
    /// Helper class for Language Server.
    /// </summary>
    internal static class LanguageServerHelper
    {
        internal static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new ()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Choose expression from multiple options (old or new).
        /// We used to pass expression in query params, but now we pass it in the request body.
        /// </summary>
        /// <param name="baseParams">Base Request params. </param>
        /// <param name="queryParams">Query Params from document uri.</param>
        /// <returns>Chosen Expression.</returns>
        internal static string ChooseExpression(LanguageServerRequestBaseParams baseParams, NameValueCollection queryParams)
        {
            return baseParams?.Text ?? queryParams.Get("expression");
        }

        /// <summary>
        ///  Attempt to parse the given params into the given type.
        /// </summary>
        /// <typeparam name="T">Type of the deserialized params.</typeparam>
        /// <param name="paramsToParse">Params to be parsed.</param>
        /// <param name="result">Reference to store the deserialized or parsed params.</param>
        /// <returns>True if params were parsed successfully or false otherwise.</returns>
        internal static bool TryParseParams<T>(string paramsToParse, out T result)
        {
            Contracts.AssertNonEmpty(paramsToParse);

            try
            {
                result = JsonSerializer.Deserialize<T>(paramsToParse, DefaultJsonSerializerOptions);
                return true;
            }
            catch
            {
                result = default;
                return false;
            }
        }
    }
}
