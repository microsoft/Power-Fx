// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Parser
{
    internal class NamedFormula 
    {
        internal IdentToken Ident { get; }

        internal Formula Formula { get; }

        public NamedFormula(IdentToken ident, Formula formula)
        {
            Contracts.AssertValue(ident);
            Contracts.AssertValue(formula);
            
            Ident = ident;
            Formula = formula;
        }
    }
}
