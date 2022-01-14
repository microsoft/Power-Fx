// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.IR.Nodes
{
    internal sealed class CallNode : IntermediateNode
    {
        public readonly TexlFunction Function;
        public readonly List<IntermediateNode> Args;

        /// <summary>
        /// Scope is non-null if the function creates a scope.
        /// </summary>
        public readonly ScopeSymbol Scope;

        public CallNode(IRContext irContext, TexlFunction func, params IntermediateNode[] args)
            : base(irContext)
        {
            Contracts.AssertValue(func);
            Contracts.AssertAllValues(args);

            Function = func;
            Args = args.ToList();
        }

        public CallNode(IRContext irContext, TexlFunction func, IList<IntermediateNode> args)
            : base(irContext)
        {
            Contracts.AssertValue(func);
            Contracts.AssertAllValues(args);

            Function = func;
            Args = args.ToList();
        }

        public CallNode(IRContext irContext, TexlFunction func, ScopeSymbol scope, IList<IntermediateNode> args)
            : this(irContext, func, args)
        {
            Contracts.AssertValue(scope);

            Scope = scope;
        }

        public bool TryGetArgument(int i, out IntermediateNode arg)
        {
            arg = default;
            if (i > Args.Count && i < 0)
            {
                return false;
            }

            arg = Args[i];
            return true;
        }

        public bool IsLambdaArg(int i)
        {
            if (i > Args.Count && i < 0)
            {
                return false;
            }

            return Args[i] is LazyEvalNode;
        }

        public override TResult Accept<TResult, TContext>(IRNodeVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        public override string ToString()
        {
            string result;
            if (Scope != null)
            {
                result = $"Call({Function.Name}, {Scope}";
            }
            else
            {
                result = $"Call({Function.Name}";
            }

            foreach (var arg in Args)
            {
                result += $", {arg}";
            }

            result += ")";
            return result;
        }
    }
}
