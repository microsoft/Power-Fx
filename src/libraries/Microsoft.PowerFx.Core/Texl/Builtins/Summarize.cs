// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // GroupBy(source:*, groupBy_col:s [, groupBy_col:s, ...], groupName:s)
    // !!!TODO [RequiresErrorContext]
    internal sealed class SummarizeFunction : FunctionWithTableInput
    {
        // !!! TODO UNCOMMENT
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public override bool HasColumnIdentifiers => true;

        public SummarizeFunction()
            : base("Summarize", TexlStrings.AboutSummarize, FunctionCategories.Table, DType.EmptyTable, 0, 2, int.MaxValue, DType.EmptyTable)
        {
            // Summarize(source, groupby_column, aggregation, ..., groupby_column, ..., aggregation, ...)
            SignatureConstraint = new SignatureConstraint(omitStartIndex: 3, repeatSpan: 1, endNonRepeatCount: 1, repeatTopLength: 6);

            ScopeInfo = new FunctionScopeInfo(this);
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

            bool isValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            // Source table type
            DType sourceType = argTypes[0];

            if (!sourceType.IsTable)
            {
                sourceType = DType.EmptyTable;
            }

            int lastIndex = args.Length - 1;
            DType groupKeyType = DType.EmptyTable, groupType = sourceType;

            var supportColumnNamesAsIdentifiers = context.Features.SupportColumnNamesAsIdentifiers;

            for (int i = 1; i < args.Length; i++)
            {
                // All args starting at index 1 need to be identifiers or lambda.

                var argType = argTypes[i];
                var arg = args[i];

                if (!base.TryGetColumnLogicalName(sourceType, supportColumnNamesAsIdentifiers, args[i], errors, out DName columnName, out DType existingType))
                {
                    isValid = false;
                    continue;
                }

                // Summarize can only group by primitive keys.
                if (!existingType.IsPrimitive)
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrNeedPrimitive);
                    isValid = false;
                    continue;
                }

                /*
                if (i < lastIndex)
                {
                    if (!base.TryGetColumnLogicalName(sourceType, supportColumnNamesAsIdentifiers, args[i], errors, out DName columnName, out DType existingType))
                    {
                        isValid = false;
                        continue;
                    }

                    // GroupBy can only group by primitive keys.
                    if (!existingType.IsPrimitive)
                    {
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrNeedPrimitive);
                        isValid = false;
                        continue;
                    }

                    // Keys cannot be specified multiple times.
                    bool dropError = false;
                    groupType = groupType.Drop(ref dropError, DPath.Root, columnName);
                    if (dropError)
                    {
                        Contracts.Assert(groupKeyType.TryGetType(columnName, out existingType));
                        errors.EnsureError(DocumentErrorSeverity.Moderate, args[i], TexlStrings.ErrColMultipleDest_Name, columnName);
                        isValid = false;
                    }
                    else
                    {
                        groupKeyType = groupKeyType.Add(columnName, existingType);
                    }
                }
                else
                {
                    if (!base.TryGetColumnLogicalName(null, supportColumnNamesAsIdentifiers, args[i], errors, out DName columnName))
                    {
                        isValid = false;
                        continue;
                    }

                    // The group name has to not collide with any other column in the resulting table.
                    Contracts.Assert(groupKeyType.IsTable);
                    if (groupKeyType.TryGetType(columnName, out DType existingType))
                    {
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrAddColNameExists);
                        isValid = false;
                        continue;
                    }

                    // Assemble the final return type from the type that respresents the group keys
                    // and the nested/grouped type.
                    Contracts.Assert(groupType.IsTable);
                    returnType = groupKeyType.Add(columnName, groupType);
                }
                */
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

            return index == 0 ? ParamIdentifierStatus.NeverIdentifier : ParamIdentifierStatus.AlwaysIdentifier;
        }

        // !!! TODO UNCOMMENT
        /*
        public override bool HasSuggestionsForParam(int argumentIndex)
        {
            Contracts.Assert(0 <= argumentIndex);

            return argumentIndex > 0 || base.HasSuggestionsForParam(argumentIndex);
        }

        public override ParamIdentifierStatus GetIdentifierParamStatus(Features features, int index)
        {
            if (!features.SupportColumnNamesAsIdentifiers)
            {
                return ParamIdentifierStatus.NeverIdentifier;
            }

            return index == 0 ? ParamIdentifierStatus.NeverIdentifier : ParamIdentifierStatus.AlwaysIdentifier;
        }

        public override bool AffectsDataSourceQueryOptions { get { return true; } }

        public override bool UpdateDataQuerySelects(CallNode callNode, TexlBinding binding, DataSourceToQueryOptionsMap dataSourceToQueryOptionsMap)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            if (!CheckArgsCount(callNode, binding))
                return false;

            var args = Contracts.VerifyValue(callNode.Args.Children);

            DType dsType = binding.GetType(args[0]);
            if (dsType.AssociatedDataSources == null)
                return false;

            bool retval = false;

            var supportColumnNamesAsIdentifiers = binding.Features.SupportColumnNamesAsIdentifiers;

            for (var i = 1; i < args.Count - 1; i++)
            {
                base.TryGetColumnLogicalName(
                    dsType,
                    supportColumnNamesAsIdentifiers,
                    args[i],
                    TexlFunction.DefaultErrorContainer,
                    out var columnName,
                    out var columnType).Verify("This has been validated by CheckTypes");

                retval |= dsType.AssociateDataSourcesToSelect(dataSourceToQueryOptionsMap, columnName, columnType, true);
            }

            return retval;
        }

        public override bool IsServerDelegatable(CallNode callNode, TexlBinding binding)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            if (!CheckArgsCount(callNode, binding))
            {
                return false;
            }

            IExternalDataSource dataSource = null;

            if (!TryGetValidDataSourceForDelegation(callNode, binding, FunctionDelegationCapability, out dataSource))
            {
                if (dataSource != null && !dataSource.IsDelegatable)
                {
                    TrackingProvider.Instance.SetDelegationTrackerStatus(DelegationStatus.DataSourceNotDelegatable, callNode, binding, this, DelegationTelemetryInfo.CreateDataSourceNotDelegatableTelemetryInfo(dataSource));
                    return false;
                }
            }

            // This function always returns false. We are recording telemetry for why it might not be delegable
            TrackingProvider.Instance.SetDelegationTrackerStatus(DelegationStatus.DelegationSuccessful, callNode, binding, this);
            return false;
        } 
        */
    }
}
