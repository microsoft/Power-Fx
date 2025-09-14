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

        internal int StartingIndex { get; }

        internal PartialAttribute Attribute { get; }

        // used by the pretty printer to get the proper operator in the output
        internal bool ColonEqual { get; }

        public NamedFormula(IdentToken ident, Formula formula, int startingIndex, bool colonEqual, PartialAttribute attribute = null)
        {
            Contracts.AssertValue(ident);
            Contracts.AssertValue(formula);

            Ident = ident;
            Formula = formula;
            StartingIndex = startingIndex;
            ColonEqual = colonEqual;
            Attribute = attribute;
        }
    }
}
