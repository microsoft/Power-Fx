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
            var ret = FormulaType.GetFromStringOrNull(token.ToString()) ?? FormulaType.Unknown;
            if (ret == FormulaType.Unknown)
            {
                foreach (var uDT in uDTs) 
                { 
                    if (uDT.Ident.ToString() == token.ToString())
                    {
                        return new KnownRecordType(uDT.Type);
                    }
                    else
                    {
                        var a = uDT.Ident.ToString();
                        var b = token.ToString();
                        throw new Exception($"HI, a: {a}, b: {b}");
                    }
                }
            }

            return ret;
        }
    }
}
