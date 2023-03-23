﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.IR.Nodes
{
    internal sealed class TextLiteralNode : IntermediateNode
    {
        public readonly string LiteralValue;

        public TextLiteralNode(IRContext irContext, string value)
            : base(irContext)
        {
            LiteralValue = value;
        }

        public override TResult Accept<TResult, TContext>(IRNodeVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        public override string ToString()
        {
            return $"\"{CharacterUtils.ExcelEscapeString(LiteralValue)}\":s";
        }
    }
}
