// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.IR
{
    /// <summary>
    /// Common helper for rewriting an IR tree. 
    /// 
    /// IR trees are immutable, so we need to walk and rewrite the tree to include any new nodes.
    /// [1] Only return new nodes if needed, 
    /// so if there is no rewrite on a node, then ensure: object.ReferenceEquals(Ret(Materialize(node)), node) == true.
    /// [2] Caller is responsible for ensuring that the new nodes have the exact same type as existing nodes. 
    /// Called must inject any coercions if there are type changes. 
    /// [3] The visitor can also take in an error container to add new errors. 
    /// </summary>
    /// <typeparam name="TResult">The Intermediate Node tagged with additional information.</typeparam>
    /// <typeparam name="TContext">A context top passed to each node.</typeparam>
    internal abstract class RewritingIRVisitor<TResult, TContext> : IRNodeVisitor<TResult, TContext>
    {
        /// <summary>
        /// Convert a tagged result back to an intermediate node.        
        /// </summary>
        /// <param name="ret"></param>
        /// <returns></returns>
        protected abstract IntermediateNode Materialize(TResult ret);

        /// <summary>
        /// Called to wrap a node as the return type.
        /// </summary>
        protected abstract TResult Ret(IntermediateNode node);

        public override TResult Visit(TextLiteralNode node, TContext context)
        {
            return Ret(node);
        }

        public override TResult Visit(NumberLiteralNode node, TContext context)
        {
            return Ret(node);
        }

        public override TResult Visit(DecimalLiteralNode node, TContext context)
        {
            return Ret(node);
        }

        public override TResult Visit(BooleanLiteralNode node, TContext context)
        {
            return Ret(node);
        }

        public override TResult Visit(ColorLiteralNode node, TContext context)
        {
            return Ret(node);
        }

        public override TResult Visit(RecordNode node, TContext context)
        {
            var newFields = VisitDict(node.Fields, context);

            if (newFields != null)
            {
                var newNode = new RecordNode(node.IRContext, newFields);
                return Ret(newNode);
            }

            return Ret(node);
        }

        // Visit each field. If no changes, then return null, signalling to caller to just pass the origina dictionary. 
        // If anything changes, return a new dictionary with the updates. 
        private IReadOnlyDictionary<DName, IntermediateNode> VisitDict(IReadOnlyDictionary<DName, IntermediateNode> fields, TContext context)
        {
            // Dictionary order is arbitrary, so just make a copy upfront rather than enumerate multiple times. 
            Dictionary<DName, IntermediateNode> newFields = new Dictionary<DName, IntermediateNode>();

            bool rewrite = false;
            foreach (var kv in fields)
            {
                var child = Materialize(kv.Value.Accept(this, context));

                newFields[kv.Key] = child;

                if (!ReferenceEquals(child, kv.Value))
                {
                    rewrite = true;
                }
            }

            if (rewrite)
            {
                return newFields;
            }

            return null;
        }

        public override TResult Visit(ErrorNode node, TContext context)
        {
            return Ret(node);
        }

        public override TResult Visit(LazyEvalNode node, TContext context)
        {
            var newChild = Materialize(node.Child.Accept(this, context));

            if (object.ReferenceEquals(newChild, node.Child))
            {
                return Ret(node);
            }

            return Ret(new LazyEvalNode(node.IRContext, newChild));
        }

        public override TResult Visit(CallNode node, TContext context)
        {
            // Derived visitor gets first chance. 
            // Can callback to base if it doesn't handle. 

            TResult arg0 = default;
            if (node.Args.Count > 0)
            {
                arg0 = node.Args[0].Accept(this, context);
            }

            return Visit(node, context, arg0);
        }

        // Return null if no change. 
        // Else returns a new copy ofthe list with changes. 
        private IList<IntermediateNode> VisitList(IList<IntermediateNode> list, TContext context, TResult arg0 = default)
        {
            List<IntermediateNode> newArgs = null;

            for (int i = 0; i < list.Count; i++)
            {
                var arg = list[i];
                var ret = (i == 0 && arg0 != null) ? arg0 : arg.Accept(this, context);

                var result = Materialize(ret);

                if (newArgs == null && !ReferenceEquals(arg, result))
                {
                    newArgs = new List<IntermediateNode>(list.Count);

                    // Copy previous
                    for (int j = 0; j < i; j++)
                    {
                        newArgs.Add(list[j]);
                    }
                }

                newArgs?.Add(result);
            }

            return newArgs;
        }

        // Pass in arg0 is we already called Accept on it.
        public TResult Visit(CallNode node, TContext context, TResult arg0)
        {
            var newArgs = VisitList(node.Args, context, arg0);

            if (newArgs == null)
            {
                // No change
                return Ret(node);
            }

            // Copy over to new node 
            if (node.Scope == null)
            {
                return Ret(new CallNode(node.IRContext, node.Function, newArgs));
            }
            else
            {
                return Ret(new CallNode(node.IRContext, node.Function, node.Scope, newArgs));
            }
        }

        public override TResult Visit(BinaryOpNode node, TContext context)
        {
            var newLeft = Materialize(node.Left.Accept(this, context));
            var newRight = Materialize(node.Right.Accept(this, context));

            if (ReferenceEquals(newLeft, node.Left) && ReferenceEquals(newRight, node.Right))
            {
                // same
                return Ret(node);
            }

            // A branch was rewritten, create new node. 
            var newNode = new BinaryOpNode(node.IRContext, node.Op, newLeft, newRight);
            return Ret(newNode);
        }

        public override TResult Visit(UnaryOpNode node, TContext context)
        {
            var newChild = Materialize(node.Child.Accept(this, context));
            if (ReferenceEquals(newChild, node.Child))
            {
                return Ret(node);
            }
            
            var newNode = new UnaryOpNode(node.IRContext, node.Op, newChild);
            return Ret(newNode);
        }

        public override TResult Visit(ScopeAccessNode node, TContext context)
        {
            return Ret(node);
        }

        public override TResult Visit(RecordFieldAccessNode node, TContext context)
        {
            var newFrom = Materialize(node.From.Accept(this, context));
            if (ReferenceEquals(newFrom, node.From))
            {
                return Ret(node);
            }

            var newNode = new RecordFieldAccessNode(node.IRContext, newFrom, node.Field);
            return Ret(newNode);
        }

        public override TResult Visit(ResolvedObjectNode node, TContext context)
        {
            return Ret(node);
        }

        public override TResult Visit(SingleColumnTableAccessNode node, TContext context)
        {
            throw new NotImplementedException();
        }

        public override TResult Visit(ChainingNode node, TContext context)
        {
            var newArgs = VisitList(node.Nodes, context);
            if (newArgs == null)
            {
                return Ret(node);
            }

            var newNode = new ChainingNode(node.IRContext, newArgs);
            return Ret(newNode);
        }

        public override TResult Visit(AggregateCoercionNode node, TContext context)
        {
            IntermediateNode newChild = Materialize(node.Child.Accept(this, context));

            var newFields = VisitDict(node.FieldCoercions, context);

            if (newFields == null && ReferenceEquals(newChild, node.Child))
            {
                return Ret(node);
            }

            var newNode = new AggregateCoercionNode(node.IRContext, node.Op, node.Scope, newChild, newFields ?? node.FieldCoercions);
            return Ret(newNode);
        }
    }
}
