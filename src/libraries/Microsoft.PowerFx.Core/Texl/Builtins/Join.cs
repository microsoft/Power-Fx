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
            : base("Join", TexlStrings.AboutJoin, FunctionCategories.Table, DType.EmptyTable, 0x8, 3, int.MaxValue, DType.EmptyTable, DType.EmptyTable)
        {
            ScopeInfo = new FunctionJoinScopeInfo(this);
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.JoinArg1, TexlStrings.JoinArg2, TexlStrings.JoinArg3 };
            yield return new[] { TexlStrings.JoinArg1, TexlStrings.JoinArg2, TexlStrings.JoinArg3, TexlStrings.JoinArg4 };

            // !!!TODO Add more signatures here.
            //yield return new[] { TexlStrings.JoinArg1, TexlStrings.JoinArg2, TexlStrings.JoinArg3, TexlStrings.JoinArg4 };
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            var valid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            var leftTable = argTypes[0];
            var rightTable = argTypes[1];
            var leftTableClone = leftTable.Clone();
            var rightTableClone = rightTable.Clone();

            // JoinType argument is present?
            if (argTypes.Count() > 3 &&
                (!BuiltInEnums.JoinTypeEnum.FormulaType._type.Accepts(argTypes[3], exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules) ||
                args[3] is not DottedNameNode))
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[3], TexlStrings.ErrJoinNotPlainJoinTypeEnum);
                valid = false;
            }

            if (args.Count() > 4)
            {
                var fError = false;

                // 5th arg on must be renaming args.
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

                    switch (dottedNameNode.Left.AsFirstName().Ident.Name.Value)
                    {
                        case "LeftRecord":
                            if (!leftTable.TryGetType(dottedNameNode.Right.Name, out var leftType))
                            {
                                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrColDNE_Name, dottedNameNode);
                                valid = false;
                                break;
                            }

                            leftTableClone = leftTableClone.Drop(ref fError, DPath.Root, dottedNameNode.Right.Name);
                            leftTableClone = leftTableClone.Add(ref fError, DPath.Root, asNode.Right.Name, leftType);
                            break;

                        case "RightRecord":
                            if (!rightTable.TryGetType(dottedNameNode.Right.Name, out var rightType))
                            {
                                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrColDNE_Name, dottedNameNode);
                                valid = false;
                                break;
                            }

                            rightTableClone = rightTableClone.Drop(ref fError, DPath.Root, dottedNameNode.Right.Name);
                            rightTableClone = rightTableClone.Add(ref fError, DPath.Root, asNode.Right.Name, rightType);
                            break;

                        default:
                            // No new error message. Binder already added error for unknown scope.
                            valid = false;
                            break;
                    }

                    if (fError)
                    {
                        valid = false;
                        break;
                    }
                }
            }

            if (leftTableClone.CanUnionWithForcedUniqueColumns(rightTableClone, useLegacyDateTimeAccepts: false, features: context.Features, out var duplicatedDType))
            {
                returnType = DType.Union(leftTableClone, rightTableClone, false, Features.PowerFxV1);
            }
            else
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrJoinCantUnion, string.Join(", ", duplicatedDType.GetAllNames(DPath.Root).Select(tname => tname.Name.Value)));
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
            var sourceNameProvider = new Dictionary<DName, DName>();

            for (int i = 0; i < carg; i++)
            {
                var arg = node.Args.Children[i];

                // Left
                if (i == 0)
                {
                    sourceNameProvider[arg is AsNode asNode ? asNode.Left.AsDottedName().Left.AsFirstName().Ident.Name : FunctionJoinScopeInfo.LeftRecord] = FunctionJoinScopeInfo.LeftRecord;
                }

                // Right
                if (i == 1)
                {
                    sourceNameProvider[arg is AsNode asNode ? asNode.Left.AsDottedName().Left.AsFirstName().Ident.Name : FunctionJoinScopeInfo.RightRecord] = FunctionJoinScopeInfo.RightRecord;
                }

                if (i > 3 && IsLambdaParam(arg, i))
                {
                    var asNode = (AsNode)arg;
                    var dName = sourceNameProvider[asNode.Left.AsDottedName().Left.AsFirstName().Ident.Name];

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
                else
                {
                    newArgs.Add(args[i]);
                }
            }

            if (carg == 3)
            {
                newArgs.Add(
                    new RecordFieldAccessNode(
                        IRContext.NotInSource(FormulaType.Build(DType.OptionSetValue)),
                        new ResolvedObjectNode(IRContext.NotInSource(FormulaType.Build(DType.OptionSetValue)), BuiltInEnums.JoinTypeEnum),
                        new DName("Inner")));
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
