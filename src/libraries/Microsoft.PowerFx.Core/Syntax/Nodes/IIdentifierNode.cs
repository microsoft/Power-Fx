// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Syntax
{
    // Interface used on Nodes that support identifiers (FirstNameNode and DottedNameNode) 
    public interface IIdentifierNode
    {
        bool IsIdentifier { get; }

        void SetIdentifier();

        string GetName();

        IIdentifierNode AsIdentifierNode();
    }
}
