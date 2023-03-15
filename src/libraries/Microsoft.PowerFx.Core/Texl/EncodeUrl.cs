// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl
{
    internal sealed class EncodeUrlFunction : StringOneArgFunction
    {
    public EncodeUrlFunction()
            : base("EncodeUrl", TexlStrings.AboutEncodeUrl, FunctionCategories.Text)
        { 
        }
    }
}
