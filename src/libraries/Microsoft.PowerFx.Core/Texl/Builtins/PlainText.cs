// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl
{
    internal class PlainTextFunction : StringOneArgFunction
    {
        public override bool IsSelfContained => true;

        public PlainTextFunction()
            : base("PlainText", TexlStrings.AboutPlainText, FunctionCategories.Text)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.PlainTextArg1 };
        }
    }
}
