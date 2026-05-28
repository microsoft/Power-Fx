// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Parser
{
    internal class NamedFormula
    {
        internal IdentToken Ident { get; }

        internal Formula Formula { get; }

        internal int StartingIndex { get; }

        internal IReadOnlyList<Attribute> Attributes { get; }

        // used by the pretty printer to get the proper operator in the output
        internal bool ColonEqual { get; }

        public NamedFormula(IdentToken ident, Formula formula, int startingIndex, bool colonEqual, IReadOnlyList<Attribute> attributes = null)
        {
            Contracts.AssertValue(ident);
            Contracts.AssertValue(formula);

            Ident = ident;
            Formula = formula;
            StartingIndex = startingIndex;
            ColonEqual = colonEqual;
            Attributes = attributes ?? Array.Empty<Attribute>();
        }
    }
}
