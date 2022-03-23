// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Syntax.Visitors;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using BinaryOpNode = Microsoft.PowerFx.Core.IR.Nodes.BinaryOpNode;
using CallNode = Microsoft.PowerFx.Core.IR.Nodes.CallNode;
using ErrorNode = Microsoft.PowerFx.Core.IR.Nodes.ErrorNode;
using RecordNode = Microsoft.PowerFx.Core.IR.Nodes.RecordNode;
using TableNode = Microsoft.PowerFx.Core.IR.Nodes.TableNode;
using TexlBinaryOpNode = Microsoft.PowerFx.Core.Syntax.Nodes.BinaryOpNode;
using TexlCallNode = Microsoft.PowerFx.Core.Syntax.Nodes.CallNode;
using TexlErrorNode = Microsoft.PowerFx.Core.Syntax.Nodes.ErrorNode;
using TexlRecordNode = Microsoft.PowerFx.Core.Syntax.Nodes.RecordNode;
using TexlTableNode = Microsoft.PowerFx.Core.Syntax.Nodes.TableNode;
using TexlUnaryOpNode = Microsoft.PowerFx.Core.Syntax.Nodes.UnaryOpNode;
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
                return MaybeInjectCoercion(node, new TableNode(context.GetIRContext(node), children), context);
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

                var leftType = context.Binding.GetType(node.Left);
                var rightType = context.Binding.GetType(node.Right);

                IntermediateNode binaryOpResult;

                switch (node.Op)
                {
                    case BinaryOp.In:
                    case BinaryOp.Exactin:
                        if (!rightType.IsAggregate)
                        {
                            if ((DType.String.Accepts(rightType) && (DType.String.Accepts(leftType) || leftType.CoercesTo(DType.String))) ||
                                (rightType.CoercesTo(DType.String) && DType.String.Accepts(leftType)))
                            {
                                binaryOpResult = new BinaryOpNode(context.GetIRContext(node), node.Op == BinaryOp.In ? BinaryOpKind.InText : BinaryOpKind.ExactInText, left, right);
                                break;
                            }

                            // anything else in scalar: not supported.
                            Contracts.Assert(context.Binding.ErrorContainer.HasErrors(node.Left) || context.Binding.ErrorContainer.HasErrors(node.Right));
                            return new ErrorNode(context.GetIRContext(node), node.ToString());
                        }

                        if (!leftType.IsAggregate)
                        {
                            if (rightType.IsTable)
                            {
                                // scalar in table: in_ST(left, right)
                                // scalar exactin table: exactin_ST(left, right)
                                binaryOpResult = new BinaryOpNode(context.GetIRContext(node), node.Op == BinaryOp.In ? BinaryOpKind.InScalarTable : BinaryOpKind.ExactInScalarTable, left, right);
                                break;
                            }

                            // scalar in record: not supported
                            // scalar exactin record: not supported
                            Contracts.Assert(context.Binding.ErrorContainer.HasErrors(node.Left) || context.Binding.ErrorContainer.HasErrors(node.Right));
                            return new ErrorNode(context.GetIRContext(node), node.ToString());
                        }

                        if (leftType.IsRecord)
                        {
                            if (rightType.IsTable)
                            {
                                // record in table: in_RT(left, right)
                                // record exactin table: in_RT(left, right)
                                // This is done regardless of "exactness".
                                binaryOpResult = new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.InRecordTable, left, right);
                                break;
                            }

                            // record in record: not supported
                            // record exactin record: not supported
                            Contracts.Assert(context.Binding.ErrorContainer.HasErrors(node.Left) || context.Binding.ErrorContainer.HasErrors(node.Right));
                            return new ErrorNode(context.GetIRContext(node), node.ToString());
                        }

                        // table in anything: not supported
                        // table exactin anything: not supported
                        Contracts.Assert(context.Binding.ErrorContainer.HasErrors(node.Left));
                        return new ErrorNode(context.GetIRContext(node), node.ToString());
                    case BinaryOp.Power:
                        binaryOpResult = new CallNode(context.GetIRContext(node), BuiltinFunctionsCore.Power, left, right);
                        break;
                    case BinaryOp.Concat:
                        binaryOpResult = new CallNode(context.GetIRContext(node), BuiltinFunctionsCore.Concatenate, left, right);
                        break;
                    case BinaryOp.Or:
                    case BinaryOp.And:
                        binaryOpResult = new CallNode(context.GetIRContext(node), node.Op == BinaryOp.And ? BuiltinFunctionsCore.And : BuiltinFunctionsCore.Or, left, new LazyEvalNode(context.GetIRContext(node), right));
                        break;
                    case BinaryOp.Add:
                        binaryOpResult = GetAddBinaryOp(context, node, left, right, leftType, rightType);
                        break;
                    case BinaryOp.Mul:
                        binaryOpResult = new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.MulNumbers, left, right);
                        break;
                    case BinaryOp.Div:
                        binaryOpResult = new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.DivNumbers, left, right);
                        break;
                    case BinaryOp.Equal:
                    case BinaryOp.NotEqual:
                    case BinaryOp.Less:
                    case BinaryOp.LessEqual:
                    case BinaryOp.Greater:
                    case BinaryOp.GreaterEqual:
                        binaryOpResult = GetBooleanBinaryOp(context, node, left, right, leftType, rightType);
                        break;
                    case BinaryOp.Error:
                        return new ErrorNode(context.GetIRContext(node), node.ToString());
                    default:
                        throw new NotSupportedException();
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

            // Replaceable nodes are dead code and should be removed entirely
            public override IntermediateNode Visit(ReplaceableNode node, IRTranslatorContext context)
            {
                Contracts.Assert(false);
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
                    case CoercionKind.GuidToText:
                        unaryOpKind = UnaryOpKind.GuidToText;
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

            private static IntermediateNode GetAddBinaryOp(IRTranslatorContext context, TexlBinaryOpNode node, IntermediateNode left, IntermediateNode right, DType leftType, DType rightType)
            {
                Contracts.AssertValue(node);
                Contracts.Assert(node.Op == BinaryOp.Add);

                switch (leftType.Kind)
                {
                    case DKind.Date:
                        if (rightType == DType.DateTime || rightType == DType.Date)
                        {
                            // Date + '-DateTime' => in days
                            // Date + '-Date' => in days

                            // Ensure that this is really '-Date' - Binding should always catch this, but let's make sure...
                            Contracts.Assert(node.Right.AsUnaryOpLit().VerifyValue().Op == UnaryOp.Minus);
                            return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.DateDifference, left, right);
                        }
                        else if (rightType == DType.Time)
                        {
                            return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.AddDateAndTime, left, right);
                        }
                        else
                        {
                            return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.AddDateAndDay, left, right);
                        }

                    case DKind.Time:
                        if (rightType == DType.Date)
                        {
                            // Time + Date => DateTime
                            return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.AddDateAndTime, right, left);
                        }
                        else if (rightType == DType.Time)
                        {
                            // Time + '-Time' => in ms
                            // Ensure that this is really '-Time' - Binding should always catch this, but let's make sure...
                            Contracts.Assert(node.Right.AsUnaryOpLit().VerifyValue().Op == UnaryOp.Minus);
                            return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.AddNumbers, left, right);
                        }
                        else
                        {
                            // Time + Number
                            return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.AddTimeAndMilliseconds, left, right);
                        }

                    case DKind.DateTime:
                        if (rightType == DType.DateTime || rightType == DType.Date)
                        {
                            // DateTime + '-DateTime' => in days
                            // DateTime + '-Date' => in days

                            // Ensure that this is really '-Date' - Binding should always catch this, but let's make sure...
                            Contracts.Assert(node.Right.AsUnaryOpLit().VerifyValue().Op == UnaryOp.Minus);
                            return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.DateDifference, left, right);
                        }
                        else
                        {
                            return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.AddDateTimeAndDay, left, right);
                        }

                    default:
                        switch (rightType.Kind)
                        {
                            case DKind.Date:
                                // Number + Date
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.AddDateAndDay, right, left);
                            case DKind.Time:
                                // Number + Date
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.AddTimeAndMilliseconds, right, left);
                            case DKind.DateTime:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.AddDateTimeAndDay, right, left);
                            default:
                                // Number + Number
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.AddNumbers, left, right);
                        }
                }
            }

            private static IntermediateNode GetBooleanBinaryOp(IRTranslatorContext context, TexlBinaryOpNode node, IntermediateNode left, IntermediateNode right, DType leftType, DType rightType)
            {
                var kindToUse = leftType.Accepts(rightType) ? leftType.Kind : rightType.Kind;

                if (!leftType.Accepts(rightType) && !rightType.Accepts(leftType))
                {
                    // There is coercion involved, pick the coerced type.
                    if (context.Binding.TryGetCoercedType(node.Left, out var leftCoerced))
                    {
                        kindToUse = leftCoerced.Kind;
                    }
                    else if (context.Binding.TryGetCoercedType(node.Right, out var rightCoerced))
                    {
                        kindToUse = rightCoerced.Kind;
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }

                switch (kindToUse)
                {
                    case DKind.Number:
                        switch (node.Op)
                        {
                            case BinaryOp.NotEqual:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.NeqNumbers, left, right);
                            case BinaryOp.Equal:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.EqNumbers, left, right);
                            case BinaryOp.Less:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.LtNumbers, left, right);
                            case BinaryOp.LessEqual:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.LeqNumbers, left, right);
                            case BinaryOp.Greater:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.GtNumbers, left, right);
                            case BinaryOp.GreaterEqual:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.GeqNumbers, left, right);
                            default:
                                throw new NotSupportedException();
                        }

                    case DKind.Date:
                        switch (node.Op)
                        {
                            case BinaryOp.NotEqual:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.NeqDate, left, right);
                            case BinaryOp.Equal:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.EqDate, left, right);
                            case BinaryOp.Less:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.LtDate, left, right);
                            case BinaryOp.LessEqual:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.LeqDate, left, right);
                            case BinaryOp.Greater:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.GtDate, left, right);
                            case BinaryOp.GreaterEqual:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.GeqDate, left, right);
                            default:
                                throw new NotSupportedException();
                        }

                    case DKind.DateTime:
                        switch (node.Op)
                        {
                            case BinaryOp.NotEqual:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.NeqDateTime, left, right);
                            case BinaryOp.Equal:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.EqDateTime, left, right);
                            case BinaryOp.Less:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.LtDateTime, left, right);
                            case BinaryOp.LessEqual:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.LeqDateTime, left, right);
                            case BinaryOp.Greater:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.GtDateTime, left, right);
                            case BinaryOp.GreaterEqual:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.GeqDateTime, left, right);
                            default:
                                throw new NotSupportedException();
                        }

                    case DKind.Time:
                        switch (node.Op)
                        {
                            case BinaryOp.NotEqual:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.NeqTime, left, right);
                            case BinaryOp.Equal:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.EqTime, left, right);
                            case BinaryOp.Less:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.LtTime, left, right);
                            case BinaryOp.LessEqual:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.LeqTime, left, right);
                            case BinaryOp.Greater:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.GtTime, left, right);
                            case BinaryOp.GreaterEqual:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.GeqTime, left, right);
                            default:
                                throw new NotSupportedException();
                        }

                    case DKind.Boolean:
                        switch (node.Op)
                        {
                            case BinaryOp.NotEqual:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.NeqBoolean, left, right);
                            case BinaryOp.Equal:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.EqBoolean, left, right);
                            default:
                                throw new NotSupportedException();
                        }

                    case DKind.String:
                        switch (node.Op)
                        {
                            case BinaryOp.NotEqual:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.NeqText, left, right);
                            case BinaryOp.Equal:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.EqText, left, right);
                            default:
                                throw new NotSupportedException();
                        }

                    case DKind.Hyperlink:
                        switch (node.Op)
                        {
                            case BinaryOp.NotEqual:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.NeqHyperlink, left, right);
                            case BinaryOp.Equal:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.EqHyperlink, left, right);
                            default:
                                throw new NotSupportedException();
                        }

                    case DKind.Currency:
                        switch (node.Op)
                        {
                            case BinaryOp.NotEqual:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.NeqCurrency, left, right);
                            case BinaryOp.Equal:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.EqCurrency, left, right);
                            default:
                                throw new NotSupportedException();
                        }

                    case DKind.Image:
                        switch (node.Op)
                        {
                            case BinaryOp.NotEqual:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.NeqImage, left, right);
                            case BinaryOp.Equal:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.EqImage, left, right);
                            default:
                                throw new NotSupportedException();
                        }

                    case DKind.Color:
                        switch (node.Op)
                        {
                            case BinaryOp.NotEqual:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.NeqColor, left, right);
                            case BinaryOp.Equal:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.EqColor, left, right);
                            default:
                                throw new NotSupportedException();
                        }

                    case DKind.Media:
                        switch (node.Op)
                        {
                            case BinaryOp.NotEqual:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.NeqMedia, left, right);
                            case BinaryOp.Equal:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.EqMedia, left, right);
                            default:
                                throw new NotSupportedException();
                        }

                    case DKind.Blob:
                        switch (node.Op)
                        {
                            case BinaryOp.NotEqual:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.NeqBlob, left, right);
                            case BinaryOp.Equal:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.EqBlob, left, right);
                            default:
                                throw new NotSupportedException();
                        }

                    case DKind.Guid:
                        switch (node.Op)
                        {
                            case BinaryOp.NotEqual:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.NeqGuid, left, right);
                            case BinaryOp.Equal:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.EqGuid, left, right);
                            default:
                                throw new NotSupportedException();
                        }

                    case DKind.ObjNull:
                        switch (node.Op)
                        {
                            case BinaryOp.NotEqual:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.NeqNull, left, right);
                            case BinaryOp.Equal:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.EqNull, left, right);
                            default:
                                throw new NotSupportedException();
                        }

                    case DKind.OptionSetValue:
                        switch (node.Op)
                        {
                            case BinaryOp.NotEqual:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.NeqOptionSetValue, left, right);
                            case BinaryOp.Equal:
                                return new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.EqOptionSetValue, left, right);
                            default:
                                throw new NotSupportedException();
                        }

                    default:
                        throw new NotSupportedException("Not supported comparison op on type " + kindToUse.ToString());
                }
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
