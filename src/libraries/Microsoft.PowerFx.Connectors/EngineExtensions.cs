// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    public static class EngineExtensions
    {
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
                TabularIRVisitor.Context context = new TabularIRVisitor.Context();

                TabularIRVisitor.RetVal ret = node.Accept(visitor, context);
                IntermediateNode result = visitor.Materialize(ret);
                return result;
            }
        }
    }
}
