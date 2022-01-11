// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.Core.IR.Nodes
{
    /*
    * Error nodes show up in the IR Tree where we could not bind the parse tree correctly.
    * This leaves the decision on how to handle this error up to implementers.
    */
    internal sealed class ErrorNode : IntermediateNode
    {
        // ErrorHint contains the stringified part of the Parse Tree
        // that resulted in this error node.
        // This mostly exists for debug purposes.
        public readonly string ErrorHint;

        public ErrorNode(IRContext irContext, string hint) : base(irContext)
        {
            ErrorHint = hint;
        }

        public override TResult Accept<TResult, TContext>(IRNodeVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        public override string ToString()
        {
            return $"Error({ErrorHint})";
        }
    }
}
