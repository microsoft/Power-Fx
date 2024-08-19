// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Texl;

namespace Microsoft.PowerFx.Syntax
{
    internal class ListFunctionVisitor : IdentityTexlVisitor
    {
        // FullName --> Name 
        // Use Fullname as key because it's unique. 
        private readonly HashSet<string> _functionNames = new HashSet<string>();
        private readonly Dictionary<string, string> _unknownFunctionNames = new Dictionary<string, string>();
        private readonly bool _anonymizedUnknownPublicFunctions;

        public static IEnumerable<string> Run(ParseResult parse, bool annonymizeUnknownPublicFunctions = false)
        {
            var visitor = new ListFunctionVisitor(annonymizeUnknownPublicFunctions);
            parse.Root.Accept(visitor);
            return visitor._functionNames;
        }

        private ListFunctionVisitor(bool annonymizeUnknownPublicFunctions)
        {
            _anonymizedUnknownPublicFunctions = annonymizeUnknownPublicFunctions;
        }

        public override bool PreVisit(CallNode node)
        {
            var hasNamespace = node.Head.Namespace.Length > 0;
            var name = node.Head.Name;

            if (_anonymizedUnknownPublicFunctions && !BuiltinFunctionsCore.IsKnownPublicFunction(name))
            {
                // An expression can have multiple unknown function names.
                // Track them all to ensure they are uniquely anonymized.
                if (!_unknownFunctionNames.ContainsKey(name))
                {
                    _unknownFunctionNames[name] = $"$#CustomFunction{_unknownFunctionNames.Count + 1}#$";
                }

                _functionNames.Add(_unknownFunctionNames[name]);
            }
            else
            {
                var fullName = hasNamespace ?
                        node.Head.Namespace + "." + name :
                        name;

                _functionNames.Add(fullName);
            }

            return base.PreVisit(node);
        }
    }
}
