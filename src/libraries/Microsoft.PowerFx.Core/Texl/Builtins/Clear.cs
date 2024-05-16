// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // !!!TODO 
    //[RequiresErrorContext]
    //[ComponentVisibilityRestriction(
    //    DisableForDataComponent = true,
    //    DisableForCommanding = true,
    //    SuppressIntellisenseForComponent = true)]

    // Clear(collection:*[])
    internal class ClearFunction : BuiltinFunction
    {
        public override bool ManipulatesCollections => true;

        public override bool ModifiesValues => true;

        public override bool IsSelfContained => false;

        public override bool AllowedWithinNondeterministicOperationOrder => false;

        public override bool SupportsParamCoercion => false;

        public override bool MutatesArg0 => true;

        public ClearFunction()
            : base("Clear", TexlStrings.AboutClear, FunctionCategories.Behavior, DType.Unknown, 0, 1, 1, DType.EmptyTable)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.ClearArg1 };
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            bool fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsUnknown);

            DType collectionType = argTypes[0];
            if (!collectionType.IsTable)
            {
                fValid = false;
                errors.EnsureError(args[0], TexlStrings.ErrNeedTable_Func, Name);
            }

            returnType = context.Features.PowerFxV1CompatibilityRules ? DType.Void : collectionType;

            return fValid;
        }

        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            Contracts.AssertValue(binding);
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            DType dataSourceType = argTypes[0];

            if (binding.Features.PowerFxV1CompatibilityRules && dataSourceType.IsTable && dataSourceType.Kind != DKind.ObjNull)
            {
                base.ValidateArgumentIsMutable(binding, args[0], errors);
            }

            if (binding.EntityScope != null && 
                binding.EntityScope.TryGetDataSource(args[0], out IExternalDataSource dataSourceInfo) &&
                !dataSourceInfo.IsClearable)
            {
                // We block non-clearable data sources.
                errors.EnsureError(args[0], TexlStrings.ErrInvalidDataSourceForFunction);
            }

            MutationUtils.CheckSemantics(binding, this, args, argTypes, errors);
        }

        // This method returns true if there are special suggestions for a particular parameter of the function.
        public override bool HasSuggestionsForParam(int argumentIndex)
        {
            Contracts.Assert(argumentIndex >= 0);

            return argumentIndex == 0;
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

            var identifiers = new List<Identifier>
            {
                firstNameNode.Ident
            };

            return identifiers;
        }
    }
}
