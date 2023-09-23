// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx
{
    // Pattern match for Set(x,y) so that we can define the variable
    internal class FindDeclarationVisitor : IdentityTexlVisitor
    {
        public List<Declaration> _declarations = new List<Declaration>();

        public class Declaration
        {
            public string _variableName;

            public TexlNode _rhs;
        }

        public override bool PreVisit(CallNode call)
        {
            if (call.Head.Name.Value == "Set")
            {
                // Infer type based on arg1. 
                var arg0 = call.Args.ChildNodes[0];
                if (arg0 is FirstNameNode arg0node)
                {
                    string arg0name = arg0node.Ident.Name.Value;

                    var arg1 = call.Args.ChildNodes[1];

                    _declarations.Add(new Declaration
                    {
                        _variableName = arg0name,
                        _rhs = arg1
                    });
                }
            }

            return base.PreVisit(call);
        }
    }
}
