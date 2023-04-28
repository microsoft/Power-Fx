// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal class PlainTextFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public PlainTextFunction()
            : base("PlainText", TexlStrings.AboutPlainText, FunctionCategories.Text, DType.String, 0, 1, 1, DType.String)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.PlainTextArg1 };
        }
    }
}
