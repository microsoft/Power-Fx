// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Mod(number:n, divisor:n)
    internal sealed class ModFunction : BuiltinFunction
    {
        public override bool SupportsParamCoercion => true;

        public override bool IsSelfContained => true;

        public ModFunction()
            : base("Mod", TexlStrings.AboutMod, FunctionCategories.MathAndStat, DType.Number, 0, 0, 2, 2, DType.Number, DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            return EnumerableUtils.Yield(new[] { TexlStrings.ModFuncArg1, TexlStrings.ModFuncArg2 });
        }
    }

    // Mod(number:n|*[n], divisor:n|*[n])
    internal sealed class ModTFunction : BuiltinFunction
    {
        public override bool SupportsParamCoercion => true;

        public override bool IsSelfContained => true;

        public ModTFunction()
            : base("Mod", TexlStrings.AboutModT, FunctionCategories.Table, DType.EmptyTable, 0, 0, 2, 2)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.ModTFuncArg1, TexlStrings.ModTFuncArg2 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "_T");
        }

        public override bool CheckInvocation(TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = base.CheckInvocation(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            var type0 = argTypes[0];
            var type1 = argTypes[1];

            var arg0 = args[0];
            var arg1 = args[1];

            // Arg0 should be either a number or a column of number (coercion is ok).
            bool matchedWithCoercion;
            if (type0.IsTable)
            {
                // Ensure we have a one-column table of numbers.
                fValid &= CheckNumericColumnType(type0, arg0, errors, ref nodeToCoercedTypeMap);
            }
            else if (CheckType(arg0, type0, DType.Number, DefaultErrorContainer, out matchedWithCoercion))
            {
                if (matchedWithCoercion)
                {
                    CollectionUtils.Add(ref nodeToCoercedTypeMap, arg0, DType.Number);
                }
            }
            else
            {
                fValid = false;
                errors.EnsureError(DocumentErrorSeverity.Severe, arg0, TexlStrings.ErrNumberExpected);
            }

            // Arg1 should be either a number or a column of number (coercion is ok).
            if (type1.IsTable)
            {
                fValid &= CheckNumericColumnType(type1, arg1, errors, ref nodeToCoercedTypeMap);
            }
            else if (CheckType(arg1, type1, DType.Number, DefaultErrorContainer, out matchedWithCoercion))
            {
                if (matchedWithCoercion)
                {
                    CollectionUtils.Add(ref nodeToCoercedTypeMap, arg1, DType.Number);
                }
            }
            else
            {
                fValid = false;
                errors.EnsureError(DocumentErrorSeverity.Severe, arg1, TexlStrings.ErrNumberExpected);
            }

            returnType = DType.CreateTable(new TypedName(DType.Number, OneColumnTableResultName));

            // At least one arg has to be a table.
            if (!(type0.IsTable || type1.IsTable))
            {
                fValid = false;
            }

            if (!fValid)
            {
                nodeToCoercedTypeMap = null;
            }

            return fValid;
        }
    }
}
