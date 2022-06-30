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
    // Substitute(source:s, match:s, replacement:s, [instanceCount:n])
    internal sealed class SubstituteFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public SubstituteFunction()
            : base("Substitute", TexlStrings.AboutSubstitute, FunctionCategories.Text, DType.String, 0, 3, 4, DType.String, DType.String, DType.String, DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.SubstituteFuncArg1, TexlStrings.SubstituteFuncArg2, TexlStrings.SubstituteFuncArg3 };
            yield return new[] { TexlStrings.SubstituteFuncArg1, TexlStrings.SubstituteFuncArg2, TexlStrings.SubstituteFuncArg3, TexlStrings.SubstituteFuncArg4 };
        }
    }

    // Substitute(source:s|*[s], match:s|*[s], replacement:s|*[s], [instanceCount:n|*[n]])
    internal sealed class SubstituteTFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public SubstituteTFunction()
            : base("Substitute", TexlStrings.AboutSubstituteT, FunctionCategories.Table, DType.EmptyTable, 0, 3, 4)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.SubstituteTFuncArg1, TexlStrings.SubstituteFuncArg2, TexlStrings.SubstituteFuncArg3 };
            yield return new[] { TexlStrings.SubstituteTFuncArg1, TexlStrings.SubstituteFuncArg2, TexlStrings.SubstituteFuncArg3, TexlStrings.SubstituteFuncArg4 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "_T");
        }

        public override bool CheckInvocation(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = CheckInvocation(args, argTypes, errors, out _, out nodeToCoercedTypeMap);

            var type0 = argTypes[0];
            var type1 = argTypes[1];
            var type2 = argTypes[2];

            // Arg0 should be either a string or a column of strings.
            // Its type dictates the function return type.
            if (type0.IsTable)
            {
                // Ensure we have a one-column table of strings
                fValid &= CheckStringColumnType(type0, args[0], errors, ref nodeToCoercedTypeMap);

                // Borrow the return type from the 1st arg
                returnType = type0;
            }
            else
            {
                returnType = DType.CreateTable(new TypedName(DType.String, OneColumnTableResultName));
                if (!DType.String.Accepts(type0))
                {
                    if (type0.CoercesTo(DType.String))
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

            // Arg1 should be either a string or a column of strings.
            if (type1.IsTable)
            {
                fValid &= CheckStringColumnType(type1, args[1], errors, ref nodeToCoercedTypeMap);
            }
            else if (!DType.String.Accepts(type1))
            {
                if (type1.CoercesTo(DType.String))
                {
                    CollectionUtils.Add(ref nodeToCoercedTypeMap, args[1], DType.String);
                }
                else
                {
                    fValid = false;
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrStringExpected);
                }
            }

            // Arg2 should be either a string or a column of strings.
            if (type2.IsTable)
            {
                fValid &= CheckStringColumnType(type2, args[2], errors, ref nodeToCoercedTypeMap);
            }
            else if (!DType.String.Accepts(type2))
            {
                if (type2.CoercesTo(DType.String))
                {
                    CollectionUtils.Add(ref nodeToCoercedTypeMap, args[2], DType.String);
                }
                else
                {
                    fValid = false;
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[2], TexlStrings.ErrStringExpected);
                }
            }

            var hasCount = args.Length == 4;
            if (hasCount)
            {
                var type3 = argTypes[3];

                // Arg3 should be either a number or a column of numbers.
                if (type3.IsTable)
                {
                    fValid &= CheckNumericColumnType(type3, args[3], errors, ref nodeToCoercedTypeMap);
                }
                else if (!DType.Number.Accepts(type3))
                {
                    if (type3.CoercesTo(DType.Number))
                    {
                        CollectionUtils.Add(ref nodeToCoercedTypeMap, args[3], DType.Number);
                    }
                    else
                    {
                        fValid = false;
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[3], TexlStrings.ErrNumberExpected);
                    }
                }
            }

            // At least one arg has to be a table.
            if (!(type0.IsTable || type1.IsTable || type2.IsTable) && (!hasCount || !argTypes[3].IsTable))
            {
                fValid = false;
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrTypeError);
                errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrTypeError);
                errors.EnsureError(DocumentErrorSeverity.Severe, args[2], TexlStrings.ErrTypeError);
                if (hasCount)
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[3], TexlStrings.ErrTypeError);
                }
            }

            return fValid;
        }
    }
}
