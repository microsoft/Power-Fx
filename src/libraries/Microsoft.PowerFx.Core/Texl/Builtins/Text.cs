// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Text(arg:n|s|d)
    // Text(arg:n|s|d, format:s)
    // Text(arg:n|s|d, format:s, language:s)
    // Corresponding DAX functions: Format, Fixed
    internal sealed class TextFunction : BuiltinFunction
    {
        public override bool SupportsParamCoercion => true;

        public override bool IsSelfContained => true;

        public TextFunction()
            : base("Text", TexlStrings.AboutText, FunctionCategories.Table | FunctionCategories.Text | FunctionCategories.DateTime, DType.String, 0, 1, 3, DType.Number, DType.String, DType.String)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.TextArg1, TexlStrings.TextArg2 };
            yield return new[] { TexlStrings.TextArg1, TexlStrings.TextArg2, TexlStrings.TextArg3 };
        }

        public override bool CheckInvocation(TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
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
            var isValidNumber = CheckType(arg0, arg0Type, DType.Number, DefaultErrorContainer, out var matchedWithCoercion);
            var arg0CoercedType = matchedWithCoercion ? DType.Number : DType.Invalid;

            if (!isValidNumber || matchedWithCoercion)
            {
                if (DType.DateTime.Accepts(arg0Type))
                {
                    // No coercion needed for datetimes here.
                    arg0CoercedType = DType.Invalid;
                }
                else
                {
                    isValidString = CheckType(arg0, arg0Type, DType.String, DefaultErrorContainer, out matchedWithCoercion);

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

            StrLitNode formatNode;
            if (!DType.String.Accepts(argTypes[1]))
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrStringExpected);
                isValid = false;
            }
            else if ((formatNode = args[1].AsStrLit()) != null)
            {
                // Verify statically that the format string doesn't contain BOTH numeric and date/time
                // format specifiers. If it does, that's an error accd to Excel and our spec.
                var fmt = formatNode.Value;

                // But firstly skip any locale-prefix
                if (fmt.StartsWith("[$-", StringComparison.OrdinalIgnoreCase))
                {
                    var end = fmt.IndexOf(']', 3);
                    if (end > 0)
                    {
                        fmt = fmt.Substring(end + 1);
                    }
                }

                var hasDateTimeFmt = fmt.IndexOfAny(new char[] { 'm', 'd', 'y', 'h', 'H', 's', 'a', 'A', 'p', 'P' }) >= 0;
                var hasNumericFmt = fmt.IndexOfAny(new char[] { '0', '#' }) >= 0;
                if (hasDateTimeFmt && hasNumericFmt)
                {
                    errors.EnsureError(DocumentErrorSeverity.Moderate, formatNode, TexlStrings.ErrIncorrectFormat_Func, Name);
                    isValid = false;
                }
            }

            if (args.Length > 2)
            {
                var argType = argTypes[2];
                if (!DType.String.Accepts(argType))
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[2], TexlStrings.ErrStringExpected);
                    isValid = false;
                }
            }

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

        // This method returns true if there are special suggestions for a particular parameter of the function.
        public override bool HasSuggestionsForParam(int argumentIndex)
        {
            Contracts.Assert(argumentIndex >= 0);

            return argumentIndex == 1 || argumentIndex == 2;
        }

        public override IEnumerable<string> GetRequiredEnumNames()
        {
            return new List<string>() { EnumConstants.DateTimeFormatEnumString };
        }
    }

    // Text(arg:O)
    internal sealed class TextFunction_UO : BuiltinFunction
    {
        public override bool SupportsParamCoercion => false;

        public override bool IsSelfContained => true;

        public TextFunction_UO()
            : base("Text", TexlStrings.AboutText, FunctionCategories.Text, DType.String, 0, 1, 1, DType.UntypedObject)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.TextArg1 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "_UO");
        }
    }
}
