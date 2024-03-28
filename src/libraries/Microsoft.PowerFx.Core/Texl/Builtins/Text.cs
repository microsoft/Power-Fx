﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Syntax.PrettyPrintVisitor;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Text(arg:n|s|d)
    // Text(arg:n|s|d, format:s)
    // Text(arg:n|s|d, format:s, language:s)
    // Corresponding DAX functions: Format, Fixed
    internal sealed class TextFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public const string TextInvariantFunctionName = "Text";

        public TextFunction()
            : base(TextInvariantFunctionName, TexlStrings.AboutText, FunctionCategories.Table | FunctionCategories.Text | FunctionCategories.DateTime, DType.String, 0, 1, 3, DType.Number, BuiltInEnums.DateTimeFormatEnum.FormulaType._type, DType.String)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.TextArg1, TexlStrings.TextArg2 };
            yield return new[] { TexlStrings.TextArg1, TexlStrings.TextArg2, TexlStrings.TextArg3 };
        }

        public override bool CheckTypes(CheckTypesContext checkTypesContext, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var isValid = true;
            returnType = DType.String;
            nodeToCoercedTypeMap = null;

            var arg0 = args[0];
            var arg0Type = argTypes[0];

            var isValidString = true;
            var isValidNumber = false;
            var matchedWithCoercion = false;
            DType arg0CoercedType = null;

            if (
                !DType.Decimal.Accepts(
                    arg0Type,
                    exact: true,
                    useLegacyDateTimeAccepts: false,
                    usePowerFxV1CompatibilityRules: checkTypesContext.Features.PowerFxV1CompatibilityRules) &&
                (checkTypesContext.NumberIsFloat || DType.Number.Accepts(arg0Type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: checkTypesContext.Features.PowerFxV1CompatibilityRules)))
            {
                isValidNumber = CheckType(checkTypesContext, arg0, arg0Type, DType.Number, DefaultErrorContainer, out matchedWithCoercion);
                arg0CoercedType = matchedWithCoercion ? DType.Number : DType.Invalid;
            }
            else
            {
                isValidNumber = CheckType(checkTypesContext, arg0, arg0Type, DType.Decimal, DefaultErrorContainer, out matchedWithCoercion);
                arg0CoercedType = matchedWithCoercion ? DType.Decimal : DType.Invalid;
            }

            if (!isValidNumber || matchedWithCoercion)
            {
                if (DType.DateTime.Accepts(arg0Type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: checkTypesContext.Features.PowerFxV1CompatibilityRules) ||
                    DType.Time.Accepts(arg0Type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: checkTypesContext.Features.PowerFxV1CompatibilityRules) ||
                    DType.Date.Accepts(arg0Type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: checkTypesContext.Features.PowerFxV1CompatibilityRules))
                {
                    // No coercion needed for datetimes here.
                    arg0CoercedType = DType.Invalid;
                }
                else
                {
                    isValidString = CheckType(checkTypesContext, arg0, arg0Type, DType.String, DefaultErrorContainer, out matchedWithCoercion);

                    if (isValidString)
                    {
                        if (matchedWithCoercion)
                        {
                            // If both the matches were with coercion, we pick string over number.
                            // For instance Text(true) returns true in the case of EXCEL. If we picked
                            // number coercion, then we would return 1 and it will not match EXCEL behavior.
                            arg0CoercedType = DType.String;
                        }
                        else
                        {
                            arg0CoercedType = DType.Invalid;
                        }
                    }
                }
            }

            if (!isValidNumber && !isValidString)
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrNumberOrStringExpected);
                isValid = false;
            }

            if (args.Length < 2)
            {
                if (isValid && arg0CoercedType.IsValid)
                {
                    CollectionUtils.Add(ref nodeToCoercedTypeMap, arg0, arg0CoercedType);
                    return true;
                }

                return isValid;
            }

            ValidateFormatArgs(Name, checkTypesContext, args, argTypes, errors, ref nodeToCoercedTypeMap, ref isValid);

            if (isValid)
            {
                if (arg0CoercedType.IsValid)
                {
                    CollectionUtils.Add(ref nodeToCoercedTypeMap, arg0, arg0CoercedType);
                    return true;
                }
            }
            else
            {
                nodeToCoercedTypeMap = null;
            }

            return isValid;
        }

        internal static void ValidateFormatArgs(string name, CheckTypesContext checkTypesContext, TexlNode[] args, DType[] argTypes, IErrorContainer errors, ref Dictionary<TexlNode, DType> nodeToCoercedTypeMap, ref bool isValid)
        {
            bool hasDateTimeFormatEnum = BuiltInEnums.DateTimeFormatEnum.FormulaType._type.Accepts(argTypes[1], exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: checkTypesContext.Features.PowerFxV1CompatibilityRules);
            if (checkTypesContext.Features.PowerFxV1CompatibilityRules && argTypes[0] != DType.UntypedObject && !TextFormatUtils.AllowedListToUseFormatString.Contains(argTypes[0]))
            {
                errors.EnsureError(DocumentErrorSeverity.Moderate, args[1], TexlStrings.ErrNotSupportedFormat_Func, name);
                isValid = false;
            }
            else if (!checkTypesContext.Features.StronglyTypedBuiltinEnums || !hasDateTimeFormatEnum)
            {
                if (hasDateTimeFormatEnum)
                {
                    // Coerce enum values to string
                    CollectionUtils.Add(ref nodeToCoercedTypeMap, args[1], DType.String);
                }
                else if (!DType.String.Accepts(argTypes[1], exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: checkTypesContext.Features.PowerFxV1CompatibilityRules))
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrStringExpected);
                    isValid = false;
                }
                else if (BinderUtils.TryGetConstantValue(checkTypesContext, args[1], out var formatArg))
                {
                    if (checkTypesContext.Features.PowerFxV1CompatibilityRules)
                    {
                        if (!TextFormatUtils.IsValidFormatArg(formatArg, formatCulture: null, defaultLanguage: null, out var textFormatArgs))
                        {
                            isValid = false;
                        }
                    }
                    else if (!TextFormatUtils.IsLegacyValidCompiledTimeFormatArg(formatArg))
                    {
                        isValid = false;
                    }

                    if (!isValid)
                    {
                        errors.EnsureError(DocumentErrorSeverity.Moderate, args[1], TexlStrings.ErrIncorrectFormat_Func, name);
                    }
                }
            }

            if (args.Length > 2)
            {
                var argType = argTypes[2];
                if (!DType.String.Accepts(argType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: checkTypesContext.Features.PowerFxV1CompatibilityRules))
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[2], TexlStrings.ErrStringExpected);
                    isValid = false;
                }
            }
        }

        // This method returns true if there are special suggestions for a particular parameter of the function.
        public override bool HasSuggestionsForParam(int argumentIndex)
        {
            Contracts.Assert(argumentIndex >= 0);

            return argumentIndex == 1 || argumentIndex == 2;
        }

        public override IEnumerable<string> GetRequiredEnumNames()
        {
            return new List<string>() { LanguageConstants.DateTimeFormatEnumString };
        }
    }

    // Text(arg:O)
    internal sealed class TextFunction_UO : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public TextFunction_UO()
            : base(TextFunction.TextInvariantFunctionName, TexlStrings.AboutText, FunctionCategories.Text, DType.String, 0, 1, 3, DType.UntypedObject, DType.String, DType.String)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.TextArg1 };
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            var isValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            if (args.Length > 1)
            {
                // The 2nd and 3rd arguments can be validated using the same logic as the normal Text function
                TextFunction.ValidateFormatArgs(Name, context, args, argTypes, errors, ref nodeToCoercedTypeMap, ref isValid);
            }

            return isValid;
        }

        public override ArgPreprocessor GetArgPreprocessor(int index, int argCount)
        {
            if (index == 0 && argCount > 1)
            {
                return ArgPreprocessor.UntypedStringToUntypedNumber;
            }

            return base.GetArgPreprocessor(index, argCount);
        }
    }
}
