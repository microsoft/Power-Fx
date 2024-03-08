// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl
{
    internal sealed class EncodeHTMLFunction : StringOneArgFunction
    {
        public EncodeHTMLFunction()
                : base("EncodeHTML", TexlStrings.AboutEncodeHTML, FunctionCategories.Text)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.EncodeHTMLArg1 };
        }
    }
}
