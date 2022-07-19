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
    // Concat(source:*[...], expr, [separator:s])
    // This is an aggregate function that folds over the specified table (first arg) with respect to
    // the '&' operator and the specified row projection (2nd arg).
    internal sealed class ConcatFunction : FunctionWithTableInput
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public ConcatFunction()
            : base("Concat", TexlStrings.AboutConcat, FunctionCategories.Table, DType.String, 0x02, 0, 2, 3, DType.EmptyTable, DType.String)
        {
            ScopeInfo = new FunctionScopeInfo(this, usesAllFieldsInScope: false);
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.ConcatArg1, TexlStrings.ConcatArg2 };
            yield return new[] { TexlStrings.ConcatArg1, TexlStrings.ConcatArg2, TexlStrings.ConcatArg3 };
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
