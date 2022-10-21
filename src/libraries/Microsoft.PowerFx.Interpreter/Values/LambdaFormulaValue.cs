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

        private readonly EvalVisitor _runner;

        private EvalVisitorContext Context { get; set; }

        // Lambdas don't get a special type. 
        // Type is the type the lambda evaluates too. 
        public LambdaFormulaValue(IRContext irContext, IntermediateNode node, EvalVisitor visitor, EvalVisitorContext context)
            : base(irContext)
        {
            _tree = node;
            _runner = visitor;
            Context = context;
        }

        public async ValueTask<FormulaValue> EvalAsync()
        {
            _runner.CheckCancel();
            var result = await _tree.Accept(_runner, Context);
            return result;
        }

        public async ValueTask<FormulaValue> EvalInRowScopeAsync(EvalVisitorContext context)
        {
            Context = context;
            return await EvalAsync();
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
