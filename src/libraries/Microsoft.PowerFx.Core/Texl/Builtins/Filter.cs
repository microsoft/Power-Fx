// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Filter(source:*, predicate1:b, [predicate2:b, ...])
    // Corresponding DAX function: Filter
    internal sealed class FilterFunction : FilterFunctionBase
    {
        public FilterFunction()
            : base("Filter", TexlStrings.AboutFilter, FunctionCategories.Table, DType.EmptyTable, -2, 2, int.MaxValue, DType.EmptyTable)
        {
            ScopeInfo = new FunctionScopeInfo(this, acceptsLiteralPredicates: false);
        }

        public override bool SupportsParamCoercion => true;

        public override bool CheckTypesAndSemanticsOnly => true;

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            // Enumerate just the base overloads (the first 3 possibilities).
            yield return new[] { TexlStrings.FilterArg1, TexlStrings.FilterArg2 };
            yield return new[] { TexlStrings.FilterArg1, TexlStrings.FilterArg2, TexlStrings.FilterArg2 };
            yield return new[] { TexlStrings.FilterArg1, TexlStrings.FilterArg2, TexlStrings.FilterArg2, TexlStrings.FilterArg2 };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 2)
            {
                return GetGenericSignatures(arity, TexlStrings.FilterArg1, TexlStrings.FilterArg2);
            }

            return base.GetSignatures(arity);
        }

        protected override bool CheckTypes(TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            nodeToCoercedTypeMap = null;

            var fArgsValid = base.CheckTypes(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            // The first Texl function arg determines the cursor type, the scope type for the lambda params, and the return type.
            fArgsValid &= ScopeInfo.CheckInput(args[0], argTypes[0], errors, out var typeScope);

            Contracts.Assert(typeScope.IsRecord);
            returnType = typeScope.ToTable();

            return fArgsValid;
        }

        protected override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            var viewCount = 0;

            var dataSourceVisitor = new ViewFilterDataSourceVisitor(binding);

            // Ensure that all the args starting at index 1 are booleans or view
            for (var i = 1; i < args.Length; i++)
            {
                if (argTypes[i].Kind == DKind.ViewValue)
                {
                    if (++viewCount > 1)
                    {
                        // Only one view expected
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrOnlyOneViewExpected);
                        continue;
                    }

                    // Use the visitor to get the datasource info and if a view was already used anywhere in the node tree.
                    args[0].Accept(dataSourceVisitor);
                    var dataSourceInfo = dataSourceVisitor.CdsDataSourceInfo;

                    if (dataSourceVisitor.ContainsViewFilter)
                    {
                        // Only one view expected
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrOnlyOneViewExpected);
                        continue;
                    }

                    if (dataSourceInfo != null)
                    {
                        // Verify the view belongs to the same datasource
                        var viewInfo = argTypes[i].ViewInfo.VerifyValue();
                        if (viewInfo.RelatedEntityName != dataSourceInfo.Name)
                        {
                            errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrViewFromCurrentTableExpected, dataSourceInfo.Name);
                        }
                    }
                    else
                    {
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrBooleanExpected);
                    }

                    continue;
                }
                else if (DType.Boolean.Accepts(argTypes[i]))
                {
                    continue;
                }
                else if (!argTypes[i].CoercesTo(DType.Boolean))
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrBooleanExpected);
                    continue;
                }
            }
        }

        // Verifies if given callnode can be server delegatable or not.
        // Return true if
        //        - Arg0 is delegatable ds and supports filter operation.
        //        - All predicates to filter are delegatable if each firstname/binary/unary/dottedname/call node in each predicate satisfies delegation criteria set by delegation strategy for each node.
        public override bool IsServerDelegatable(CallNode callNode, TexlBinding binding)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            if (!CheckArgsCount(callNode, binding))
            {
                return false;
            }

            FilterOpMetadata metadata = null;
            if (TryGetEntityMetadata(callNode, binding, out IDelegationMetadata delegationMetadata))
            {
                if (!binding.Document.Properties.EnabledFeatures.IsEnhancedDelegationEnabled ||
                    !TryGetValidDataSourceForDelegation(callNode, binding, DelegationCapability.ArrayLookup, out _))
                {
                    SuggestDelegationHint(callNode, binding);
                    return false;
                }

                metadata = delegationMetadata.FilterDelegationMetadata.VerifyValue();
            }
            else
            {
                if (!TryGetValidDataSourceForDelegation(callNode, binding, FunctionDelegationCapability, out var dataSource))
                {
                    return false;
                }

                metadata = dataSource.DelegationMetadata.FilterDelegationMetadata;
            }

            var args = callNode.Args.Children.VerifyValue();

            // Validate for each predicate node.
            for (var i = 1; i < args.Length; i++)
            {
                if (!IsValidDelegatableFilterPredicateNode(args[i], binding, metadata))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool IsEcsExcemptedLambda(int index)
        {
            // All lambdas in filter can be excluded from ECS.
            return IsLambdaParam(index);
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
