﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Syntax
{
    internal sealed class ChainTrackerVisitor : IdentityTexlVisitor
    {
        private bool _usesChains;

        private ChainTrackerVisitor()
        {
            _usesChains = false;
        }

        public static bool Run(TexlNode node)
        {
            var visitor = new ChainTrackerVisitor();
            node.Accept(visitor);
            return visitor._usesChains;
        }

        public override bool PreVisit(VariadicOpNode node)
        {
            if (node.Op == VariadicOp.Chain)
            {
                _usesChains = true;
                return false;
            }

            return true;
        }
    }
}
