﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class BaseNLParams
    {
        /// <summary>
        /// Additional context for NL operation. Usually, a stringified JSON object.
        /// </summary>
        public string Context { get; set; } = null;
    }

    public class BaseNLResult
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault | JsonIgnoreCondition.WhenWritingNull)]
        public string DiagnosticsJson { get; set; } = null;
    }
}
