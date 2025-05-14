// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.PowerFx.Core.Functions.Delegation;

namespace Microsoft.PowerFx.Connectors
{
    public class ColumnCapabilitiesPoco
    {
        [JsonPropertyName("filterFunctions")]
        public string[] FilterFunctions { get; set; }

        // Strong-typing
        public IEnumerable<DelegationOperator> FilterFunctionOps() =>
            Utilities.ToDelegationOp(FilterFunctions);

        public static ColumnCapabilitiesPoco New(IEnumerable<DelegationOperator> ops)
        {
            return new ColumnCapabilitiesPoco
            {
                FilterFunctions = ops.ToStr().ToArray()
            };
        }
    }
}
