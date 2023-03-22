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
    internal sealed class ModFunction : MathFunction
    {
        public ModFunction()
            : base("Mod", TexlStrings.AboutMod, FunctionCategories.MathAndStat, 2, 2, nativeDecimal: true)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            // Decimal TODO: Generic and use Math func names?
            return EnumerableUtils.Yield(new[] { TexlStrings.ModFuncArg1, TexlStrings.ModFuncArg2 });
        }
    }

    // Mod(number:n|*[n], divisor:n|*[n])
    // Decimal TODO: Should derive from MathTableFunction, needs interpreter implementation
    internal sealed class ModTFunction : MathTableFunction
    {
        public ModTFunction()
            : base("Mod", TexlStrings.AboutModT, FunctionCategories.Table, 2, 2, nativeDecimal: true)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            // Decimal TODO: Generic and use Math func names?
            yield return new[] { TexlStrings.ModTFuncArg1, TexlStrings.ModTFuncArg2 };
        }

#if false
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

            var fValid = true;
            nodeToCoercedTypeMap = null;

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

            returnType = DType.CreateTable(new TypedName(DType.Number, GetOneColumnTableResultName(context.Features)));

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
#endif
    }
}
