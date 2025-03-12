// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.IR.Nodes;

namespace Microsoft.PowerFx.Core.IR
{
    internal class PredicateIRVisitor : IRNodeVisitor<PredicateIRVisitor.RetVal, PredicateIRVisitor.Context>
    {
        public override RetVal Visit(TextLiteralNode node, Context context)
        {
            throw new NotImplementedException();
        }

        public override RetVal Visit(NumberLiteralNode node, Context context)
        {
            throw new NotImplementedException();
        }

        public override RetVal Visit(DecimalLiteralNode node, Context context)
        {
            throw new NotImplementedException();
        }

        public override RetVal Visit(BooleanLiteralNode node, Context context)
        {
            throw new NotImplementedException();
        }

        public override RetVal Visit(ColorLiteralNode node, Context context)
        {
            throw new NotImplementedException();
        }

        public override RetVal Visit(RecordNode node, Context context)
        {
            throw new NotImplementedException();
        }

        public override RetVal Visit(ErrorNode node, Context context)
        {
            throw new NotImplementedException();
        }

        public override RetVal Visit(LazyEvalNode node, Context context)
        {
            throw new NotImplementedException();
        }

        public override RetVal Visit(CallNode node, Context context)
        {
            throw new NotImplementedException();
        }

        public override RetVal Visit(BinaryOpNode node, Context context)
        {
            throw new NotImplementedException();
        }

        public override RetVal Visit(UnaryOpNode node, Context context)
        {
            throw new NotImplementedException();
        }

        public override RetVal Visit(ScopeAccessNode node, Context context)
        {
            throw new NotImplementedException();
        }

        public override RetVal Visit(RecordFieldAccessNode node, Context context)
        {
            throw new NotImplementedException();
        }

        public override RetVal Visit(ResolvedObjectNode node, Context context)
        {
            throw new NotImplementedException();
        }

        public override RetVal Visit(SingleColumnTableAccessNode node, Context context)
        {
            throw new NotImplementedException();
        }

        public override RetVal Visit(ChainingNode node, Context context)
        {
            throw new NotImplementedException();
        }

        public override RetVal Visit(AggregateCoercionNode node, Context context)
        {
            throw new NotImplementedException();
        }

        public class RetVal
        {
        }

        public class Context
        {
        }
    }
}
