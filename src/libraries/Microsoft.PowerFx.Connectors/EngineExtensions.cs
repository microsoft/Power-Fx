using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;

namespace Microsoft.PowerFx.Connectors
{
    public static class EngineExtensions
    {
        public static void EnableTabularConnectors(this Engine engine)
        {
            IRTransform t = new TabularTransform(hooks, maxRows);

            engine.IRTransformList.Add(t);
        }
    }

    private class TabularTransform : IRTransform
    {
        private readonly DelegationEngineExtensions.DelegationHooks _hooks;
        private readonly int _maxRows;

        public DelegationIRTransform(DelegationEngineExtensions.DelegationHooks hooks, int maxRows)
            : base("DelegationIRTransform")
        {
            _hooks = hooks;
            _maxRows = maxRows;
        }

        public override IntermediateNode Transform(IntermediateNode node, ICollection<ExpressionError> errors)
        {
            var visitor = new DelegationIRVisitor(_hooks, errors, _maxRows);
            var context = new DelegationIRVisitor.Context();

            var ret = node.Accept(visitor, context);
            var result = visitor.Materialize(ret);
            return result;
        }
    }
}
