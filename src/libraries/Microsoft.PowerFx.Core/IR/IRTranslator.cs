// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Texl.Builtins;
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
    internal class IRResult
    {
        public IntermediateNode TopNode;
        public ScopeSymbol RuleScopeSymbol;
    }

    internal class IRTranslator
    {
        private const string DeferredNotSupportedExceptionMsg = "Deferred(Unknown) is not supported in expressions to be evaluated. This is always an error, deferred is only valid when calling Check";

        /// <summary>
        /// Returns the top node of the IR tree, and a symbol that corresponds to the Rule Scope.
        /// </summary>
        public static (IntermediateNode topNode, ScopeSymbol ruleScopeSymbol) Translate(TexlBinding binding)
        {
            Contracts.AssertValue(binding);

            var ruleScopeSymbol = new ScopeSymbol(0);
            return (binding.Top.Accept(new IRTranslatorVisitor(binding.Features), new IRTranslatorContext(binding, ruleScopeSymbol)), ruleScopeSymbol);
        }

        private class IRTranslatorVisitor : TexlFunctionalVisitor<IntermediateNode, IRTranslatorContext>
        {
            private readonly Features _features;

            public IRTranslatorVisitor(Features features)
            {
                _features = features;
            }

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

            public override IntermediateNode Visit(DecLitNode node, IRTranslatorContext context)
            {
                Contracts.AssertValue(node);
                Contracts.AssertValue(context);

                // I think node.DecValue might be dead code, this could be cleaned up (copied comment from NumLitNode overload)
                var value = node.Value?.Value ?? node.DecValue;
                return MaybeInjectCoercion(node, new DecimalLiteralNode(context.GetIRContext(node), value), context);
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
                var childrenAreRecords = node.ChildNodes.Any(c =>
                {
                    var childType = context.Binding.GetType(c);
                    return childType.Kind != DKind.ObjNull && childType.IsRecord;
                });

                if (!context.Binding.CheckTypesContext.Features.TableSyntaxDoesntWrapRecords || !childrenAreRecords)
                {
                    // Let's add "Value:" here                    
                    children = children.Select(childNode =>
                        new RecordNode(
                            new IRContext(childNode.IRContext.SourceContext, RecordType.Empty().Add(TableValue.ValueName, childNode.IRContext.ResultType)),
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
                var irc = context.GetIRContext(node);

                IntermediateNode result;
                switch (node.Op)
                {
                    case UnaryOp.Not:
                        result = new CallNode(irc, BuiltinFunctionsCore.Not, child);
                        break;
                    case UnaryOp.Minus:
                        UnaryOpKind unaryOpKind = irc.ResultType._type.Kind switch
                        {
                            DKind.Decimal => UnaryOpKind.NegateDecimal,
                            DKind.Date => UnaryOpKind.NegateDate,
                            DKind.DateTime => UnaryOpKind.NegateDateTime,
                            DKind.Time => UnaryOpKind.NegateTime,
                            _ => UnaryOpKind.Negate
                        };
                        result = new UnaryOpNode(irc, unaryOpKind, child);
                        break;
                    case UnaryOp.Percent:
                        result = new UnaryOpNode(irc, irc.ResultType == FormulaType.Decimal ? UnaryOpKind.PercentDecimal : UnaryOpKind.Percent, child);
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
                        var args = new List<IntermediateNode> { left, right };

                        // Since we are directly injecting Power Function and Power function delegates arg preprocessing to IR.
                        // we need to make sure that the arguments are attached with preprocessor e.g. Blank to Zero.
                        args = AttachArgPreprocessor(args, BuiltinFunctionsCore.Power, node, 2, context);
                        binaryOpResult = new CallNode(context.GetIRContext(node), BuiltinFunctionsCore.Power, args);
                        break;
                    case BinaryOpKind.Concatenate:
                        binaryOpResult = ConcatenateArgs(left, right, context.GetIRContext(node));
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
                    case BinaryOpKind.AddNumberAndTime:
                        binaryOpResult = new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.AddTimeAndNumber, right, left);
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
                    case BinaryOpKind.TimeDifference:
                        // Validated in Matrix + Binder
                        if (right is UnaryOpNode unaryNegate)
                        {
                            if (unaryNegate.Op == UnaryOpKind.Negate ||
                                unaryNegate.Op == UnaryOpKind.NegateDecimal ||
                                unaryNegate.Op == UnaryOpKind.NegateDate ||
                                unaryNegate.Op == UnaryOpKind.NegateDateTime ||
                                unaryNegate.Op == UnaryOpKind.NegateTime)
                            {
                                binaryOpResult = new BinaryOpNode(context.GetIRContext(node), kind, left, unaryNegate.Child);
                                break;
                            }
                        }

                        throw new NotSupportedException();

                    case BinaryOpKind.AddDateAndTime:
                        if (right is not UnaryOpNode { Op: UnaryOpKind.Negate or UnaryOpKind.NegateTime } unaryNegate3)
                        {
                            binaryOpResult = new BinaryOpNode(context.GetIRContext(node), kind, left, right);
                        }
                        else
                        {
                            binaryOpResult = new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.SubtractDateAndTime, left, unaryNegate3.Child);
                        }

                        break;

                    case BinaryOpKind.SubtractNumberAndDate:
                    case BinaryOpKind.SubtractNumberAndDateTime:
                        // Validated in Matrix + Binder
                        if (right is UnaryOpNode unaryNegate4)
                        {
                            if (unaryNegate4.Op == UnaryOpKind.Negate ||
                                unaryNegate4.Op == UnaryOpKind.NegateDecimal ||
                                unaryNegate4.Op == UnaryOpKind.NegateDate ||
                                unaryNegate4.Op == UnaryOpKind.NegateDateTime ||
                                unaryNegate4.Op == UnaryOpKind.NegateTime)
                            {
                                binaryOpResult = new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.SubtractNumberAndDate, left, unaryNegate4.Child);
                                break;
                            }
                        }

                        throw new NotSupportedException();

                    case BinaryOpKind.SubtractNumberAndTime:
                        // Validated in Matrix + Binder
                        if (right is UnaryOpNode unaryNegate5)
                        {
                            if (unaryNegate5.Op == UnaryOpKind.Negate ||
                                unaryNegate5.Op == UnaryOpKind.NegateDecimal ||
                                unaryNegate5.Op == UnaryOpKind.NegateDate ||
                                unaryNegate5.Op == UnaryOpKind.NegateDateTime ||
                                unaryNegate5.Op == UnaryOpKind.NegateTime)
                            {
                                binaryOpResult = new BinaryOpNode(context.GetIRContext(node), BinaryOpKind.SubtractNumberAndTime, left, unaryNegate5.Child);
                                break;
                            }
                        }

                        throw new NotSupportedException();

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

                    var supportColumnNamesAsIdentifiers = _features.SupportColumnNamesAsIdentifiers;
                    if (supportColumnNamesAsIdentifiers && func.IsIdentifierParam(i))
                    {
                        var identifierNode = arg.AsFirstName();
                        Contracts.Assert(identifierNode != null);

                        // Transform the identifier node as a string literal
                        var nodeName = context.Binding.TryGetReplacedIdentName(identifierNode.Ident, out var newIdent) ? new DName(newIdent) : identifierNode.Ident.Name;
                        args.Add(new TextLiteralNode(context.GetIRContext(arg, DType.String), nodeName.Value));
                    }
                    else if (func.IsLazyEvalParam(i))
                    {
                        var child = arg.Accept(this, scope != null && func.ScopeInfo.AppliesToArgument(i) ? context.With(scope) : context);
                        args.Add(new LazyEvalNode(context.GetIRContext(arg), child));
                    }
                    else
                    {
                        args.Add(arg.Accept(this, context));
                    }
                }

                // This can add pre-processing to arguments, such as BlankToZero, Truncate etc...
                // based on the function.
                args = AttachArgPreprocessor(args, func, node, node.Args.Count, context);

                // this can rewrite the entire call node to any intermediate node.
                // e.g. For Boolean(true), Instead of IR as Call(Boolean, true) it can be rewritten directly to emit true.
                var irNode = func.CreateIRCallNode(node, context, args, scope);

                return MaybeInjectCoercion(node, irNode, context);
            }

            private List<IntermediateNode> AttachArgPreprocessor(List<IntermediateNode> args, TexlFunction func, TexlNode node, int argCount, IRTranslatorContext context)
            {
                var len = args.Count;
                List<IntermediateNode> convertedArgs = new List<IntermediateNode>(len);

                for (var i = 0; i < len; i++)
                {
                    IntermediateNode convertedNode;
                    var argPreprocessor = func.GetArgPreprocessor(i, argCount);

                    switch (argPreprocessor)
                    {
                        case ArgPreprocessor.ReplaceBlankWithFloatZero:
                            convertedNode = ReplaceBlankWithFloatZero(args[i]);
                            break;
                        case ArgPreprocessor.ReplaceBlankWithDecimalZero:
                            convertedNode = ReplaceBlankWithDecimalZero(args[i]);
                            break;
                        case ArgPreprocessor.ReplaceBlankWithFloatZeroAndTruncate:
                            convertedNode = ReplaceBlankWithFloatZeroAndTruncatePreProcessor(args[i]);
                            break;
                        case ArgPreprocessor.ReplaceBlankWithDecimalZeroAndTruncate:
                            convertedNode = ReplaceBlankWithDecimalZeroAndTruncatePreProcessor(args[i]);
                            break;
                        case ArgPreprocessor.ReplaceBlankWithEmptyString:
                            convertedNode = BlankToEmptyString(args[i]);
                            break;
                        case ArgPreprocessor.ReplaceBlankWithCallZero_Scalar:
                            var callIRContext_Scalar = context.GetIRContext(node);
                            convertedNode = ReplaceBlankWithCallTypedZero_Scalar(args[i], callIRContext_Scalar.ResultType);
                            break;
                        case ArgPreprocessor.ReplaceBlankWithCallZero_SingleColumnTable:
                            var callIRContext_SCT = context.GetIRContext(node);
                            convertedNode = ReplaceBlankWithCallTypedZero_Scalar(args[i], ((TableType)callIRContext_SCT.ResultType).SingleColumnFieldType);
                            break;
                        case ArgPreprocessor.UntypedStringToUntypedNumber:
                            if (context.Binding.BindingConfig.NumberIsFloat)
                            {
                                convertedNode = new UnaryOpNode(IRContext.NotInSource(FormulaType.UntypedObject), UnaryOpKind.UntypedStringToUntypedFloat, args[i]);
                            }
                            else
                            {
                                convertedNode = new UnaryOpNode(IRContext.NotInSource(FormulaType.UntypedObject), UnaryOpKind.UntypedStringToUntypedDecimal, args[i]);
                            }

                            break;
                        case ArgPreprocessor.MutationCopy:
                            convertedNode = MutationCopy(args[i]);
                            break;
                        default:
                            convertedNode = args[i];
                            break;
                    }

                    convertedArgs.Add(convertedNode);
                }

                return convertedArgs;
            }

            /// <summary>
            /// Adds IRContext.MutationCopy flag for arguments that are about to be mutated.
            /// This flag is used to make shallow copies of data structures as the 
            /// mutation function's argument is being evaluated.
            /// Note that setting this flag is recursive for the dot operator.
            /// </summary>
            private static IntermediateNode MutationCopy(IntermediateNode arg)
            {
                if (arg is CallNode)
                {
                    // assumes that the list of functions that can be mutated through is handled elsewhere
                    return arg;
                }
                else if (arg is RecordFieldAccessNode fa)
                {
                    return new RecordFieldAccessNode(new IRContext(fa.IRContext.SourceContext, fa.IRContext.ResultType, isMutation: true), MutationCopy(fa.From), fa.Field);
                }
                else if (arg is ResolvedObjectNode ro)
                {
                    return new ResolvedObjectNode(new IRContext(ro.IRContext.SourceContext, ro.IRContext.ResultType, isMutation: true), ro.Value);
                }

                throw new NotImplementedException("Mutation thrgouh an accessor node that does not support mutation");
            }

            /// <summary>
            /// Wraps node arg => Coalesce(arg , 0) when arg is not Number Literal.
            /// </summary>
            private static IntermediateNode ReplaceBlankWithFloatZero(IntermediateNode arg)
            {
                if (arg is NumberLiteralNode)
                {
                    return arg;
                }

                // need a new context since when arg is Blank IRContext.ResultType is not a Number but a Blank.
                var convertedIRContext = new IRContext(arg.IRContext.SourceContext, FormulaType.Number);
                var zeroLitNode = new NumberLiteralNode(convertedIRContext, 0d);
                var convertedNode = new CallNode(convertedIRContext, BuiltinFunctionsCore.Coalesce, arg, zeroLitNode);
                return convertedNode;
            }

            /// <summary>
            /// Wraps node arg => Coalesce(arg , 0) when arg is not Number Literal.
            /// </summary>
            private static IntermediateNode ReplaceBlankWithDecimalZero(IntermediateNode arg)
            {
                if (arg is DecimalLiteralNode)
                {
                    return arg;
                }

                // need a new context since when arg is Blank IRContext.ResultType is not a Number but a Blank.
                var convertedIRContext = new IRContext(arg.IRContext.SourceContext, FormulaType.Decimal);
                var zeroLitNode = new DecimalLiteralNode(convertedIRContext, 0m);
                var convertedNode = new CallNode(convertedIRContext, BuiltinFunctionsCore.Coalesce, arg, zeroLitNode);
                return convertedNode;
            }

            /// <summary>
            /// Wraps node arg => Coalesce(arg , 0) when arg is not Number Literal.
            /// </summary>
            private static IntermediateNode ReplaceBlankWithCallTypedZero_Scalar(IntermediateNode arg, FormulaType returnType)
            {
                IntermediateNode zeroLitNode;
                IRContext convertedIRContext;

                if (arg is NumberLiteralNode || arg is DecimalLiteralNode)
                {
                    return arg;
                }

                // need a new context since when arg is Blank IRContext.ResultType is not a Number but a Blank.
                if (returnType == FormulaType.Number)
                {
                    convertedIRContext = new IRContext(arg.IRContext.SourceContext, FormulaType.Number);
                    zeroLitNode = new NumberLiteralNode(convertedIRContext, 0d);
                }
                else if (returnType == FormulaType.Decimal)
                {
                    convertedIRContext = new IRContext(arg.IRContext.SourceContext, FormulaType.Decimal);
                    zeroLitNode = new DecimalLiteralNode(convertedIRContext, 0m);
                }
                else
                {
                    throw new NotImplementedException("Unexpected type");
                }

                var convertedNode = new CallNode(convertedIRContext, BuiltinFunctionsCore.Coalesce, arg, zeroLitNode);
                return convertedNode;
            }

            /// <summary>
            /// Wraps node arg => Trunc(Coalesce(arg , Float(0))).
            /// </summary>
            private static IntermediateNode ReplaceBlankWithFloatZeroAndTruncatePreProcessor(IntermediateNode arg)
            {
                var blankToZeroNode = ReplaceBlankWithFloatZero(arg);
                var truncateNode = new CallNode(blankToZeroNode.IRContext, BuiltinFunctionsCore.Trunc, blankToZeroNode);
                return truncateNode;
            }

            /// <summary>
            /// Wraps node arg => Trunc(Coalesce(arg , Decimal(0))).
            /// </summary>
            private static IntermediateNode ReplaceBlankWithDecimalZeroAndTruncatePreProcessor(IntermediateNode arg)
            {
                var blankToZeroNode = ReplaceBlankWithDecimalZero(arg);
                var truncateNode = new CallNode(blankToZeroNode.IRContext, BuiltinFunctionsCore.Trunc, blankToZeroNode);
                return truncateNode;
            }

            /// <summary>
            /// Wraps node arg => UnaryOp(BlankToEmptyString, arg).
            /// </summary>
            private static IntermediateNode BlankToEmptyString(IntermediateNode arg)
            {
                // need a new context since when arg is Blank IRContext.ResultType is not a String but a Blank.
                var convertedIRContext = new IRContext(arg.IRContext.SourceContext, FormulaType.String);

                // We are not using coalesce, because coalesce doesn't considers empty string as blank.
                return new UnaryOpNode(convertedIRContext, UnaryOpKind.BlankToEmptyString, arg);
            }

            /// <summary>
            /// This is not a generic arg concatenate function, but a special case for the Concatenate function,
            /// used for Binary Concatenate operator.
            /// </summary>
            private static IntermediateNode ConcatenateArgs(IntermediateNode arg1, IntermediateNode arg2, IRContext irContext)
            {
                var concatenateArgs = new List<IntermediateNode>();
                foreach (var arg in new[] { arg1, arg2 })
                {
                    // if arg is call node to Concatenate unpack it, and pass it as arg to outer Concatenate
                    if (arg is CallNode maybeConcatenate && maybeConcatenate.Function is ConcatenateFunction concatenateFunction)
                    {
                        foreach (var argC in maybeConcatenate.Args)
                        {
                            concatenateArgs.Add(argC);
                        }
                    }
                    else
                    {
                        concatenateArgs.Add(arg);
                    }
                }

                var concatenatedNode = new CallNode(irContext, BuiltinFunctionsCore.Concatenate, concatenateArgs);
                return concatenatedNode;
            }

            public override IntermediateNode Visit(FirstNameNode node, IRTranslatorContext context)
            {
                Contracts.AssertValue(node);
                Contracts.AssertValue(context);

                var nodeType = context.Binding.GetType(node);

                if (nodeType.IsDeferred)
                {
                    throw new NotSupportedException(DeferredNotSupportedExceptionMsg);
                }

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

                    case BindKind.Enum:
                        {
                            // If StronglyTypedEnums is disabled, this should have been handled by the DottedName visitor.
                            Contracts.Assert(context.Binding.Features.StronglyTypedBuiltinEnums);

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
                    var usePFxV1CompatRules = context.Binding.Features.PowerFxV1CompatibilityRules;

                    if (DType.Color.Accepts(resultType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePFxV1CompatRules))
                    {
                        result = new ColorLiteralNode(context.GetIRContext(node), ConvertToColor((double)value));
                    } 
                    else if (DType.Number.Accepts(resultType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePFxV1CompatRules))
                    {
                        result = new NumberLiteralNode(context.GetIRContext(node), (double)value);
                    }
                    else if (DType.String.Accepts(resultType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePFxV1CompatRules))
                    {
                        result = new TextLiteralNode(context.GetIRContext(node), (string)value);
                    }
                    else if (DType.Boolean.Accepts(resultType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePFxV1CompatRules))
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
                    typeLhs.TryGetType(nameRhs, out var typeRhs);
                    if (node.Left is FirstNameNode namespaceNode && context.Binding.GetInfo(namespaceNode)?.Kind == BindKind.QualifiedValue)
                    {
                        Contracts.Assert(false, "QualifiedValues not yet supported by PowerFx");
                        throw new NotSupportedException();
                    }

                    if (typeRhs.IsDeferred)
                    {
                        throw new NotSupportedException(DeferredNotSupportedExceptionMsg);
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

            private IntermediateNode GetAggregateCoercionNode(UnaryOpKind unaryOpKind, IntermediateNode child, IRTranslatorContext context, DType fromType, DType toType)
            {
                var fieldCoercions = new Dictionary<DName, IntermediateNode>();
                var scope = GetNewScope();
                foreach (var fromField in fromType.GetNames(DPath.Root))
                {
                    if (!toType.TryGetType(fromField.Name, out var toFieldType) || toFieldType.Accepts(fromField.Type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: _features.PowerFxV1CompatibilityRules))
                    {
                        continue;
                    }
                    else
                    {
                        var coercionKind = CoercionMatrix.GetCoercionKind(fromField.Type, toFieldType, _features.PowerFxV1CompatibilityRules);
                        if (coercionKind == CoercionKind.None)
                        {
                            continue;
                        }

                        var innerCoersion = InjectCoercion(new ScopeAccessNode(IRContext.NotInSource(FormulaType.Build(fromField.Type)), new ScopeAccessSymbol(scope, scope.AddOrGetIndexForField(fromField.Name))), context, fromField.Type, toFieldType);
                        fieldCoercions.Add(fromField.Name, innerCoersion);
                    }
                }

                if (!fieldCoercions.Any())
                {
                    return child;
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
                var coercionKind = CoercionMatrix.GetCoercionKind(fromType, toType, context.Binding.Features.PowerFxV1CompatibilityRules);
                UnaryOpKind unaryOpKind;
                switch (coercionKind)
                {
                    case CoercionKind.TextToNumber:
                        return new CallNode(
                            IRContext.NotInSource(FormulaType.Build(toType)),
                            context.Binding.BindingConfig.NumberIsFloat ? BuiltinFunctionsCore.Value : BuiltinFunctionsCore.Float,
                            child);
                    case CoercionKind.TextToDecimal:
                        return new CallNode(IRContext.NotInSource(FormulaType.Build(toType)), BuiltinFunctionsCore.Decimal, child);

                    case CoercionKind.DecimalToNumber:
                        return new CallNode(
                            IRContext.NotInSource(FormulaType.Build(toType)),
                            context.Binding.BindingConfig.NumberIsFloat ? BuiltinFunctionsCore.Value : BuiltinFunctionsCore.Float,
                            child);
                    case CoercionKind.NumberToDecimal:
                        return new CallNode(IRContext.NotInSource(FormulaType.Build(toType)), BuiltinFunctionsCore.Decimal, child);

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
                    case CoercionKind.BooleanToDecimal:
                        unaryOpKind = UnaryOpKind.BooleanToDecimal;
                        break;
                    case CoercionKind.OptionSetToNumber:
                        unaryOpKind = UnaryOpKind.OptionSetToNumber;
                        break;
                    case CoercionKind.OptionSetToColor:
                        unaryOpKind = UnaryOpKind.OptionSetToColor;
                        break;
                    case CoercionKind.OptionSetToDecimal:
                        unaryOpKind = UnaryOpKind.OptionSetToDecimal;
                        break;
                    case CoercionKind.DateToNumber:
                        unaryOpKind = UnaryOpKind.DateToNumber;
                        break;
                    case CoercionKind.DateToDecimal:
                        unaryOpKind = UnaryOpKind.DateToDecimal;
                        break;
                    case CoercionKind.TimeToNumber:
                        unaryOpKind = UnaryOpKind.TimeToNumber;
                        break;
                    case CoercionKind.TimeToDecimal:
                        unaryOpKind = UnaryOpKind.TimeToDecimal;
                        break;
                    case CoercionKind.DateTimeToNumber:
                        unaryOpKind = UnaryOpKind.DateTimeToNumber;
                        break;
                    case CoercionKind.DateTimeToDecimal:
                        unaryOpKind = UnaryOpKind.DateTimeToDecimal;
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
                    case CoercionKind.PenImageToHyperlink:
                        unaryOpKind = UnaryOpKind.PenImageToHyperlink;
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
                    case CoercionKind.HyperlinkToImage:
                        unaryOpKind = UnaryOpKind.HyperlinkToImage;
                        break;
                    case CoercionKind.PenImageToImage:
                        unaryOpKind = UnaryOpKind.PenImageToImage;
                        break;
                    case CoercionKind.BlobToImage:
                        unaryOpKind = UnaryOpKind.BlobToImage;
                        break;
                    case CoercionKind.TextToMedia:
                        unaryOpKind = UnaryOpKind.TextToMedia;
                        break;
                    case CoercionKind.BlobToMedia:
                        unaryOpKind = UnaryOpKind.BlobToMedia;
                        break;
                    case CoercionKind.HyperlinkToMedia:
                        unaryOpKind = UnaryOpKind.HyperlinkToMedia;
                        break;
                    case CoercionKind.TextToBlob:
                        unaryOpKind = UnaryOpKind.TextToBlob;
                        break;
                    case CoercionKind.HyperlinkToBlob:
                        unaryOpKind = UnaryOpKind.HyperlinkToBlob;
                        break;
                    case CoercionKind.NumberToText:
                        unaryOpKind = UnaryOpKind.NumberToText;
                        break;
                    case CoercionKind.DecimalToText:
                        unaryOpKind = UnaryOpKind.DecimalToText;
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
                    case CoercionKind.DecimalToBoolean:
                        unaryOpKind = UnaryOpKind.DecimalToBoolean;
                        break;
                    case CoercionKind.TextToBoolean:
                        unaryOpKind = UnaryOpKind.TextToBoolean;
                        break;
                    case CoercionKind.OptionSetToBoolean:
                        unaryOpKind = UnaryOpKind.OptionSetToBoolean;
                        break;
                    case CoercionKind.RecordToTable:
                        unaryOpKind = UnaryOpKind.RecordToTable;
                        break;
                    case CoercionKind.NumberToDateTime:
                        unaryOpKind = UnaryOpKind.NumberToDateTime;
                        break;
                    case CoercionKind.DecimalToDateTime:
                        unaryOpKind = UnaryOpKind.DecimalToDateTime;
                        break;
                    case CoercionKind.NumberToDate:
                        unaryOpKind = UnaryOpKind.NumberToDate;
                        break;
                    case CoercionKind.DecimalToDate:
                        unaryOpKind = UnaryOpKind.DecimalToDate;
                        break;
                    case CoercionKind.NumberToTime:
                        unaryOpKind = UnaryOpKind.NumberToTime;
                        break;
                    case CoercionKind.DecimalToTime:
                        unaryOpKind = UnaryOpKind.DecimalToTime;
                        break;
                    case CoercionKind.DateTimeToDate:
                        unaryOpKind = UnaryOpKind.DateTimeToDate;
                        break;
                    case CoercionKind.DateToDateTime:
                        unaryOpKind = UnaryOpKind.DateToDateTime;
                        break;
                    case CoercionKind.DateTimeToTime:
                        unaryOpKind = UnaryOpKind.DateTimeToTime;
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
                    case CoercionKind.TextToGUID:
                        unaryOpKind = UnaryOpKind.TextToGUID;
                        break;
                    case CoercionKind.GUIDToText:
                        unaryOpKind = UnaryOpKind.GUIDToText;
                        break;
                    case CoercionKind.NumberToCurrency:
                        unaryOpKind = UnaryOpKind.NumberToCurrency;
                        break;
                    case CoercionKind.TextToCurrency:
                        unaryOpKind = UnaryOpKind.TextToCurrency;
                        break;
                    case CoercionKind.CurrencyToNumber:
                        unaryOpKind = UnaryOpKind.CurrencyToNumber;
                        break;
                    case CoercionKind.CurrencyToBoolean:
                        unaryOpKind = UnaryOpKind.CurrencyToBoolean;
                        break;
                    case CoercionKind.BooleanToCurrency:
                        unaryOpKind = UnaryOpKind.BooleanToCurrency;
                        break;
                    case CoercionKind.CurrencyToText:
                        unaryOpKind = UnaryOpKind.CurrencyToText;
                        break;
                    case CoercionKind.MediaToText:
                        unaryOpKind = UnaryOpKind.MediaToText;
                        break;
                    case CoercionKind.ImageToText:
                        unaryOpKind = UnaryOpKind.ImageToText;
                        break;
                    case CoercionKind.BlobToText:
                        unaryOpKind = UnaryOpKind.BlobToText;
                        break;
                    case CoercionKind.PenImageToText:
                        unaryOpKind = UnaryOpKind.PenImageToText;
                        break;
                    case CoercionKind.UntypedToText:
                        return new CallNode(IRContext.NotInSource(FormulaType.Build(toType)), BuiltinFunctionsCore.Text_UO, child);
                    case CoercionKind.UntypedToNumber:
                        return new CallNode(
                            IRContext.NotInSource(FormulaType.Build(toType)),
                            context.Binding.BindingConfig.NumberIsFloat ? BuiltinFunctionsCore.Value_UO : BuiltinFunctionsCore.Float_UO,
                            child);
                    case CoercionKind.UntypedToDecimal:
                        return new CallNode(IRContext.NotInSource(FormulaType.Build(toType)), BuiltinFunctionsCore.Decimal_UO, child);
                    case CoercionKind.UntypedToBoolean:
                        return new CallNode(IRContext.NotInSource(FormulaType.Build(toType)), BuiltinFunctionsCore.Boolean_UO, child);
                    case CoercionKind.UntypedToDate:
                        return new CallNode(IRContext.NotInSource(FormulaType.Build(toType)), BuiltinFunctionsCore.DateValue_UO, child);
                    case CoercionKind.UntypedToTime:
                        return new CallNode(IRContext.NotInSource(FormulaType.Build(toType)), BuiltinFunctionsCore.TimeValue_UO, child);
                    case CoercionKind.UntypedToDateTime:
                        return new CallNode(IRContext.NotInSource(FormulaType.Build(toType)), BuiltinFunctionsCore.DateTimeValue_UO, child);
                    case CoercionKind.UntypedToColor:
                        return new CallNode(IRContext.NotInSource(FormulaType.Build(toType)), BuiltinFunctionsCore.ColorValue_UO, child);
                    case CoercionKind.UntypedToGUID:
                        return new CallNode(IRContext.NotInSource(FormulaType.Build(toType)), BuiltinFunctionsCore.GUID_UO, child);
                    case CoercionKind.None:
                        // No coercion needed, return the child node
                        return child;
                    default:
                        throw new InvalidOperationException("Unexpected Coercion Kind: " + coercionKind);
                }

                return new UnaryOpNode(IRContext.NotInSource(FormulaType.Build(toType)), unaryOpKind, child);
            }

            private static System.Drawing.Color ConvertToColor(double input)
            {
                var value = (uint)input;
                return System.Drawing.Color.FromArgb(
                            (byte)((value >> 24) & 0xFF),
                            (byte)((value >> 16) & 0xFF),
                            (byte)((value >> 8) & 0xFF),
                            (byte)(value & 0xFF));
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

            public IRContext GetIRContext(TexlNode node, DType type)
            {
                return new IRContext(node.GetTextSpan(), FormulaType.Build(type));
            }
        }
    }
}
