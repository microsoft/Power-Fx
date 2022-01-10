// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.IR;
using System;
using System.Diagnostics;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerFx
{
    [DebuggerDisplay("{_tree}")]
    internal class LambdaFormulaValue : FormulaValue
    {
        private readonly IntermediateNode _tree;

        // Lambdas don't get a special type. 
        // Type is the type the lambda evaluates too. 
        public LambdaFormulaValue(IRContext irContext, IntermediateNode node)
            : base(irContext)
        {
            _tree = node;
        }

        public FormulaValue Eval(EvalVisitor runner, SymbolContext context)
        {
            var result = _tree.Accept(runner, context);
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
    }
}
