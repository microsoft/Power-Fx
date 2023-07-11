// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // ColorFade(color:c|*[c], fadeDelta:n|*[n])
    internal sealed class ColorFadeTFunction : BuiltinFunction
    {
        private static readonly string TableKindString = DType.EmptyTable.GetKindString();

        public override bool IsSelfContained => true;

        public ColorFadeTFunction()
            : base("ColorFade", TexlStrings.AboutColorFadeT, FunctionCategories.Table, DType.EmptyTable, 0, 2, 2)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.ColorFadeTArg1, TexlStrings.ColorFadeTArg2 };
        }

        public override IEnumerable<string> GetRequiredEnumNames()
        {
            return new List<string>() { LanguageConstants.ColorEnumString };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "_T");
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

            var type0 = argTypes[0];
            var type1 = argTypes[1];

            var otherType = DType.Invalid;
            TexlNode otherArg = null;

            // At least one of the arguments has to be a table.
            if (type0.IsTableNonObjNull)
            {
                // Ensure we have a one-column table of colors.
                fValid &= CheckColorColumnType(context, args[0], type0, errors, ref nodeToCoercedTypeMap, out returnType);

                // Check arg1 below.
                otherArg = args[1];
                otherType = type1;

                fValid &= CheckOtherType(context, otherType, otherArg, DType.Number, errors, ref nodeToCoercedTypeMap);

                Contracts.Assert(returnType.IsTable);
                Contracts.Assert(!fValid || returnType.IsColumn);
            }
            else if (type1.IsTableNonObjNull)
            {
                // Ensure we have a one-column table of numerics.
                fValid &= CheckNumericColumnType(context, args[1], type1, errors, ref nodeToCoercedTypeMap);

                // Since the 1st arg is not a table, make a new table return type *[Result:c]
                returnType = DType.CreateTable(new TypedName(DType.Color, GetOneColumnTableResultName(context.Features)));

                // Check arg0 below.
                otherArg = args[0];
                otherType = type0;

                fValid &= CheckOtherType(context, otherType, otherArg, DType.Color, errors, ref nodeToCoercedTypeMap);

                Contracts.Assert(returnType.IsTable);
                Contracts.Assert(!fValid || returnType.IsColumn);
            }
            else
            {
                Contracts.Assert(returnType.IsTable);

                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrTypeError_Ex1_Ex2_Found, TableKindString, DType.Color.GetKindString(), type0.GetKindString());
                errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrTypeError_Ex1_Ex2_Found, TableKindString, DType.Number.GetKindString(), type1.GetKindString());

                // Both args are invalid. No need to continue.
                return false;
            }

            return fValid;
        }

        private bool CheckOtherType(CheckTypesContext context, DType otherType, TexlNode otherArg, DType expectedType, IErrorContainer errors, ref Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.Assert(otherType.IsValid);
            Contracts.AssertValue(otherArg);
            Contracts.Assert(expectedType == DType.Color || expectedType == DType.Number);
            Contracts.AssertValue(errors);

            if (otherType.IsTableNonObjNull)
            {
                // Ensure we have a one-column table of numerics/color values based on expected type.
                return CheckColumnType(context, otherArg, otherType, expectedType == DType.Number ? DType.Number : DType.Color, errors, ref nodeToCoercedTypeMap);
            }

            if (expectedType.Accepts(otherType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules))
            {
                return true;
            }

            if (otherType.CoercesTo(expectedType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules))
            {
                CollectionUtils.Add(ref nodeToCoercedTypeMap, otherArg, expectedType);
                return true;
            }

            errors.EnsureError(DocumentErrorSeverity.Severe, otherArg, TexlStrings.ErrTypeError_Ex1_Ex2_Found, TableKindString, expectedType.GetKindString(), otherType.GetKindString());
            return false;
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
