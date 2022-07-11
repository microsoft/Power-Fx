// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.IR.Nodes
{
    internal abstract class IntermediateNode
    {
        public IRContext IRContext { get; }

        public IntermediateNode(IRContext irContext)
        {
            IRContext = irContext;
        }

        /// <summary>
        /// This method visits the node using the visitor and context provided.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="visitor">Visitor to use.</param>
        /// <param name="context">Context.</param>
        /// <returns></returns>
        /// <param name="stackMarker"></param>
        public abstract TResult Accept<TResult, TContext>(IRNodeVisitor<TResult, TContext> visitor, TContext context, StackMarker stackMarker);
    }
}
