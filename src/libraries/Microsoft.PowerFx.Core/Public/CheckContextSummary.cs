// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Get summary of information from the <see cref="CheckResult"/>.
    /// This can be used for editor features that help write expressions. 
    /// </summary>
    public class CheckContextSummary
    {
        public bool AllowsSideEffects { get; set; }

        public bool IsPreV1Semantics { get; set; }

        /// <summary>
        /// Optional. If present, the expected type this expression should produce.
        /// </summary>
        public FormulaType ExpectedReturnType { get; set; }

        // Top level preferred symbols - this may filter out symbols
        // Biased to 'ThisRecord', not and not including implicit scope. 
        public IEnumerable<SymbolEntry> SuggestedSymbols { get; set; }
    }
}
