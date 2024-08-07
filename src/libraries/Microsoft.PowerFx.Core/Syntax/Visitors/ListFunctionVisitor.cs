// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.PowerFx.Syntax
{
    internal class ListFunctionVisitor : IdentityTexlVisitor
    {
        // FullName --> Name 
        // Use Fullname as key because it's unique. 
        private readonly HashSet<string> _functionNames = new HashSet<string>();

        public static IEnumerable<string> Run(ParseResult parse)
        {
            var visitor = new ListFunctionVisitor();
            parse.Root.Accept(visitor);
            return visitor._functionNames;
        }

        public override bool PreVisit(CallNode node)
        {
            var hasNamespace = node.Head.Namespace.Length > 0;

            var name = node.Head.Name;
            var fullName = hasNamespace ?
                    node.Head.Namespace + "." + name :
                    name;

            _functionNames.Add(fullName);

            return base.PreVisit(node);
        }
    }
}
