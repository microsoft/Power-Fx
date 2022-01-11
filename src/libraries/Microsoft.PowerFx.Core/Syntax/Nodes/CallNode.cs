// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.SourceInformation;
using Microsoft.PowerFx.Core.Syntax.Visitors;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax.Nodes
{
    internal sealed class CallNode : TexlNode
    {
        public readonly Identifier Head;
        public readonly ListNode Args;
        // ParenClose can be null.
        public readonly Token ParenClose;
        // HeadNode is null for simple invocations. It is typically non-null if the head is
        // a more complex expression, e.g. a non-identifier, or a namespace-qualified identifier
        // in the form of a DottedNameNode.
        public readonly TexlNode HeadNode;

        // Unique invocation id for this node. This is used in order to uniquely identify this node in the document.
        public readonly string UniqueInvocationId;

        // Parse Tree is assigned a unique id that is used later to create unique node ids.
        private static volatile int _uniqueInvocationIdNext;

        public CallNode(ref int idNext, Token primaryToken, SourceList sourceList, Identifier head, TexlNode headNode, ListNode args, Token tokParenClose)
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

#pragma warning disable 420
            // A volatile field should not normally be passed using a ref or out parameter, since it will not be treated
            // as volatile within the scope of the function. There are exceptions to this, such as when calling an interlocked API.
            // Hence disabling the warning for this instance.
            var invocationId = Interlocked.Increment(ref _uniqueInvocationIdNext);
#pragma warning restore 420

            // We need to generate a globally unique name for this function invocation, so we use
            // a new (hardcoded) guid as well as the unique counter to avoid colliding with any
            // other data sources that may be imported by the user.
            UniqueInvocationId = string.Format("Inv_7339A45FDB3141D49CB36063B712F5E0_{0}", invocationId);
        }

        public override TexlNode Clone(ref int idNext, Span ts)
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

        public override void Accept(TexlVisitor visitor)
        {
            Contracts.AssertValue(visitor);
            if (visitor.PreVisit(this))
            {
                Args.Accept(visitor);
                visitor.PostVisit(this);
            }
        }

        public override Result Accept<Result, Context>(TexlFunctionalVisitor<Result, Context> visitor, Context context)
        {
            return visitor.Visit(this, context);
        }

        public override NodeKind Kind => NodeKind.Call;

        public override CallNode CastCall()
        {
            return this;
        }

        public override CallNode AsCall()
        {
            return this;
        }

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
        public bool HasArgumentAsyncWithNoSideEffects(TexlBinding binding, int firstArgument = 0)
        {
            // check if the CallNode has any async arguments.
            // some functions don't need to look at all
            // arguments (e.g. Filter and LookUp where the first arg is a data source)
            return Args.Children.Skip(firstArgument).Any(x => binding.IsAsyncWithNoSideEffects(x));
        }
    }
}