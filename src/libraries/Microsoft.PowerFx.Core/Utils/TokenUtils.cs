// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Utils
{
    internal static class TokenUtils
    {
        internal static FormulaType GetFormulaType(this IdentToken token)
        {
            var formulaType = FormulaType.Unknown;
            if (PrimitiveTypesSymbolTable.Instance.TryLookup(token.Name, out var info) && info.Data is FormulaType ft)
            {
                formulaType = ft;
            }

            return formulaType;
        }

        internal static FormulaType GetFormulaType(this IdentToken token, INameResolver nameResolver)
        {
            var formulaType = FormulaType.Unknown;

            if (nameResolver.LookupType(token.Name, out var ct))
            {
                formulaType = ct;
            }
            else if (PrimitiveTypesSymbolTable.Instance.TryLookup(token.Name, out var ptInfo) && ptInfo.Data is FormulaType pt)
            {
                formulaType = pt;
            }

            return formulaType;
        }
    }
}
