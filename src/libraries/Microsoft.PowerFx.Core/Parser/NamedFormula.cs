// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Syntax.SourceInformation;

namespace Microsoft.PowerFx.Core.Parser
{
    internal class NamedFormula 
    {
        internal IdentToken Ident { get; }

        internal Formula Formula { get; }

        internal int StartingIndex { get; }

        public NamedFormula(IdentToken ident, Formula formula, int startingIndex)
        {
            Contracts.AssertValue(ident);
            Contracts.AssertValue(formula);
            
            Ident = ident;
            Formula = formula;
            StartingIndex = startingIndex;
        }
    }
}
