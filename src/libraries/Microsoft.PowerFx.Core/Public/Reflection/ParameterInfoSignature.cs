// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    // ParameterInfo may have been a better name, but that conflicts with a type
    // in Microsoft.PowerFx.Core.Functions.TransportSchemas. 
    [DebuggerDisplay("{Name}: {Description}")]
    public class ParameterInfoSignature
    {
        public string Name { get; init; }

        public string Description { get; init; }

        // More fields, like optional? default value?, etc.

        // Could be null if not available.
        public FormulaType ParameterType { get; init; }
    }
}
