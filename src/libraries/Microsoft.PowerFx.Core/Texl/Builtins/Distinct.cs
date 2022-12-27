// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata;
using Microsoft.PowerFx.Core.Functions.FunctionArgValidators;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Logging.Trackers;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Distinct(source:*, valueFunc:b)
    internal sealed class DistinctFunction : FunctionWithTableInput
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public DistinctFunction()
            : base("Distinct", TexlStrings.AboutDistinct, FunctionCategories.Table, DType.EmptyTable, 0x02, 2, 2, DType.EmptyTable)
        {
            ScopeInfo = new FunctionScopeInfo(this);
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.DistinctArg1, TexlStrings.DistinctArg2 };
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = CheckTypes(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            var exprType = argTypes[1];

            returnType = DType.CreateTable(new TypedName(exprType, GetOneColumnTableResultName(context.Features)));

            return fValid;
        }

        // This method returns true if there are special suggestions for a particular parameter of the function.
        public override bool HasSuggestionsForParam(int argumentIndex)
        {
            Contracts.Assert(argumentIndex >= 0);

            return argumentIndex == 0;
        }
    }
}
