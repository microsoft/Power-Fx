// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    internal class TabularIRVisitor : RewritingIRVisitor<TabularIRVisitor.RetVal, TabularIRVisitor.Context>
    {
        public TabularIRVisitor()
        {
        }

        public override RetVal Visit(ResolvedObjectNode node, Context context)
        {
            // Key test to determine when we will add InjectServiceProviderFunction call: only for ConnectorDType nodes (created in ConnectorTableType constructor)
            if (node.IRContext.ResultType._type is ConnectorDType dType)
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

            return new CallNode(IRContext.NotInSource(ret.RecordType), new InjectServiceProviderFunction(), ret.OriginalNode);
        }

        protected override RetVal Ret(IntermediateNode node)
        {
            return new RetVal(node);
        }

        public class RetVal
        {
            internal readonly IntermediateNode OriginalNode;
            internal readonly bool NeedsInjection;
            internal readonly RecordType RecordType;

            public RetVal(IntermediateNode node, bool needsInjection = false, ConnectorDType dType = null)
            {
                OriginalNode = node;
                NeedsInjection = needsInjection;
                RecordType = dType?.RecordType;
            }
        }

        public class Context
        {
        }
    }
}
