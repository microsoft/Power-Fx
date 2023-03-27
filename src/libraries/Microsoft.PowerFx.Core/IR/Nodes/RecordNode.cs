// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.IR.Nodes
{
    internal sealed class RecordNode : IntermediateNode
    {
        public readonly IReadOnlyDictionary<DName, IntermediateNode> Fields;

        public RecordNode(IRContext irContext, IReadOnlyDictionary<DName, IntermediateNode> fields)
            : base(irContext)
        {
            Contracts.AssertAllValid(fields.Keys);
            Contracts.AssertAllValues(fields.Values);

            Fields = fields;
        }

        public override TResult Accept<TResult, TContext>(IRNodeVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        public override string ToString()
        {
            return $"{string.Join(", ", Fields.Select(kvp => $"{{{kvp.Key}: {kvp.Value}}}"))}";
        }
    }
}
