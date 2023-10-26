﻿// Copyright (c) Microsoft Corporation.
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
    // Replace(source:s, start:n, count:n, replacement:s)
    internal sealed class ReplaceFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public ReplaceFunction()
            : base("Replace", TexlStrings.AboutReplace, FunctionCategories.Text, DType.String, 0, 4, 4, DType.String, DType.Number, DType.Number, DType.String)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.ReplaceFuncArg1, TexlStrings.StringFuncArg2, TexlStrings.StringFuncArg3, TexlStrings.ReplaceFuncArg4 };
        }
    }

    // Replace(source:s|*[s], start:n|*[n], count:n|*[n], replacement:s|*[s])
    internal sealed class ReplaceTFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public ReplaceTFunction()
            : base("Replace", TexlStrings.AboutReplaceT, FunctionCategories.Table, DType.EmptyTable, 0, 4, 4)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.StringTFuncArg1, TexlStrings.StringFuncArg2, TexlStrings.StringFuncArg3, TexlStrings.ReplaceFuncArg4 };
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
            var type2 = argTypes[2];
            var type3 = argTypes[3];

            // Arg0 should be either a string or a column of strings.
            // Its type dictates the function return type.
            if (type0.IsTable)
            {
                // Ensure we have a one-column table of strings
                fValid &= CheckStringColumnType(context, args[0], type0, errors, ref nodeToCoercedTypeMap, out returnType);
            }
            else
            {
                returnType = DType.CreateTable(new TypedName(DType.String, GetOneColumnTableResultName(context.Features)));
                if (!DType.String.Accepts(type0, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules))
                {
                    if (type0.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, context.Features))
                    {
                        CollectionUtils.Add(ref nodeToCoercedTypeMap, args[0], DType.String);
                    }
                    else
                    {
                        fValid = false;
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrStringExpected);
                    }
                }
            }

            // Arg1 should be either a number or a column of numbers.
            if (type1.IsTable)
            {
                fValid &= CheckNumericColumnType(context, args[1], type1, errors, ref nodeToCoercedTypeMap);
            }
            else if (!CheckType(context, args[1], type1, DType.Number, errors, ref nodeToCoercedTypeMap))
            { 
                fValid = false;
                errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrNumberExpected);
            }

            // Arg2 should be either a number or a column of numbers.
            if (type2.IsTable)
            {
                fValid &= CheckNumericColumnType(context, args[2], type2, errors, ref nodeToCoercedTypeMap);
            }
            else if (!CheckType(context, args[2], type2, DType.Number, errors, ref nodeToCoercedTypeMap))
            {
                fValid = false;
                errors.EnsureError(DocumentErrorSeverity.Severe, args[2], TexlStrings.ErrNumberExpected);
            }

            // Arg3 should be either a string or a column of strings.
            if (type3.IsTable)
            {
                fValid &= CheckStringColumnType(context, args[3], type3, errors, ref nodeToCoercedTypeMap);
            }
            else if (!DType.String.Accepts(type3, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules))
            {
                if (type3.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, context.Features))
                {
                    CollectionUtils.Add(ref nodeToCoercedTypeMap, args[3], DType.String);
                }
                else
                {
                    fValid = false;
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[3], TexlStrings.ErrStringExpected);
                }
            }

            // At least one arg has to be a table.
            if (!type0.IsTable && !type1.IsTable && !type2.IsTable && !type3.IsTable)
            {
                fValid = false;
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrTypeError);
                errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrTypeError);
                errors.EnsureError(DocumentErrorSeverity.Severe, args[2], TexlStrings.ErrTypeError);
                errors.EnsureError(DocumentErrorSeverity.Severe, args[3], TexlStrings.ErrTypeError);
            }

            return fValid;
        }
    }
}
