// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Publish;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Refresh(source:*[...]) : b
    // Refresh(source:![...]) : b    
    internal sealed class RefreshFunction : BuiltinFunction
    {
        // This is a stateful asynchronous function with side effects.
        public override bool IsSelfContained => false;

        public override bool ModifiesValues => true;

        public override bool IsStateless => false;

        public override bool IsAsync => true;

        public override bool DisableForDataComponent => true;

        public override Capabilities Capabilities => Capabilities.OutboundInternetAccess | Capabilities.EnterpriseAuthentication | Capabilities.PrivateNetworkAccess;

        public override bool SupportsParamCoercion => false;

        public RefreshFunction()
            : base("Refresh", TexlStrings.AboutRefresh, FunctionCategories.Table, DType.Boolean, 0, 1, 1)
        { }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.RefreshArg1 };
        }

        // We have suggestions for the first argument.
        public override bool HasSuggestionsForParam(int argumentIndex)
        {
            Contracts.Assert(argumentIndex >= 0);

            return argumentIndex == 0;
        }

        protected override bool RequiresPagedDataForParamCore(TexlNode[] args, int paramIndex, TexlBinding binding)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.Assert(paramIndex >= 0 && paramIndex < args.Length);
            Contracts.AssertValue(binding);
            Contracts.Assert(binding.IsPageable(args[paramIndex].VerifyValue()));

            // Refresh only needs metadata. No actual data from datasource is required.
            return false;
        }

        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            Contracts.AssertValue(binding);
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.AssertAllValid(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            if (!argTypes[0].IsAggregate)
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrNeedAgg);
                return;
            }

            // Only certain types of data sources are refreshable.
            // Note that we're only adding errors we know for sure that the argument refers to a non-refreshable
            // data source. Currently this can only be done for simple FirstNameNodes. For more complex expressions such as:
            //      Refresh(If(a < 1, Data1, [1,2,3]))
            // ...we have no way of inferring statically what the possible result of evaluating the expression will be,
            // so it's not safe to flag the rule with errors. The runtime safety mechanisms will later kick in and prevent
            // crashes when the rule gets evaluated. If the argument evaluates to a non-refreshable data source, such as
            // a collection or excel table, the Refresh invocation will simply be a no-op in that case.
            DataSourceInfo dsInfo;
            FirstNameInfo firstNameInfo;
            if (binding.TryCastToFirstName(args[0], out firstNameInfo) &&
                binding.IsInfoKindDataSource(firstNameInfo) &&
                (dsInfo = firstNameInfo.Data as DataSourceInfo) != null &&
                !dsInfo.IsRefreshable)
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrDataSourceCannotBeRefreshed);
            }
        }

        public override IEnumerable<Identifier> GetIdentifierOfModifiedValue(TexlNode[] args, out TexlNode identifierNode)
        {
            Contracts.AssertValue(args);

            identifierNode = null;
            if (args.Length == 0)
            {
                return null;
            }

            var firstNameNode = args[0]?.AsFirstName();
            identifierNode = firstNameNode;
            if (firstNameNode == null)
            {
                return null;
            }

            var identifiers = new List<Identifier>();
            identifiers.Add(firstNameNode.Ident);
            return identifiers;
        }
    }
}
