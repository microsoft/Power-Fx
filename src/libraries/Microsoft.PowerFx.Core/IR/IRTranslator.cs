// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using BinaryOpNode = Microsoft.PowerFx.Core.IR.Nodes.BinaryOpNode;
using CallNode = Microsoft.PowerFx.Core.IR.Nodes.CallNode;
using ErrorNode = Microsoft.PowerFx.Core.IR.Nodes.ErrorNode;
using RecordNode = Microsoft.PowerFx.Core.IR.Nodes.RecordNode;
using TexlBinaryOpNode = Microsoft.PowerFx.Syntax.BinaryOpNode;
using TexlCallNode = Microsoft.PowerFx.Syntax.CallNode;
using TexlErrorNode = Microsoft.PowerFx.Syntax.ErrorNode;
using TexlRecordNode = Microsoft.PowerFx.Syntax.RecordNode;
using TexlTableNode = Microsoft.PowerFx.Syntax.TableNode;
using TexlUnaryOpNode = Microsoft.PowerFx.Syntax.UnaryOpNode;
using UnaryOpNode = Microsoft.PowerFx.Core.IR.Nodes.UnaryOpNode;

namespace Microsoft.PowerFx.Core.IR
{
    internal class IRTranslator
    {
        /// <summary>
        /// Returns the top node of the IR tree, and a symbol that corresponds to the Rule Scope.
        /// </summary>
        public static (IntermediateNode topNode, ScopeSymbol ruleScopeSymbol) Translate(TexlBinding binding)
        {
            Contracts.AssertValue(binding);

            var ruleScopeSymbol = new ScopeSymbol(0);
            return (binding.Top.Accept(new IRTranslatorVisitor(), new IRTranslatorContext(binding, ruleScopeSymbol)), ruleScopeSymbol);
        }

        private class IRTranslatorVisitor : TexlFunctionalVisitor<IntermediateNode, IRTranslatorContext>
        {
            public override IntermediateNode Visit(BlankNode node, IRTranslatorContext context)
            {
                Contracts.AssertValue(node);
                Contracts.AssertValue(context);

                return MaybeInjectCoercion(node, new CallNode(context.GetIRContext(node), BuiltinFunctionsCore.Blank), context);
            }

            public override IntermediateNode Visit(BoolLitNode node, IRTranslatorContext context)
            {
                Contracts.AssertValue(node);
                Contracts.AssertValue(context);

                return MaybeInjectCoercion(node, new BooleanLiteralNode(context.GetIRContext(node), node.Value), context);
            }

            public override IntermediateNode Visit(StrLitNode node, IRTranslatorContext context)
            {
                Contracts.AssertValue(node);
                Contracts.AssertValue(context);

                return MaybeInjectCoercion(node, new TextLiteralNode(context.GetIRContext(node), node.Value), context);
            }

            public override IntermediateNode Visit(NumLitNode node, IRTranslatorContext context)
            {
                Contracts.AssertValue(node);
                Contracts.AssertValue(context);

                // I think node.NumValue might be dead code, this could be cleaned up
                var value = node.Value?.Value ?? node.NumValue;
                return MaybeInjectCoercion(node, new NumberLiteralNode(context.GetIRContext(node), value), context);
            }

            public override IntermediateNode Visit(TexlRecordNode node, IRTranslatorContext context)
            {
                Contracts.AssertValue(node);
                Contracts.AssertValue(context);

                var values = new Dictionary<DName, IntermediateNode>();
                for (var i = 0; i < node.Count; i++)
                {
                    var childNode = node.Children[i];
                    var identifierName = context.Binding.TryGetReplacedIdentName(node.Ids[i], out var newIdent) ? new DName(newIdent) : node.Ids[i].Name;

                    var childIR = childNode.Accept(this, context);

                    values.Add(identifierName, childIR);
                }

                var recordNode = new RecordNode(context.GetIRContext(node), values);
                return MaybeInjectCoercion(node, recordNode, context);
            }

