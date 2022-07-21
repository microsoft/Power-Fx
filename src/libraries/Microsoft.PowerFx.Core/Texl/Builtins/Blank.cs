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
    // Blank()
    internal sealed class BlankFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public BlankFunction()
            : base(
            "Blank",
            TexlStrings.AboutBlank,
            FunctionCategories.Text,
            DType.ObjNull, // return type
            0, // no lambdas            
            0, // min arity of 0
            0) // max arity of 0
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new TexlStrings.StringGetter[] { };
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
