// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;

namespace Microsoft.PowerFx.Types
{
    [DebuggerDisplay("{_tree}")]
    internal class LambdaFormulaValue : FormulaValue
    {
        private readonly IntermediateNode _tree;

        internal EvalVisitor Runner { get; }

        internal EvalVisitorContext Context { get; }

        // Lambdas don't get a special type. 
        // Type is the type the lambda evaluates too. 
        public LambdaFormulaValue(IRContext irContext, IntermediateNode node)
            : this(irContext, node, null, null)
        {
        }

        public LambdaFormulaValue(IRContext irContext, IntermediateNode node, EvalVisitor visitor, EvalVisitorContext? context)
            : base(irContext)
        {
            _tree = node;
            Runner = visitor;
            Context = context ?? default;
        }

        public async ValueTask<FormulaValue> EvalAsync(EvalVisitor runner, EvalVisitorContext context)
        {
            runner.CheckCancel();
            var result = await _tree.Accept(runner, context);
            return result;
        }

        public override object ToObject()
        {
            return "<Lambda>";
        }

        public override void Visit(IValueVisitor visitor)
        {
            throw new NotImplementedException();
        }

        internal TResult Visit<TResult, TContext>(IRNodeVisitor<TResult, TContext> visitor, TContext context)
        {
            return _tree.Accept(visitor, context);
        }

        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings)
        {
            // Internal only.
            throw new NotImplementedException("LambdaFormulaValue cannot be serialized.");
        }
    }
}
