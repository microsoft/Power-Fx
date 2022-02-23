// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Min(arg1:n, arg2:n, ..., argN:n)
    // Max(arg1:n, arg2:n, ..., argN:n)
    // Corresponding Excel functions: Min, Max
    internal sealed class MinMaxFunction_DT : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        private readonly bool _isMin;

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            if (_isMin)
            {
                return BuiltinFunctionsCore.Min.GetUniqueTexlRuntimeName(isPrefetching);
            }
            else
            {
                return BuiltinFunctionsCore.Max.GetUniqueTexlRuntimeName(isPrefetching);
            }
        }

        public MinMaxFunction_DT(bool isMin)
            : base(isMin ? "Min" : "Max", isMin ? TexlStrings.AboutMin : TexlStrings.AboutMax, FunctionCategories.MathAndStat, DType.DateTime, 0, 1, int.MaxValue, DType.DateTime)
        {
            _isMin = isMin;
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.StatisticalArg };
            yield return new[] { TexlStrings.StatisticalArg, TexlStrings.StatisticalArg };
            yield return new[] { TexlStrings.StatisticalArg, TexlStrings.StatisticalArg, TexlStrings.StatisticalArg };
        }
    }
}
