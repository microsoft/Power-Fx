// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Syntax.Visitors
{
    internal class ScopePredicateVisitor : IdentityTexlVisitor
    {
        private readonly TexlBinding _binding;

        public HashSet<DName> InUsePredicates;

        public ScopePredicateVisitor(TexlBinding binding)
        {
            _binding = binding;

            InUsePredicates = new HashSet<DName>();
        }

        public override void Visit(FirstNameNode node)
        {
            var info = _binding.GetInfo(node);
            
            //_binding.TryGetCall(_node.Id, out var callInfo);

            //if (_idents.Contains(node.Ident.Name) || _typeScope.TryGetType(node.Ident.Name, out _))
            //{
            //    InUsePredicates.Add(node.Ident.Name);
            //}

            var test = "dasdasd";
        }

        public override bool PreVisit(DottedNameNode node)
        {
            // No need to visit right node.
            node.Left.Accept(this);
            return false;
        }

        public override bool PreVisit(CallNode node)
        {
            if (_binding.TryGetCall(node.Id, out var callInfo) && 
                callInfo.Function.ScopeInfo != null &&
                callInfo.Function.ScopeInfo.CheckPredicateUsage)
            {
                return true;
            }

            return false;
        }
    }
}
