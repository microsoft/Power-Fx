// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Syntax.SourceInformation;

namespace Microsoft.PowerFx.Syntax
{
    public sealed class RecordOfNode : TexlNode
    {
        internal FirstNameNode TableName { get; }

        internal RecordOfNode(ref int idNext, Token firstToken, FirstNameNode tableName, SourceList sources)
            : base(ref idNext, firstToken, sources)
        {
            TableName = tableName;
        }

        internal override TexlNode Clone(ref int idNext, Span ts)
        {
            var tableName = TableName.Clone(ref idNext, ts);
            var newNodes = new Dictionary<TexlNode, TexlNode>
            {
                { TableName, tableName }
            };

            return new RecordOfNode(ref idNext, Token.Clone(ts).As<Token>(), TableName, this.SourceList.Clone(ts, newNodes));
        }

        /// <inheritdoc />
        public override void Accept(TexlVisitor visitor)
        {
            Contracts.AssertValue(visitor);
            visitor.Visit(this);
        }

        /// <inheritdoc />
        public override TResult Accept<TResult, TContext>(TexlFunctionalVisitor<TResult, TContext> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }

        /// <inheritdoc />
        public override NodeKind Kind => NodeKind.RecordOf;
    }
}