            public override IntermediateNode Visit(TexlTableNode node, IRTranslatorContext context)
            {
                Contracts.AssertValue(node);
                Contracts.AssertValue(context);

                var children = node.Children.Select(child => child.Accept(this, context)).ToArray();
                var irContext = context.GetIRContext(node);

                if (!context.Binding.Features.HasTableSyntaxDoesntWrapRecords() || (children.Any() && children.First() is not RecordNode))
                {
                    // Let's add "Value:" here                    
                    children = children.Select(childNode =>
                        new RecordNode(
                            new IRContext(childNode.IRContext.SourceContext, new RecordType().Add(TableValue.ValueName, childNode.IRContext.ResultType)),
                            new Dictionary<DName, IntermediateNode>
                            {
                                { TableValue.ValueDName, childNode }
                            }))
                        .ToArray();

                    irContext = new IRContext(node.GetCompleteSpan(), irContext.ResultType);
                }

                return MaybeInjectCoercion(node, new CallNode(irContext, BuiltinFunctionsCore.Table, children), context);
            }

            public override IntermediateNode Visit(TexlUnaryOpNode node, IRTranslatorContext context)
            {
                Contracts.AssertValue(node);
                Contracts.AssertValue(context);

                var child = node.Child.Accept(this, context);

                IntermediateNode result;
                switch (node.Op)
                {
                    case UnaryOp.Not:
                        result = new CallNode(context.GetIRContext(node), BuiltinFunctionsCore.Not, child);
                        break;
                    case UnaryOp.Minus:
                        result = new UnaryOpNode(context.GetIRContext(node), UnaryOpKind.Negate, child);
                        break;
                    case UnaryOp.Percent:
                        result = new UnaryOpNode(context.GetIRContext(node), UnaryOpKind.Percent, child);
                        break;
                    default:
                        throw new NotSupportedException();
                }

                return MaybeInjectCoercion(node, result, context);
            }

            public override IntermediateNode Visit(TexlBinaryOpNode node, IRTranslatorContext context)
            {
                Contracts.AssertValue(node);
                Contracts.AssertValue(context);

                var left = node.Left.Accept(this, context);
                var right = node.Right.Accept(this, context);

                var kind = BinaryOpMatrix.GetBinaryOpKind(node, context.Binding);

                IntermediateNode binaryOpResult;
                
                switch (kind)
                {
                    // Call Node Replacements:
                    case BinaryOpKind.Power:
                        binaryOpResult = new CallNode(context.GetIRContext(node), BuiltinFunctionsCore.Power, left, right);
                        break;
                    case BinaryOpKind.Concatenate:
                        binaryOpResult = new CallNode(context.GetIRContext(node), BuiltinFunctionsCore.Concatenate, left, right);
                        break;
                    case BinaryOpKind.Or:
                    case BinaryOpKind.And:
                        binaryOpResult = new CallNode(context.GetIRContext(node), node.Op == BinaryOp.And ? BuiltinFunctionsCore.And : BuiltinFunctionsCore.Or, left, new LazyEvalNode(context.GetIRContext(node), right));
                        break;

                    // Reversed Args:
                    case BinaryOpKind.AddTimeAndDate:
                        binaryOpResult = new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.AddDateAndTime, right, left);
                        break;
                    case BinaryOpKind.AddDayAndDate:
                        binaryOpResult = new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.AddDateAndDay, right, left);
                        break;
                    case BinaryOpKind.AddMillisecondsAndTime:
                        binaryOpResult = new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.AddTimeAndMilliseconds, right, left);
                        break;
                    case BinaryOpKind.AddDayAndDateTime:
                        binaryOpResult = new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.AddDateTimeAndDay, right, left);
                        break;

                    case BinaryOpKind.Invalid:
                        if (node.Op == BinaryOp.NotEqual) 
                        {
                            binaryOpResult = new BooleanLiteralNode(context.GetIRContext(node), true);
                        }
                        else if (node.Op == BinaryOp.Equal)
                        {
                            binaryOpResult = new BooleanLiteralNode(context.GetIRContext(node), false);
                        }
                        else if (node.Op == BinaryOp.Error || context.Binding.ErrorContainer.HasErrors(node.Left) || context.Binding.ErrorContainer.HasErrors(node.Right))
                        {
                            return new ErrorNode(context.GetIRContext(node), node.ToString());
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }

                        break;

