// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    internal class CdpIRVisitor : RewritingIRVisitor<CdpIRVisitor.RetVal, CdpIRVisitor.Context>
    {
        public CdpIRVisitor()
        {
        }

        public override RetVal Visit(ResolvedObjectNode node, Context context)
        {
            // Key test to determine when we will add InjectServiceProviderFunction call: only for TabularDType nodes (created in ConnectorTableType constructor)
            if (node.IRContext.ResultType._type is CdpDtype dType)
            {
                return new RetVal(node, true, dType);
            }

            return new RetVal(node);
        }

        public override RetVal Visit(CallNode node, Context context)
        {
            return base.Visit(node, context);
        }

        // This is where we add InjectServiceProviderFunction call in the IR
        public override IntermediateNode Materialize(RetVal ret)
        {
            if (!ret.NeedsInjection)
            {
                return ret.OriginalNode;
            }

            return new CallNode(IRContext.NotInSource(ret.TableType), new InjectServiceProviderFunction(), ret.OriginalNode);
        }

        protected override RetVal Ret(IntermediateNode node)
        {
            return new RetVal(node);
        }

        public class RetVal
        {
            internal readonly IntermediateNode OriginalNode;

            internal readonly bool NeedsInjection;

            internal readonly TableType TableType;

            public RetVal(IntermediateNode node, bool needsInjection = false, CdpDtype dType = null)
            {
                OriginalNode = node;
                NeedsInjection = needsInjection;
                TableType = dType?.TableType;
            }
        }

        public class Context
        {
        }
    }
}
