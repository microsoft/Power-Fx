// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Intellisense;

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class PublishTokensParams
    {
        /// <summary>
        /// The URI for which token information is reported.
        /// </summary>
        public string Uri { get; set; } = string.Empty;

        /// <summary>
        /// A map of token information items.
        /// </summary>
        public IReadOnlyDictionary<string, TokenResultType> Tokens { get; set; } = new Dictionary<string, TokenResultType>();
    }
}
