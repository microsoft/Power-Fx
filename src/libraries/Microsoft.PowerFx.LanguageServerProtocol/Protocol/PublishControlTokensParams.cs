// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.LanguageServerProtocol.Schemas;

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    internal class PublishControlTokensParams
    {
        /// <summary>
        /// The Version for which token information is reported.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// A list of control token information items.
        /// </summary>
        public IEnumerable<ControlToken> Data { get; set; } = new List<ControlToken>();
    }
}
