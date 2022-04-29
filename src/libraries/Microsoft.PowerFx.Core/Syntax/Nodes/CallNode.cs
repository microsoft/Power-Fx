// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Syntax.SourceInformation;

namespace Microsoft.PowerFx.Syntax
{
    /// <summary>
    /// Function call parse node. Example:
    /// 
    /// <code>Head(Args...)</code>
    /// </summary>
    public sealed class CallNode : TexlNode
    {
        /// <summary>
        /// The identifier of the function call.
        /// </summary>
        public Identifier Head { get; }

        /// <summary>
        /// The argument list of the function call.
        /// </summary>
        public ListNode Args { get; }

        // ParenClose can be null.
        internal readonly Token ParenClose;

        // HeadNode is null for simple invocations. It is typically non-null if the head is
        // a more complex expression, e.g. a non-identifier, or a namespace-qualified identifier
        // in the form of a DottedNameNode.
        internal readonly TexlNode HeadNode;

        // Unique invocation id for this node. This is used in order to uniquely identify this node in the document.
        internal readonly string UniqueInvocationId;

        // Parse Tree is assigned a unique id that is used later to create unique node ids.
        internal static volatile int _uniqueInvocationIdNext;

        internal CallNode(ref int idNext, Token primaryToken, SourceList sourceList, Identifier head, TexlNode headNode, ListNode args, Token tokParenClose)
            : base(ref idNext, primaryToken, sourceList)
        {
            Contracts.AssertValue(head);
            Contracts.AssertValueOrNull(headNode);
            Contracts.AssertValue(args);
            Contracts.AssertValueOrNull(tokParenClose);

            Head = head;
            HeadNode = headNode;
            Args = args;
            Args.Parent = this;
            ParenClose = tokParenClose;

            var headDepth = HeadNode == null ? 0 : HeadNode.Depth;
            _depth = 1 + (args.Depth > headDepth ? args.Depth : headDepth);

            if (headNode != null)
            {
                MinChildID = Math.Min(headNode.MinChildID, MinChildID);
            }

            if (args != null)
            {
                MinChildID = Math.Min(args.MinChildID, MinChildID);
            }

            // A volatile field should not normally be passed using a ref or out parameter, since it will not be treated
            // as volatile within the scope of the function. There are exceptions to this, such as when calling an interlocked API.
            // Hence disabling the warning for this instance.
            var invocationId = Interlocked.Increment(ref _uniqueInvocationIdNext);

            // We need to generate a globally unique name for this function invocation, so we use
            // a new (hardcoded) guid as well as the unique counter to avoid colliding with any
            // other data sources that may be imported by the user.
            UniqueInvocationId = string.Format("Inv_7339A45FDB3141D49CB36063B712F5E0_{0}", invocationId);
        }

        internal override TexlNode Clone(ref int idNext, Span ts)
        {
            var args = Args.Clone(ref idNext, ts).AsList();
            var newNodes = new Dictionary<TexlNode, TexlNode>
            {
                { Args, args }
            };

            TexlNode headNode = null;
            if (HeadNode != null)
            {
                headNode = HeadNode.Clone(ref idNext, ts);
                newNodes.Add(HeadNode, headNode);
            }

            return new CallNode(ref idNext, Token.Clone(ts), SourceList.Clone(ts, newNodes), Head.Clone(ts), headNode, args, ParenClose);
        }

        /// <inheritdoc />
        public override void Accept(TexlVisitor visitor)
        {
            Contracts.AssertValue(visitor);
            if (visitor.PreVisit(this))
            {
                Args.Accept(visitor);
                visitor.PostVisit(this);
            }
        }

        /// <inheritdoc />
        public override TResult Accept<TResult, TContext>(TexlFunctionalVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        /// <inheritdoc />
        public override NodeKind Kind => NodeKind.Call;

        internal override CallNode CastCall()
        {
            return this;
        }

        internal override CallNode AsCall()
        {
            return this;
        }

        /// <inheritdoc />
        public override Span GetTextSpan()
        {
            if (ParenClose == null)
            {
                return base.GetTextSpan();
            }

            // If the call is a Service call then adjust the span for the entire call
            DottedNameNode dotted;
            if (HeadNode != null && (dotted = HeadNode.AsDottedName()) != null)
            {
                return new Span(dotted.GetCompleteSpan().Min, ParenClose.Span.Lim);
            }

            return new Span(Head.Token.Span.Min, ParenClose.Span.Lim);
        }

        /// <inheritdoc />
        public override Span GetCompleteSpan()
        {
            int limit;

            // If we have a close paren, then the call node is complete.
            // If not, then the call node ends with the end of the last argument.
            if (ParenClose != null)
            {
                limit = ParenClose.Span.Lim;
            }
            else
            {
                limit = Args.GetCompleteSpan().Lim;
            }

            DottedNameNode dotted;
            if (HeadNode != null && (dotted = HeadNode.AsDottedName()) != null)
            {
                return new Span(dotted.GetCompleteSpan().Min, limit);
            }

            return new Span(Head.Token.Span.Min, limit);
        }

        // Does the CallNode have an argument/expression that is async without side effects
        // Check 1..N arguments to identify if there is an AsyncWithNoSideEffects expression.
        internal bool HasArgumentAsyncWithNoSideEffects(TexlBinding binding, int firstArgument = 0)
        {
            // check if the CallNode has any async arguments.
            // some functions don't need to look at all
            // arguments (e.g. Filter and LookUp where the first arg is a data source)
            return Args.Children.Skip(firstArgument).Any(x => binding.IsAsyncWithNoSideEffects(x));
        }
    }
}
