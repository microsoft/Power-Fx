// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Utils
{
    internal static class TokenUtils
    {
        internal static FormulaType GetFormulaType(this IdentToken token, IEnumerable<UDT> uDTs)
        {
            return FormulaType.GetFromStringOrNull(token.ToString()) ?? FormulaType.Unknown;
        }
    }
}
