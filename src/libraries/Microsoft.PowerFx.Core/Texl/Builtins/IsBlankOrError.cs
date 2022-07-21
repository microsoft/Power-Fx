// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // IsBlankOrError(expression:E)
    internal sealed class IsBlankOrErrorFunction : IsBlankFunctionBase
    {
        public const string IsBlankOrErrorInvariantFunctionName = "IsBlankOrError";

        public IsBlankOrErrorFunction()
            : base(IsBlankOrErrorInvariantFunctionName, TexlStrings.AboutIsBlankOrError, FunctionCategories.Table | FunctionCategories.Information, DType.Boolean, 0, 1, 1)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.IsBlankOrErrorArg1 };
        }
    }

    // IsBlankOrError(expression:E)
    // Equivalent Excel and DAX function: IsBlank
    internal sealed class IsBlankOrErrorOptionSetValueFunction : BuiltinFunction
    {
        public override bool SupportsParamCoercion => true;

        public override bool IsSelfContained => true;

        public IsBlankOrErrorOptionSetValueFunction()
            : base(IsBlankOrErrorFunction.IsBlankOrErrorInvariantFunctionName, TexlStrings.AboutIsBlankOrError, FunctionCategories.Table | FunctionCategories.Information, DType.Boolean, 0, 1, 1, DType.OptionSetValue)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.IsBlankOrErrorArg1 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "OptionSetValue");
        }
    }
}
