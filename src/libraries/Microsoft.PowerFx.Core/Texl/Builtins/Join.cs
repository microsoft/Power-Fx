// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Core.IR.IRTranslator;
using IRCallNode = Microsoft.PowerFx.Core.IR.Nodes.CallNode;
using RecordNode = Microsoft.PowerFx.Core.IR.Nodes.RecordNode;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal class JoinFunction : FilterFunctionBase
    {
        public override bool IsSelfContained => true;

        public override int ScopeArgs => 2;

        public JoinFunction()
            : base("Join", TexlStrings.AboutJoin, FunctionCategories.Table, DType.EmptyTable, 0x8, 5, int.MaxValue, DType.EmptyTable, DType.EmptyTable)
        {
            ScopeInfo = new FunctionJoinScopeInfo(this);
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.JoinArg1, TexlStrings.JoinArg2, TexlStrings.JoinArg3 };
            yield return new[] { TexlStrings.JoinArg1, TexlStrings.JoinArg2, TexlStrings.JoinArg3, TexlStrings.JoinArg4 };
            yield return new[] { TexlStrings.JoinArg1, TexlStrings.JoinArg2, TexlStrings.JoinArg3, TexlStrings.JoinArg4, TexlStrings.JoinArg5 };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 5)
            {
                return GetGenericSignatures(arity, TexlStrings.JoinArg5, TexlStrings.JoinArg5);
            }

            return base.GetSignatures(arity);
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            var valid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            var leftTable = argTypes[0];
            var rightTable = argTypes[1];
            var atLeastOneRigthRecordField = false;

            // We include all arg[0] columns by default. Makers will explicitly declare columns from arg[1].
            returnType = leftTable.Clone();

            // JoinType argument is present?
            if (argTypes.Count() > 3 &&
                (!BuiltInEnums.JoinTypeEnum.FormulaType._type.Accepts(argTypes[3], exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules) ||
                args[3] is not DottedNameNode))
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[3], TexlStrings.ErrJoinNotPlainJoinTypeEnum);
                valid = false;
            }

            ScopeInfo.GetScopeIdent(args, out var scopeIdent);

            if (args.Count() > 4)
            {                
                var fError = false;

                // 5th arg on must be renaming/declaration args. It is mandatory for makers to declarer at least 1 RightRecord column.
                foreach (var node in args.Skip(4))
                {
                    if (node is not AsNode asNode)
                    {
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrJoinArgIsNotAsNode);
                        valid = false;
                        break;
                    }

                    if (asNode.Left is not DottedNameNode dottedNameNode)
                    {
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrJoinArgIsNotAsNode);
                        valid = false;
                        break;
                    }
                    
                    if (dottedNameNode.Left.AsFirstName().Ident.Name == scopeIdent[0])
                    {
                        if (!leftTable.TryGetType(dottedNameNode.Right.Name, out var leftType))
                        {
                            errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrColDNE_Name, dottedNameNode);
                            valid = false;
                            break;
                        }

                        returnType = returnType.Drop(ref fError, DPath.Root, dottedNameNode.Right.Name);
                        returnType = returnType.Add(ref fError, DPath.Root, asNode.Right.Name, leftType);
                    }
                    else if (dottedNameNode.Left.AsFirstName().Ident.Name == scopeIdent[1])
                    {
                        if (!rightTable.TryGetType(dottedNameNode.Right.Name, out var rightType))
                        {
                            errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrColDNE_Name, dottedNameNode);
                            valid = false;
                            break;
                        }

                        returnType = returnType.Add(ref fError, DPath.Root, asNode.Right.Name, rightType);

                        atLeastOneRigthRecordField = true;
                    }
                    else
                    {
                        // No new error message. Binder already added error for unknown scope.
                        valid = false;
                    }                    

                    if (fError)
                    {
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrJoinCantAddRename, node);
                        valid = false;
                        break;
                    }
                }
            }

            if (!atLeastOneRigthRecordField)
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrJoinAtLeastOneRigthRecordField, scopeIdent[1]);
                valid = false;
            }

            return valid;
        }

        public override bool IsLambdaParam(TexlNode node, int index)
        {
            return index == 2 || (index > 3 && node is AsNode);
        }

        public override IEnumerable<string> GetRequiredEnumNames()
        {
            return new List<string>() { BuiltInEnums.JoinTypeEnum.EntityName.Value };
        }

        public override ParamIdentifierStatus GetIdentifierParamStatus(TexlNode node, Features features, int index)
        {
            return ParamIdentifierStatus.NeverIdentifier;
        }

        internal override IntermediateNode CreateIRCallNode(PowerFx.Syntax.CallNode node, IRTranslatorContext context, List<IntermediateNode> args, ScopeSymbol scope)
        {
            var carg = node.Args.Count;
            var newArgs = new List<IntermediateNode>();
            var recordTypesMap = new Dictionary<DName, RecordType>();
            var recordValueMap = new Dictionary<DName, Dictionary<DName, IntermediateNode>>();

            // Inlcudes source1, source2, predicate, and joinType.
            newArgs.AddRange(args.Take(4));

            ScopeInfo.GetScopeIdent(node.Args.Children.ToArray(), out var scopeIdent);

            for (int i = 4; i < carg; i++)
            {
                var asNode = node.Args.Children[i] as AsNode;
                var dName = asNode.Left.AsDottedName().Left.AsFirstName().Ident.Name == scopeIdent[0] ? FunctionJoinScopeInfo.LeftRecord : FunctionJoinScopeInfo.RightRecord;

                if (!recordTypesMap.TryGetValue(dName, out _))
                {
                    recordTypesMap[dName] = RecordType.Empty();
                }

                recordTypesMap[dName] = recordTypesMap[dName].Add(asNode.Left.AsDottedName().Right.Name, FormulaType.String);

                if (!recordValueMap.TryGetValue(dName, out _))
                {
                    recordValueMap[dName] = new Dictionary<DName, IntermediateNode>();
                }

                recordValueMap[dName].Add(asNode.Left.AsDottedName().Right.Name, new TextLiteralNode(IRContext.NotInSource(FormulaType.String), asNode.Right.Name));
            }

            if (recordTypesMap.ContainsKey(FunctionJoinScopeInfo.LeftRecord))
            {
                newArgs.Add(new RecordNode(
                    IRContext.NotInSource(recordTypesMap[FunctionJoinScopeInfo.LeftRecord]),
                    recordValueMap[FunctionJoinScopeInfo.LeftRecord]));
            }
            else
            {
                newArgs.Add(new RecordNode(
                    IRContext.NotInSource(RecordType.Empty()), new Dictionary<DName, IntermediateNode>()));
            }

            if (recordTypesMap.ContainsKey(FunctionJoinScopeInfo.RightRecord))
            {
                newArgs.Add(new RecordNode(
                    IRContext.NotInSource(recordTypesMap[FunctionJoinScopeInfo.RightRecord]),
                    recordValueMap[FunctionJoinScopeInfo.RightRecord]));
            }
            else
            {
                newArgs.Add(new RecordNode(
                    IRContext.NotInSource(RecordType.Empty()), new Dictionary<DName, IntermediateNode>()));
            }

            return new IRCallNode(context.GetIRContext(node), this, scope, newArgs);
        }
    }
}