                    // Date Diff pulls from nested unary op
                    case BinaryOpKind.DateDifference:
                        // Validated in Matrix + Binder
                        if (right is not UnaryOpNode { Op: UnaryOpKind.Negate } unaryNegate)
                        {
                            throw new NotSupportedException();
                        }

                        binaryOpResult = new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.DateDifference, left, unaryNegate.Child);
                        break;

                    // All others used directly
                    default:
                        binaryOpResult = new BinaryOpNode(context.GetIRContext(node), kind, left, right);
                        break;
                }

                return MaybeInjectCoercion(node, binaryOpResult, context);
            }

            public override IntermediateNode Visit(TexlCallNode node, IRTranslatorContext context)
            {
                Contracts.AssertValue(node);
                Contracts.AssertValue(context);

                var info = context.Binding.GetInfo(node);
                var carg = node.Args.Count;
                var func = (TexlFunction)info.Function;

                var resultType = context.Binding.GetType(node);

                if (func == null || carg < func.MinArity || carg > func.MaxArity)
                {
                    throw new NotImplementedException();
                }

                var args = new List<IntermediateNode>();
                ScopeSymbol scope = null;
                if (func.ScopeInfo != null)
                {
                    scope = GetNewScope();
                }

                for (var i = 0; i < carg; ++i)
                {
                    var arg = node.Args.Children[i];
                    if (func.IsLazyEvalParam(i))
                    {
                        var child = arg.Accept(this, scope != null ? context.With(scope) : context);
                        args.Add(new LazyEvalNode(context.GetIRContext(arg), child));
                    }
                    else
                    {
                        args.Add(arg.Accept(this, context));
                    }
                }

                if (scope != null)
                {
                    return MaybeInjectCoercion(node, new CallNode(context.GetIRContext(node), func, scope, args), context);
                }

                return MaybeInjectCoercion(node, new CallNode(context.GetIRContext(node), func, args), context);
            }

            public override IntermediateNode Visit(FirstNameNode node, IRTranslatorContext context)
            {
                Contracts.AssertValue(node);
                Contracts.AssertValue(context);

                var info = context.Binding.GetInfo(node);
                if (info == null)
                {
                    // Binding previously failed for this node, we don't know enough to do something useful here
                    Contracts.Assert(context.Binding.ErrorContainer.HasErrors(node));
                    return new ErrorNode(context.GetIRContext(node), node.ToString());
                }

                var nodeName = context.Binding.TryGetReplacedIdentName(node.Ident, out var newIdent) ? new DName(newIdent) : node.Ident.Name;

                IntermediateNode result;
                switch (info.Kind)
                {
                    case BindKind.LambdaFullRecord:
                        {
                            // This is a full reference to the lambda param, e.g. "ThisRecord" in an expression like "Filter(T, ThisRecord.Price < 100)".
                            Contracts.Assert(info.UpCount >= 0);
                            Contracts.Assert(context.Scopes.Length - info.UpCount > 0);

                            var scope = context.Scopes[context.Scopes.Length - info.UpCount - 1];

                            result = new ScopeAccessNode(context.GetIRContext(node), scope);
                            break;
                        }

                    case BindKind.LambdaField:
                        {
                            // This is a normal bind, e.g. "Price" in an expression like "Filter(T, Price < 100)".
                            Contracts.Assert(info.UpCount >= 0);
                            Contracts.Assert(context.Scopes.Length - info.UpCount > 0);

                            var scope = context.Scopes[context.Scopes.Length - info.UpCount - 1];
                            var fieldAccess = new ScopeAccessSymbol(scope, scope.AddOrGetIndexForField(nodeName));

                            result = new ScopeAccessNode(context.GetIRContext(node), fieldAccess);
                            break;
                        }

                    case BindKind.OptionSet:
                    case BindKind.PowerFxResolvedObject:
                        {
                            result = new ResolvedObjectNode(context.GetIRContext(node), info.Data);
                            break;
                        }

                    default:
                        Contracts.Assert(false, "Unsupported Bindkind");
                        throw new NotImplementedException();
                }

                return MaybeInjectCoercion(node, result, context);
            }

            public override IntermediateNode Visit(DottedNameNode node, IRTranslatorContext context)
            {
                Contracts.AssertValue(node);
                Contracts.AssertValue(context);

                var typeLhs = context.Binding.GetType(node.Left);
                var nameRhs = context.Binding.TryGetReplacedIdentName(node.Right, out var newIdent) ? new DName(newIdent) : node.Right.Name;

                var resultType = context.Binding.GetType(node);
                IntermediateNode result;

                if (typeLhs.IsEnum)
                {
                    var value = context.Binding.GetInfo(node).VerifyValue().Data;
                    Contracts.Assert(value != null);

                    if (DType.Color.Accepts(resultType))
                    {
                        Contracts.Assert(value is uint);
                        result = new ColorLiteralNode(context.GetIRContext(node), (uint)value);
                    }
                    else if (DType.Number.Accepts(resultType))
                    {
                        result = new NumberLiteralNode(context.GetIRContext(node), (double)value);
                    }
                    else if (DType.String.Accepts(resultType))
                    {
                        result = new TextLiteralNode(context.GetIRContext(node), (string)value);
                    }
                    else if (DType.Boolean.Accepts(resultType))
                    {
                        result = new BooleanLiteralNode(context.GetIRContext(node), (bool)value);
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }

                    return MaybeInjectCoercion(node, result, context);
                }

                // Do not visit the left node for an enum bind
                var left = node.Left.Accept(this, context);

                if (typeLhs.IsOptionSet)
                {
                    result = new RecordFieldAccessNode(context.GetIRContext(node), left, nameRhs);
                }
                else if (typeLhs.IsView)
                {
                    Contracts.Assert(false, "Unsupported LHS Type for DottedNames");
                    throw new NotSupportedException();
                }
                else if (typeLhs.IsTable)
                {
                    result = new SingleColumnTableAccessNode(context.GetIRContext(node), left, nameRhs);
                }
                else if (node.UsesBracket)
                {
                    // Disambiguated scope field access.
                    // For example, MyData[@field], should resolve to the value of 'field' in the current row in MyData.
                    // In this case, left should be a ValueAccessNode where symbol == scope
                    if (left is ScopeAccessNode valueAccess && valueAccess.Value is ScopeSymbol scope)
                    {
                        result = new ScopeAccessNode(context.GetIRContext(node), new ScopeAccessSymbol(scope, scope.AddOrGetIndexForField(nameRhs)));
                    }
                    else
                    {
                        Contracts.Assert(false, "Scope Symbol not found for scope access");
                        return new ErrorNode(context.GetIRContext(node), node.ToString());
                    }
                }
                else if (typeLhs.IsRecord)
                {
                    // Field access within a record.
                    Contracts.Assert(typeLhs.IsRecord);

                    if (typeLhs.TryGetType(nameRhs, out var typeRhs) &&
                        typeRhs.IsExpandEntity &&
                        context.Binding.TryGetEntityInfo(node, out var expandInfo) &&
                        expandInfo.IsTable)
                    {
                        // No relationships in PFX yet
                        Contracts.Assert(false, "Relationships not yet supported");
                        throw new NotSupportedException();
                    }
                    else
                    {
                        if (node.Left is FirstNameNode namespaceNode && context.Binding.GetInfo(namespaceNode)?.Kind == BindKind.QualifiedValue)
                        {
                            Contracts.Assert(false, "QualifiedValues not yet supported by PowerFx");
                            throw new NotSupportedException();
                        }

                        if (left is ScopeAccessNode valueAccess && valueAccess.Value is ScopeSymbol scope)
                        {
                            result = new ScopeAccessNode(context.GetIRContext(node), new ScopeAccessSymbol(scope, scope.AddOrGetIndexForField(nameRhs)));
                        }
                        else
                        {
                            result = new RecordFieldAccessNode(context.GetIRContext(node), left, nameRhs);
                        }
                    }
                }
                else if (typeLhs.IsUntypedObject)
                {
                    // Field access within a custom object.
                    Contracts.Assert(typeLhs.IsUntypedObject);

                    var right = new TextLiteralNode(IRContext.NotInSource(FormulaType.String), nameRhs);

                    return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.DynamicGetField, left, right);
                }
                else
                {
                    Contracts.Assert(context.Binding.ErrorContainer.HasErrors(node.Left) || context.Binding.ErrorContainer.HasErrors(node));
                    return new ErrorNode(context.GetIRContext(node), node.ToString());
                }

                return MaybeInjectCoercion(node, result, context);
            }

            public override IntermediateNode Visit(VariadicOpNode node, IRTranslatorContext context)
            {
                Contracts.AssertValue(node);
                Contracts.AssertValue(context);

                if (node.Children.Length == 1)
                {
                    return MaybeInjectCoercion(node, node.Children[0].Accept(this, context), context);
                }

                var children = new List<IntermediateNode>();
                foreach (var child in node.Children)
                {
                    children.Add(child.Accept(this, context));
                }

                return MaybeInjectCoercion(node, new ChainingNode(context.GetIRContext(node), children), context);
            }

            public override IntermediateNode Visit(StrInterpNode node, IRTranslatorContext context)
            {
                Contracts.AssertValue(node);
                Contracts.AssertValue(context);

                if (node.Children.Length == 1)
                {
                    return MaybeInjectCoercion(node, node.Children[0].Accept(this, context), context);
                }

                var children = new List<IntermediateNode>();
                foreach (var child in node.Children)
                {
                    children.Add(child.Accept(this, context));
                }

                return MaybeInjectCoercion(node, new CallNode(context.GetIRContext(node), BuiltinFunctionsCore.Concatenate, children), context);
            }

            public override IntermediateNode Visit(AsNode node, IRTranslatorContext context)
            {
                Contracts.AssertValue(node);
                Contracts.AssertValue(context);

                return MaybeInjectCoercion(node, node.Left.Accept(this, context), context);
            }

            public override IntermediateNode Visit(TexlErrorNode node, IRTranslatorContext context)
            {
                Contracts.AssertValue(node);
                Contracts.AssertValue(context);

                return new ErrorNode(context.GetIRContext(node), node.ToString());
            }

            // List nodes are pointless (just a container for Args for Call Nodes?)
            // We could genuinely clean them up entirely
            public override IntermediateNode Visit(ListNode node, IRTranslatorContext context)
            {
                throw new NotImplementedException();
            }

            // These do not apply to PowerFx Scenarios and should be cleaned up.
            public override IntermediateNode Visit(ParentNode node, IRTranslatorContext context)
            {
                Contracts.Assert(false, "Parent Keyword not supported in PowerFx");
                throw new NotSupportedException();
            }

            public override IntermediateNode Visit(SelfNode node, IRTranslatorContext context)
            {
                Contracts.Assert(false, "Parent Keyword not supported in PowerFx");
                throw new NotSupportedException();
            }

            private int _scopeId = 1;

            private ScopeSymbol GetNewScope()
            {
                return new ScopeSymbol(_scopeId++);
            }

            private static TextLiteralNode GetDateTimeToTextLiteralNode(IRTranslatorContext context, CoercionKind kind)
            {
                switch (kind)
                {
                    case CoercionKind.DateToText:
                        return new TextLiteralNode(IRContext.NotInSource(FormulaType.String), "\'shortdate\'");
                    case CoercionKind.TimeToText:
                        return new TextLiteralNode(IRContext.NotInSource(FormulaType.String), "\'shorttime\'");
                    case CoercionKind.DateTimeToText:
                        return new TextLiteralNode(IRContext.NotInSource(FormulaType.String), "\'shortdatetime\'");
                    default:
                        throw new NotSupportedException("Invalid DateTimeToText coercion kind");
                }
            }

            private AggregateCoercionNode GetAggregateCoercionNode(UnaryOpKind unaryOpKind, IntermediateNode child, IRTranslatorContext context, DType fromType, DType toType)
            {
                var fieldCoercions = new Dictionary<DName, IntermediateNode>();
                var scope = GetNewScope();
                foreach (var fromField in fromType.GetNames(DPath.Root))
                {
                    if (!toType.TryGetType(fromField.Name, out var toFieldType) || toFieldType.Accepts(fromField.Type))
                    {
                        continue;
                    }
                    else
                    {
                        var coercionKind = CoercionMatrix.GetCoercionKind(fromField.Type, toFieldType);
                        if (coercionKind == CoercionKind.None)
                        {
                            continue;
                        }

                        fieldCoercions.Add(
                            fromField.Name,
                            InjectCoercion(
                                new ScopeAccessNode(IRContext.NotInSource(FormulaType.Build(fromField.Type)), new ScopeAccessSymbol(scope, scope.AddOrGetIndexForField(fromField.Name))),
                                context,
                                fromField.Type,
                                toFieldType));
                    }
                }

                return new AggregateCoercionNode(IRContext.NotInSource(FormulaType.Build(toType)), unaryOpKind, scope, child, fieldCoercions);
            }

            private IntermediateNode MaybeInjectCoercion(TexlNode nodeIn, IntermediateNode child, IRTranslatorContext context)
            {
                if (!context.Binding.CanCoerce(nodeIn))
                {
                    return child;
                }

                var fromType = context.Binding.GetType(nodeIn);
                context.Binding.TryGetCoercedType(nodeIn, out var toType).Verify();
                Contracts.Assert(!fromType.IsError);
                Contracts.Assert(!toType.IsError);

                return InjectCoercion(child, context, fromType, toType);
            }

            private IntermediateNode InjectCoercion(IntermediateNode child, IRTranslatorContext context, DType fromType, DType toType)
            {
                var coercionKind = CoercionMatrix.GetCoercionKind(fromType, toType);
                UnaryOpKind unaryOpKind;
                switch (coercionKind)
                {
                    case CoercionKind.TextToNumber:
                        return new CallNode(IRContext.NotInSource(FormulaType.Build(toType)), BuiltinFunctionsCore.Value, child);

                    case CoercionKind.DateToText:
                    case CoercionKind.TimeToText:
                    case CoercionKind.DateTimeToText:
                        return new CallNode(IRContext.NotInSource(FormulaType.Build(toType)), BuiltinFunctionsCore.Text, child, GetDateTimeToTextLiteralNode(context, coercionKind));

                    case CoercionKind.TextToDateTime:
                        return new CallNode(IRContext.NotInSource(FormulaType.Build(toType)), BuiltinFunctionsCore.DateTimeValue, child);
                    case CoercionKind.TextToDate:
                        return new CallNode(IRContext.NotInSource(FormulaType.Build(toType)), BuiltinFunctionsCore.DateValue, child);
                    case CoercionKind.TextToTime:
                        return new CallNode(IRContext.NotInSource(FormulaType.Build(toType)), BuiltinFunctionsCore.TimeValue, child);

                    case CoercionKind.RecordToRecord:
                        return GetAggregateCoercionNode(UnaryOpKind.RecordToRecord, child, context, fromType, toType);
                    case CoercionKind.TableToTable:
                        return GetAggregateCoercionNode(UnaryOpKind.TableToTable, child, context, fromType, toType);

                    // After this it's just duplicating the coercion kind
                    case CoercionKind.BooleanToNumber:
                        unaryOpKind = UnaryOpKind.BooleanToNumber;
                        break;
                    case CoercionKind.BooleanOptionSetToNumber:
                        unaryOpKind = UnaryOpKind.BooleanOptionSetToNumber;
                        break;
                    case CoercionKind.DateToNumber:
                        unaryOpKind = UnaryOpKind.DateToNumber;
                        break;
                    case CoercionKind.TimeToNumber:
                        unaryOpKind = UnaryOpKind.TimeToNumber;
                        break;
                    case CoercionKind.DateTimeToNumber:
                        unaryOpKind = UnaryOpKind.DateTimeToNumber;
                        break;
                    case CoercionKind.BlobToHyperlink:
                        unaryOpKind = UnaryOpKind.BlobToHyperlink;
                        break;
                    case CoercionKind.ImageToHyperlink:
                        unaryOpKind = UnaryOpKind.ImageToHyperlink;
                        break;
                    case CoercionKind.MediaToHyperlink:
                        unaryOpKind = UnaryOpKind.MediaToHyperlink;
                        break;
                    case CoercionKind.TextToHyperlink:
                        unaryOpKind = UnaryOpKind.TextToHyperlink;
                        break;
                    case CoercionKind.SingleColumnRecordToLargeImage:
                        unaryOpKind = UnaryOpKind.SingleColumnRecordToLargeImage;
                        break;
                    case CoercionKind.ImageToLargeImage:
                        unaryOpKind = UnaryOpKind.ImageToLargeImage;
                        break;
                    case CoercionKind.LargeImageToImage:
                        unaryOpKind = UnaryOpKind.LargeImageToImage;
                        break;
                    case CoercionKind.TextToImage:
                        unaryOpKind = UnaryOpKind.TextToImage;
                        break;
                    case CoercionKind.TextToMedia:
                        unaryOpKind = UnaryOpKind.TextToMedia;
                        break;
                    case CoercionKind.TextToBlob:
                        unaryOpKind = UnaryOpKind.TextToBlob;
                        break;
                    case CoercionKind.NumberToText:
                        unaryOpKind = UnaryOpKind.NumberToText;
                        break;
                    case CoercionKind.BooleanToText:
                        unaryOpKind = UnaryOpKind.BooleanToText;
                        break;
                    case CoercionKind.OptionSetToText:
                        unaryOpKind = UnaryOpKind.OptionSetToText;
                        break;
                    case CoercionKind.ViewToText:
                        unaryOpKind = UnaryOpKind.ViewToText;
                        break;
                    case CoercionKind.NumberToBoolean:
                        unaryOpKind = UnaryOpKind.NumberToBoolean;
                        break;
                    case CoercionKind.TextToBoolean:
                        unaryOpKind = UnaryOpKind.TextToBoolean;
                        break;
                    case CoercionKind.BooleanOptionSetToBoolean:
                        unaryOpKind = UnaryOpKind.BooleanOptionSetToBoolean;
                        break;
                    case CoercionKind.RecordToTable:
                        unaryOpKind = UnaryOpKind.RecordToTable;
                        break;
                    case CoercionKind.NumberToDateTime:
                        unaryOpKind = UnaryOpKind.NumberToDateTime;
                        break;
                    case CoercionKind.NumberToDate:
                        unaryOpKind = UnaryOpKind.NumberToDate;
                        break;
                    case CoercionKind.NumberToTime:
                        unaryOpKind = UnaryOpKind.NumberToTime;
                        break;
                    case CoercionKind.DateTimeToDate:
                        unaryOpKind = UnaryOpKind.DateTimeToDate;
                        break;
                    case CoercionKind.DateToDateTime:
                        unaryOpKind = UnaryOpKind.DateToDateTime;
                        break;
                    case CoercionKind.DateToTime:
                        unaryOpKind = UnaryOpKind.DateToTime;
                        break;
                    case CoercionKind.TimeToDate:
                        unaryOpKind = UnaryOpKind.TimeToDate;
                        break;
                    case CoercionKind.TimeToDateTime:
                        unaryOpKind = UnaryOpKind.TimeToDateTime;
                        break;
                    case CoercionKind.BooleanToOptionSet:
                        unaryOpKind = UnaryOpKind.BooleanToOptionSet;
                        break;
                    case CoercionKind.AggregateToDataEntity:
                        unaryOpKind = UnaryOpKind.AggregateToDataEntity;
                        break;
                    case CoercionKind.None:
                        // No coercion needed, return the child node
                        return child;
                    default:
                        throw new InvalidOperationException("Unexpected Coercion Kind: " + coercionKind);
                }

                return new UnaryOpNode(IRContext.NotInSource(FormulaType.Build(toType)), unaryOpKind, child);
            }
        }

        internal class IRTranslatorContext
        {
            public readonly TexlBinding Binding;
            public ScopeSymbol[] Scopes;

            public IRTranslatorContext(TexlBinding binding, ScopeSymbol baseScope)
            {
                Contracts.AssertValue(binding);
                Contracts.AssertValue(baseScope);

                Binding = binding;
                Scopes = new ScopeSymbol[] { baseScope };
            }

            private IRTranslatorContext(IRTranslatorContext context, ScopeSymbol newScope)
            {
                Binding = context.Binding;
                Scopes = context.Scopes.Concat(new List<ScopeSymbol>() { newScope }).ToArray();
            }

            public IRTranslatorContext With(ScopeSymbol scope)
            {
                return new IRTranslatorContext(this, scope);
            }

            public IRContext GetIRContext(TexlNode node)
            {
                return new IRContext(node.GetTextSpan(), FormulaType.Build(Binding.GetType(node)));
            }
        }
    }
}
