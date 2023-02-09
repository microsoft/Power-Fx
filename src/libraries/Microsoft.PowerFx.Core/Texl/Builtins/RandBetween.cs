﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // RandBetween()
    // Equivalent DAX/Excel function: RandBetween
    internal sealed class RandBetweenFunction : BuiltinFunction
    {
        public override ArgPreprocessor GetArgPreprocessor(int index)
        {
            return base.GetGenericArgPreprocessor(index);
        }

        // Multiple invocations may produce different return values.
        public override bool IsStateless => false;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public RandBetweenFunction()
            : base(
                "RandBetween",
                TexlStrings.AboutRandBetween,
                FunctionCategories.MathAndStat,
                returnType: DType.Number,
                maskLambdas: 0,
                arityMin: 2,
                arityMax: 2,
                DType.Number,
                DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.RandBetweenArg1, TexlStrings.RandBetweenArg2 };
        }
    }
}
