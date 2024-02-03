// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl
{
    internal sealed class EscapeHtmlFunction : StringOneArgFunction
    {
    public EscapeHtmlFunction()
            : base("EscapeHtml", TexlStrings.AboutEscapeHtml, FunctionCategories.Text)
        { 
        }

    public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.EscapeHtmlArg1 };
        }
    }
}
