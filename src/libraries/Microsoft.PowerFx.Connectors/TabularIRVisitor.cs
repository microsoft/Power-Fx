// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Connectors.Internal;
using Microsoft.PowerFx.Core.Functions.OData;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;

namespace Microsoft.PowerFx.Connectors
{
    internal class TabularIRVisitor : RewritingIRVisitor<TabularIRVisitor.RetVal, TabularIRVisitor.ODataContext>
    {
        internal List<ExpressionError> Errors = new List<ExpressionError>();

        public TabularIRVisitor()
        {
        }

        public override RetVal Visit(ResolvedObjectNode node, ODataContext context)
        {
            // Key test to determine when we will add InjectServiceProviderFunction call: only for TabularDType nodes (created in ConnectorTableType constructor)
            if (node.IRContext.ResultType._type is TabularDType dType)
            {
                return new RetVal(node, true, dType);
            }

            return new RetVal(node);
        }

        public override RetVal Visit(CallNode node, ODataContext context)
        {
            RetVal rv = base.Visit(node, context);

            if (rv.Node is CallNode callNode && callNode.Function is IODataFunction oDataFunction)
            {
                if (callNode.Args[0] is CallNode innerCallNode && innerCallNode.Function is InjectServiceProviderFunction ispf)
                {
                    context.ODataTypeProcessor = new ODataCommandProcessor(ispf.TabularDType);
                }

                bool b = context.ODataTypeProcessor.TryAddODataCommand(oDataFunction.GetODataCommand(callNode));

                if (!b)
                {
                    Errors.Add(new ExpressionError() { Severity = ErrorSeverity.Warning, Message = $"Cannot delegate function {callNode.Function.Name}" });
                }
            }

            return rv;
        }

        // This is where we add InjectServiceProviderFunction call in the IR
        public override IntermediateNode Materialize(RetVal ret)
        {
            if (!ret.NeedsInjection)
            {
                return ret.Node;
            }

            return new CallNode(IRContext.NotInSource(ret.TableType.TableType), new InjectServiceProviderFunction(ret.TableType), ret.Node);
        }

        protected override RetVal Ret(IntermediateNode node)
        {
            return new RetVal(node);
        }      

        public class RetVal
        {
            internal readonly IntermediateNode Node;

            internal readonly bool NeedsInjection;

            internal readonly TabularDType TableType;            

            public RetVal(IntermediateNode node, bool needsInjection = false, TabularDType dType = null)
            {
                Node = node;
                NeedsInjection = needsInjection;
                TableType = dType;
            }
        }

        public class ODataContext
        {
            public ODataCommandProcessor ODataTypeProcessor;

            public ODataContext()
            {
                ODataTypeProcessor = null;
            }
        }
    }
}
