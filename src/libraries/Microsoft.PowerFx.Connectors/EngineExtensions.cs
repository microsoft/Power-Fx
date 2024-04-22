// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;

namespace Microsoft.PowerFx.Connectors
{
    public static class EngineExtensions
    {
        // To support tabular connectors, we need to use an IR transform to inject the ServiceProvider at runtime (which contains the HttpClient)
        [Obsolete("Might not be needed in future.")]
        public static void EnableTabularConnectors(this Engine engine)
        {
            engine.IRTransformList.Add(new TabularTransform());
        }

        private class TabularTransform : IRTransform
        {
            public TabularTransform()
                : base(nameof(TabularTransform))
            {
            }

            public override IntermediateNode Transform(IntermediateNode node, ICollection<ExpressionError> errors)
            {
                TabularIRVisitor visitor = new TabularIRVisitor();
                TabularIRVisitor.ODataContext context = new TabularIRVisitor.ODataContext();

                TabularIRVisitor.RetVal ret = node.Accept(visitor, context);
                IntermediateNode result = visitor.Materialize(ret);

                foreach (ExpressionError ee in visitor.Errors)
                {
                    errors.Add(ee);
                }

                return result;
            }
        }
    }
}
