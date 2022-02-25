// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // IsEmpty(expression:E)
    internal sealed class IsEmptyFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool HasPreciseErrors => true;

        public override bool SupportsParamCoercion => false;

        public IsEmptyFunction()
            : base("IsEmpty", TexlStrings.AboutIsEmpty, FunctionCategories.Table | FunctionCategories.Information, DType.Boolean, 0, 1, 1)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.IsEmptyArg1 };
        }
    }
}
