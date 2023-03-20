// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Functions.Delegation.DelegationStrategies;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // AsType(record:P, table:*[]): ![]
    internal sealed class AsTypeFunction : BuiltinFunction
    {
        public const string AsTypeInvariantFunctionName = "AsType";

        public override bool IsAsync => true;

        public override bool CanReturnExpandInfo => true;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public AsTypeFunction()
            : base(AsTypeInvariantFunctionName, TexlStrings.AboutAsType, FunctionCategories.Table, DType.EmptyRecord, 0, 2, 2, DType.Error /* Polymorphic type is checked in override */, DType.EmptyTable)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.AsTypeArg1, TexlStrings.AsTypeArg2 };
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == 2);
            Contracts.Assert(argTypes.Length == 2);
            Contracts.AssertValue(errors);

            if (!base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap))
            {
                return false;
            }

            var tableArgType = argTypes[1];

            if (tableArgType.AssociatedDataSources.Any())
            {
                var tableDsInfo = tableArgType.AssociatedDataSources.Single();

                if (context.IsEnhancedDelegationEnabled && (tableDsInfo is IExternalCdsDataSource) && argTypes[0].HasPolymorphicInfo)
                {
                    var expandInfo = argTypes[0].PolymorphicInfo.TryGetExpandInfo(tableDsInfo.TableMetadata.Name);
                    if (expandInfo != null)
                    {
                        returnType = argTypes[0].ExpandPolymorphic(argTypes[1], expandInfo);
                        return true;
                    }
                }
            }

            returnType = argTypes[1].ToRecord();
            return true;
        }

        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            // Check if first argument is poly type or an activity pointer
            if (!argTypes[0].IsPolymorphic && !argTypes[0].IsActivityPointer)
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrBadType_ExpectedType_ProvidedType, DKind.Polymorphic.ToString(), argTypes[0].GetKindString());
            }

            // Check if table arg referrs to a connected data source.
            var tableArg = args[1];
            if (!binding.TryGetFirstNameInfo(tableArg.Id, out var tableInfo) ||
                tableInfo.Data is not IExternalDataSource tableDsInfo ||
                !(tableDsInfo is IExternalTabularDataSource))
            {
                errors.EnsureError(tableArg, TexlStrings.ErrAsTypeAndIsTypeExpectConnectedDataSource);
            }
        }

        public override bool IsRowScopedServerDelegatable(CallNode callNode, TexlBinding binding, OperationCapabilityMetadata metadata)
        {
            return binding.Document.Properties.EnabledFeatures.IsEnhancedDelegationEnabled && metadata.IsDelegationSupportedByTable(DelegationCapability.AsType);
        }

        protected override bool RequiresPagedDataForParamCore(TexlNode[] args, int paramIndex, TexlBinding binding)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.Assert(paramIndex >= 0 && paramIndex < args.Length);
            Contracts.AssertValue(binding);
            Contracts.Assert(binding.IsPageable(args[paramIndex].VerifyValue()));

            // For the second argument, we need only metadata. No actual data from datasource is required.
            return paramIndex != 1;
        }

        public override ICallNodeDelegatableNodeValidationStrategy GetCallNodeDelegationStrategy()
        {
            return new AsTypeCallNodeDelegationStrategy(this);
        }
    }

    internal sealed class AsTypeCallNodeDelegationStrategy : DelegationValidationStrategy
    {
        public AsTypeCallNodeDelegationStrategy(TexlFunction function)
            : base(function)
        {
        }

        protected override bool IsValidAsyncOrImpureNode(TexlNode node, TexlBinding binding)
        {
            // AsType should always be marked as valid regardless of it being async and impure.
            return true;
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
