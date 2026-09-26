// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Update(DataSource, OldRecord, NewRecord, [RemoveFlags])
    internal class UpdateFunction : BuiltinFunction
    {
        public override bool ManipulatesCollections => true;

        public override bool ModifiesValues => true;

        public override bool IsSelfContained => false;

        public override bool AllowedWithinNondeterministicOperationOrder => false;

        public override bool SupportsParamCoercion => true;

        public override bool MutatesArg(int argIndex, TexlNode arg) => argIndex == 0;

        public UpdateFunction()
            : base("Update", TexlStrings.AboutUpdate, FunctionCategories.Behavior | FunctionCategories.Table, DType.EmptyRecord, 0, 3, 4, DType.EmptyTable, DType.EmptyRecord, DType.EmptyRecord, DType.String)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.UpdateArg_Source, TexlStrings.UpdateArg_Record, TexlStrings.UpdateArg_Update };
            yield return new[] { TexlStrings.UpdateArg_Source, TexlStrings.UpdateArg_Record, TexlStrings.UpdateArg_Update, TexlStrings.UpdateArg_RemoveFlags };
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            DType collectionType = argTypes[0];
            if (!collectionType.IsTable)
            {
                errors.EnsureError(args[0], TexlStrings.ErrNeedTable_Func, Name);
                fValid = false;
            }

            // Verify OldRecord type (arg 1)
            DType oldRecordType = argTypes[1];
            if (!oldRecordType.IsRecord)
            {
                errors.EnsureError(args[1], TexlStrings.ErrNeedRecord_Arg, args[1]);
                fValid = false;
            }
            else
            {
                if (!collectionType.Accepts(oldRecordType.ToTable(), exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules))
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrTableDoesNotAcceptThisType);
                    fValid = false;
                }
            }

            // Verify NewRecord type (arg 2)
            DType newRecordType = argTypes[2];
            if (!newRecordType.IsRecord)
            {
                errors.EnsureError(args[2], TexlStrings.ErrNeedRecord_Arg, args[2]);
                fValid = false;
            }
            else
            {
                if (!collectionType.Accepts(newRecordType.ToTable(), exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules))
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[2], TexlStrings.ErrTableDoesNotAcceptThisType);
                    fValid = false;
                }
            }

            // Verify RemoveFlags (arg 3, if present)
            if (args.Length == 4)
            {
                DType removeFlagsType = argTypes[3];
                if (!DType.String.Accepts(removeFlagsType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules))
                {
                    errors.EnsureError(args[3], TexlStrings.ErrRemoveAllArg, args[3]);
                    fValid = false;
                }
                else if (args[3] is StrLitNode strNode)
                {
                    if (strNode.Value.ToUpperInvariant() != "ALL")
                    {
                        errors.EnsureError(args[3], TexlStrings.ErrRemoveAllArg, args[3]);
                        fValid = false;
                    }
                }
            }

            returnType = context.Features.PowerFxV1CompatibilityRules ? DType.Void : collectionType.ToRecord();

            return fValid;
        }

        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            base.CheckSemantics(binding, args, argTypes, errors);
            base.ValidateArgumentIsMutable(binding, args[0], errors);
        }
    }
}
