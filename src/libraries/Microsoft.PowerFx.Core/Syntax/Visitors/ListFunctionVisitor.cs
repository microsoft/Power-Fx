// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.PowerFx.Syntax
{
    public class ListFunctionVisitor : IdentityTexlVisitor
    {
        public HashSet<string> _functionNamespaces = new HashSet<string>();

        // FullName --> Name 
        // Use Fullname as key because it's unique. 
        public Dictionary<string, string> _functionNames = new Dictionary<string, string>();

        public static ListFunctionVisitor Run(CheckResult check)
        {
            return Run(check.Parse);
        }

        public static ListFunctionVisitor Run(string expression, ParserOptions options = null)
        {
            options ??= new ParserOptions();
            var parse = options.Parse(expression);

            return Run(parse);
        }

        public static ListFunctionVisitor Run(ParseResult parse)
        {
            var visitor = new ListFunctionVisitor();
            parse.Root.Accept(visitor);
            return visitor;
        }

        public override bool PreVisit(CallNode node)
        {
            var hasNamespace = node.Head.Namespace.Length > 0;

            if (hasNamespace)
            {
                _functionNamespaces.Add(node.Head.Namespace.ToString());
            }

            var name = node.Head.Name;
            var fullName = hasNamespace ?
                    node.Head.Namespace + "." + name :
                    name;

            _functionNames[fullName] = name;

            return base.PreVisit(node);
        }
    }
}
