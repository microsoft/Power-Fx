// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // ColorValue(colorstring:s)
    // Returns the color value for the given color string
    internal sealed class ColorValueFunction : BuiltinFunction
    {
        public const string ColorValueFunctionInvariantName = "ColorValue";

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public ColorValueFunction()
            : base(ColorValueFunctionInvariantName, TexlStrings.AboutColorValue, FunctionCategories.Color, DType.Color, 0, 1, 1, DType.String)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.ColorValueArg1 };
        }
    }

    // ColorValue(colorstring:uo)
    // ColorValue UntypedObject override
    internal sealed class ColorValueFunction_UO : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public ColorValueFunction_UO()
            : base(ColorValueFunction.ColorValueFunctionInvariantName, TexlStrings.AboutColorValue, FunctionCategories.Color, DType.Color, 0, 1, 1, DType.UntypedObject)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.ColorValueArg1 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "_UO");
        }
    }
}
