// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Syntax.Visitors
{
    internal class ScopePredicateVisitor : IdentityTexlVisitor
    {
        private readonly DType _typeScope;
        private readonly DName[] _idents;

        // The Summarize function creates a 'ThisGroup' indentifier that can only be used inside a call.
        private bool _allowAggregateCall;

        public HashSet<DName> InUsePredicates;

        public ScopePredicateVisitor(DType typeScope, DName[] indents, bool allowAggregateCall = false)
        {
            _typeScope = typeScope;
            _idents = indents;
            _allowAggregateCall = allowAggregateCall;

            InUsePredicates = new HashSet<DName>();
        }

        public override void Visit(FirstNameNode node)
        {
            if (_idents.Contains(node.Ident.Name) || _typeScope.TryGetType(node.Ident.Name, out _))
            {
                InUsePredicates.Add(node.Ident.Name);
            }
        }

        public override bool PreVisit(DottedNameNode node)
        {
            // No need to visit right node.
            node.Left.Accept(this);
            return false;
        }

        public override bool PreVisit(CallNode node)
        {
            if (_allowAggregateCall)
            {
                // If `ThisGroup` is placed inside multiple calls, raise a warning.
                // Example
                //      Summarize(t1, Fruit, CountRows( Distinct( ThisGroup, Supplier ) ) As CountOfSuppliers )
                _allowAggregateCall = false;
                return true;
            }

            return false;
        }
    }
}
