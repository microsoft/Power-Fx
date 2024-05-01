// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Core.IR.IRTranslator;
using CallNode = Microsoft.PowerFx.Syntax.CallNode;
using IRCallNode = Microsoft.PowerFx.Core.IR.Nodes.CallNode;
using RecordNode = Microsoft.PowerFx.Core.IR.Nodes.RecordNode;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Summarize( Table, GroupByColumn1 [, GroupByColumn2 …], AggregateExpr1 As Name [, AggregateExpr1 As Name …] )
    internal sealed class SummarizeFunction : FunctionWithTableInput
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public override bool HasColumnIdentifiers => true;

        public SummarizeFunction()
            : base("Summarize", TexlStrings.AboutSummarize, FunctionCategories.Table, DType.EmptyTable, 0, 2, int.MaxValue, DType.EmptyTable)
        {
            SignatureConstraint = new SignatureConstraint(omitStartIndex: 3, repeatSpan: 1, endNonRepeatCount: 1, repeatTopLength: 6);
            ScopeInfo = new FunctionThisGroupScopeInfo(this);
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.SummarizeArg1, TexlStrings.SummarizeArg2, TexlStrings.SummarizeArg3 };
            yield return new[] { TexlStrings.SummarizeArg1, TexlStrings.SummarizeArg2, TexlStrings.SummarizeArg3, TexlStrings.SummarizeArg2 };
            yield return new[] { TexlStrings.SummarizeArg1, TexlStrings.SummarizeArg2, TexlStrings.SummarizeArg2, TexlStrings.SummarizeArg3, TexlStrings.SummarizeArg3 };
        }

        // Produce a signature of the form:
        // Sumamrize(source, grouping_column, aggregation, grouping_column, aggregation, ...)
        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            Contracts.Assert(arity >= 0);

            if (arity <= 3)
            {
                return base.GetSignatures(arity);
            }

            // Limit the argCount avoiding potential OOM
            int argCount = arity > SignatureConstraint.RepeatTopLength + SignatureConstraint.EndNonRepeatCount ? SignatureConstraint.RepeatTopLength + SignatureConstraint.EndNonRepeatCount : arity;
            var args = new TexlStrings.StringGetter[argCount];

            int lastIndex = argCount - 1;
            args[0] = TexlStrings.SummarizeArg1;

            var addSwitch = false;

            for (int i = 1; i < lastIndex; i++)
            {
                if (addSwitch)
                {
                    args[i] = TexlStrings.SummarizeArg2;
                }
                else
                {
                    args[i] = TexlStrings.SummarizeArg3;
                }

                addSwitch = !addSwitch;
            }

            return new[] { args };
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            bool isValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            // Source table type
            DType sourceType = argTypes[0];

            if (args[0] is AsNode)
            {
                isValid = false;
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrSummarizeDataSourceScopeNotSupported);
            }

            if (sourceType.Contains(FunctionThisGroupScopeInfo.ThisGroup))
            {
                isValid = false;
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrSummarizeDataSourceContainsThisGroupColumn);
            }

            var atLeastOneGroupByColumn = false;

            for (int i = 1; i < args.Length; i++)
            {
                var argType = argTypes[i];
                var arg = args[i];

                DName columnName;
                DType existingType;

                // All args starting at index 1 need to be identifiers or aggregate functions (wrapped in AsNode node).
                switch (arg)
                {
                    case AsNode nameNode:
                        existingType = argType;
                        columnName = nameNode.Right.Name;
                        break;

                    case FirstNameNode:
                        if (!TryGetColumnLogicalName(sourceType, true, arg, errors, out columnName, out existingType))
                        {
                            isValid = false;
                            continue;
                        }

                        if (DType.TryGetDisplayNameForColumn(sourceType, columnName, out var displayName))
                        {
                            if (displayName == FunctionThisGroupScopeInfo.ThisGroup)
                            {
                                isValid = false;
                                errors.EnsureError(DocumentErrorSeverity.Severe, arg, TexlStrings.ErrSummarizeThisGroupColumnName);
                                continue;
                            }
                        }

                        atLeastOneGroupByColumn = true;
                        break;

                    default:
                        isValid = false;
                        errors.EnsureError(DocumentErrorSeverity.Severe, arg, TexlStrings.ErrSummarizeInvalidArg);
                        continue;
                }

                if (columnName.Equals(FunctionThisGroupScopeInfo.ThisGroup))
                {
                    isValid = false;
                    errors.EnsureError(DocumentErrorSeverity.Severe, arg, TexlStrings.ErrSummarizeThisGroupColumnName);
                    continue;
                }
                
                // Restricted supported types
                if (!(existingType == DType.String || 
                      existingType == DType.Number || 
                      existingType == DType.Decimal || 
                      existingType == DType.Boolean || 
                      existingType == DType.Time || 
                      existingType == DType.OptionSet || 
                      existingType == DType.OptionSetValue || 
                      existingType == DType.DateTime ||
                      existingType == DType.Date))
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, arg, TexlStrings.ErrNeedPrimitive);
                    isValid = false;
                    continue;
                }

                var fError = false;
                returnType = returnType.Add(ref fError, DPath.Root, columnName, existingType);
                if (fError)
                {
                    isValid = false;
                    errors.EnsureError(DocumentErrorSeverity.Severe, arg, TexlStrings.ErrColConflict_Name, columnName);
                    continue;
                }
            }

            if (!atLeastOneGroupByColumn)
            {
                isValid = false;
                errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrSummarizeNoGroupBy);
            }

            Contracts.Assert(returnType.IsTable);
            return isValid;
        }

        public override ParamIdentifierStatus GetIdentifierParamStatus(Features features, int index)
        {
            return index == 0 ? ParamIdentifierStatus.NeverIdentifier : ParamIdentifierStatus.PossiblyIdentifier;
        }

        public override bool ParameterCanBeIdentifier(TexlNode node, int index, Features features)
        {
            return index > 0 && node is not AsNode;
        }

        public override bool IsLambdaParam(TexlNode node, int index)
        {
            Contracts.AssertIndexInclusive(index, MaxArity);

            return index > 0 && node != null && (node is AsNode || node is CallNode);
        }

        internal override IntermediateNode CreateIRCallNode(CallNode node, IRTranslatorContext context, List<IntermediateNode> args, ScopeSymbol scope)
        {
            var carg = node.Args.Count;
            var newArgs = new List<IntermediateNode>();

            for (int i = 0; i < carg; i++)
            {
                var arg = node.Args.Children[i];

                if (IsLambdaParam(arg, i))
                {
                    var asNode = (AsNode)arg;
                    var irCallNode = (LazyEvalNode)args[i];
                    var recordType = RecordType.Empty().Add(asNode.Right.Name, context.GetIRContext(arg).ResultType);

                    var child = new RecordNode(
                        IRContext.NotInSource(recordType),
                        new Dictionary<DName, IntermediateNode>() { { asNode.Right.Name, irCallNode.Child } });

                    newArgs.Add(new LazyEvalNode(context.GetIRContext(arg), child));
                }
                else
                {
                    newArgs.Add(args[i]);
                }
            }

            return new IRCallNode(context.GetIRContext(node), this, scope, newArgs);
        }
    }
}
