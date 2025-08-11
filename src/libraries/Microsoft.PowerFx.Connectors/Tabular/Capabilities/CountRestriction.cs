// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Microsoft.PowerFx.Connectors
{
    internal class CountRestriction
    {
        /// <summary>
        /// Indicates whether table supports $count=true in OData queries.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName(CapabilityConstants.Countable)]
        public readonly bool IsCountable;

        public CountRestriction(bool isCountable)
        {
            IsCountable = isCountable;
        }
    }
}
