// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

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
        // GroupBy(source, name, name, ..., name, name, groupName)
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

            const string thisGroup = "ThisGroup";

            bool isValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            // Source table type
            DType sourceType = argTypes[0];

            if (args[0] is AsNode)
            {
                isValid = false;
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrSummarizeDataSourceScopeNotSupported);
            }

            if (sourceType.Contains(new DName(thisGroup)))
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

                    case DottedNameNode dottedNameNode:
                        existingType = argType;
                        columnName = dottedNameNode.Right.Name;
                        atLeastOneGroupByColumn = true;
                        break;

                    case FirstNameNode:
                        if (!TryGetColumnLogicalName(sourceType, context.Features.SupportColumnNamesAsIdentifiers, arg, errors, out columnName, out existingType))
                        {
                            isValid = false;
                            continue;
                        }

                        atLeastOneGroupByColumn = true;
                        break;

                    default:
                        isValid = false;
                        errors.EnsureError(DocumentErrorSeverity.Severe, arg, TexlStrings.ErrSummarizeInvalidArg);
                        continue;
                }

                if (columnName == thisGroup)
                {
                    isValid = false;
                    errors.EnsureError(DocumentErrorSeverity.Severe, arg, TexlStrings.ErrSummarizeThisGroupColumnName);
                    continue;
                }
                
                if (!existingType.IsPrimitive)
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
            if (!features.SupportColumnNamesAsIdentifiers)
            {
                return ParamIdentifierStatus.NeverIdentifier;
            }

            return index == 0 ? ParamIdentifierStatus.NeverIdentifier : ParamIdentifierStatus.PossiblyIdentifier;
        }

        public override bool TranslateAsNodeToRecordNode(TexlNode node, int index)
        {
            Contracts.Assert(node != null);

            return index > 0 && node is AsNode asNode;
        }

        public override bool ParameterCanBeIdentifier(TexlNode node, int index, Features features)
        {
            return index > 0 && node is not AsNode;
        }

        public override bool IsLambdaParam(TexlNode node, int index)
        {
            Contracts.AssertIndexInclusive(index, MaxArity);
            Contracts.Assert(node != null);

            return index > 0 && node is AsNode;
        }
    }
}
