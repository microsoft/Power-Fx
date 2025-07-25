﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Syntax.Visitors
{
    // Visitor to resolve TypeLiteralNode.TypeRoot into DType.
    internal class DTypeVisitor : DefaultVisitor<DType, INameResolver>
    {
        private DTypeVisitor() 
            : base(DType.Invalid)
        {
        }

        public static DType Run(TexlNode node, INameResolver context)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(context);

            return node.Accept(new DTypeVisitor(), context);
        }

        public override DType Visit(FirstNameNode node, INameResolver context)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(context);

            var name = node.Ident.Name;
            if (context.LookupType(name, out FormulaType ft))
            {
                return ft._type;
            }

            return DType.Invalid;
        }

        public override DType Visit(RecordNode node, INameResolver context)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(context);

            if (node.ChildNodes.Count < 1)
            {
                return DType.Invalid;
            }

            var typedNames = new List<TypedName>();
            var names = new HashSet<DName>();

            foreach (var (cNode, ident) in node.ChildNodes.Zip(node.Ids, (a, b) => (a, b)))
            {
                var name = ident.Name;

                // Invalid if Record fields repeat.
                if (!names.Add(name))
                {
                    return DType.Invalid;
                }

                var ty = cNode.Accept(this, context);

                if (ty == DType.Invalid || ty.IsVoid)
                {
                    return DType.Invalid;
                }

                typedNames.Add(new TypedName(ty, name));
            }

            return DType.CreateRecord(typedNames);
        }

        public override DType Visit(TableNode node, INameResolver context)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(context);

            if (node.ChildNodes.Count != 1)
            {
                return DType.Invalid;
            }

            var childNode = node.ChildNodes.First();
            var ty = childNode.Accept(this, context);

            if (ty == DType.Invalid || ty.IsVoid)
            {
                return DType.Invalid;
            }

            if (ty.IsRecord || ty.IsTable)
            {
                return ty.ToTable();
            }

            // Single column table syntax
            var rowType = DType.EmptyRecord.Add(new TypedName(ty, TableValue.ValueDName));

            return rowType.ToTable();
        }

        public override DType Visit(CallNode node, INameResolver context)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(context);
            Contracts.AssertValue(node.Args);
            Contracts.AssertAllValues(node.Args.ChildNodes);

            if (!TypeLiteralNode.ValidRecordOfNode(node))
            {
                return DType.Invalid;
            }

            Contracts.Assert(node.Args.ChildNodes.Count == 1);

            var childType = node.Args.ChildNodes.Single().Accept(this, context);

            if (!childType.IsTable)
            {
                return DType.Invalid;
            }

            return childType.ToRecord();
        }
    }
}
