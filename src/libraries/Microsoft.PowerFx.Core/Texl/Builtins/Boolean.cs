// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Boolean(arg:s)
    // Corresponding Excel and DAX function: Boolean
    internal sealed class BooleanFunction : BuiltinFunction
    {
        public const string BooleanInvariantFunctionName = "Boolean";

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public BooleanFunction()
            : base(BooleanInvariantFunctionName, TexlStrings.AboutBoolean, FunctionCategories.Text, DType.Boolean, 0, 1, 1, DType.String)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.BooleanArg1 };
        }
    }

    // Boolean(arg:O)
    internal sealed class BooleanFunction_UO : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public BooleanFunction_UO()
            : base(BooleanFunction.BooleanInvariantFunctionName, TexlStrings.AboutBoolean, FunctionCategories.Text, DType.Boolean, 0, 1, 1, DType.UntypedObject)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.BooleanArg1 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "_UO");
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
