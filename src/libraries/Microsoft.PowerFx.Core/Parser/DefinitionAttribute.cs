// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Parser
{
    internal class DefinitionAttribute
    {
        public IdentToken AttributeName;

        // This probably should be a TexlNode, but for now we're prototyping a simple case
        // of just [Partial And]-type attributes, where they're pairs of name and operation.  
        public IdentToken AttributeOperation;

        public DefinitionAttribute(IdentToken attributeName, IdentToken attributeOperation)
        {
            AttributeName = attributeName;
            AttributeOperation = attributeOperation;
        }
    }
}
