// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Parser
{
    internal sealed class PartialAttribute
    {
        public enum AttributeOperationKind
        {
            Error,
            PartialAnd,
            PartialOr,
            PartialTable,
            PartialRecord
        }

        public readonly IdentToken AttributeName;

        // This probably should be a TexlNode, but for now we're prototyping a simple case
        // of just [Partial And]-type attributes, where they're pairs of name and operation.
        public readonly Token AttributeOperationToken;

        public readonly AttributeOperationKind AttributeOperation;

        public PartialAttribute(IdentToken attributeName, Token attributeOperationToken)
        {
            AttributeName = attributeName;
            AttributeOperationToken = attributeOperationToken;
            AttributeOperation = ToKind(attributeOperationToken);
        }

        private AttributeOperationKind ToKind(Token attributeOperation)
        {
            if (attributeOperation is KeyToken keyTok)
            {
                if (keyTok.Kind == TokKind.KeyAnd)
                {
                    return AttributeOperationKind.PartialAnd;
                }
                else if (keyTok.Kind == TokKind.KeyOr)
                {
                    return AttributeOperationKind.PartialOr;
                }
            }
            else if (attributeOperation is IdentToken identTok)
            {
                if (identTok.Name.Value == "Table")
                {
                    return AttributeOperationKind.PartialTable;
                }
                else if (identTok.Name.Value == "Record")
                {
                    return AttributeOperationKind.PartialRecord;
                }
            }

            return AttributeOperationKind.Error;
        }

        public bool SameAttribute(PartialAttribute other)
        {
            return AttributeName.Name.Value == other.AttributeName.Name.Value &&
                AttributeOperation == other.AttributeOperation;
        }
    }
}
