// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Infinity()
    // Returns the IEEE 754 double-precision positive infinity value.
    // Negative infinity is obtained by applying unary minus: -Infinity().
    internal sealed class InfinityFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool IsAllowedInSimpleExpressions => true;

        public InfinityFunction()
            : base(
                "Infinity",
                TexlStrings.AboutInfinity,
                FunctionCategories.MathAndStat,
                DType.Number, // return type
                0,            // no lambdas
                0,            // min arity of 0
                0) // max arity of 0
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new TexlStrings.StringGetter[] { };
        }
    }

    // MaxValue()
    // Returns the largest finite value representable as a Number (double.MaxValue).
    internal sealed class MaxValueFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool IsAllowedInSimpleExpressions => true;

        public MaxValueFunction()
            : base(
                "MaxValue",
                TexlStrings.AboutMaxValue,
                FunctionCategories.MathAndStat,
                DType.Number, // return type
                0,            // no lambdas
                0,            // min arity of 0
                0) // max arity of 0
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new TexlStrings.StringGetter[] { };
        }
    }

    // MinValue()
    // Returns the most-negative finite value representable as a Number (double.MinValue).
    internal sealed class MinValueFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool IsAllowedInSimpleExpressions => true;

        public MinValueFunction()
            : base(
                "MinValue",
                TexlStrings.AboutMinValue,
                FunctionCategories.MathAndStat,
                DType.Number, // return type
                0,            // no lambdas
                0,            // min arity of 0
                0) // max arity of 0
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new TexlStrings.StringGetter[] { };
        }
    }
}
