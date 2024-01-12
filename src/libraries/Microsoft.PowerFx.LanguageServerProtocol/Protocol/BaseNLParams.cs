// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class BaseNLParams
    {
        /// <summary>
        /// Additional context for NL operation. Usually, a stringified JSON object.
        /// </summary>
        public string NLContext { get; set; } = null;
    }
}
