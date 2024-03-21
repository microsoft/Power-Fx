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

            if (nameResolver.Lookup(token.Name, out var info) && info.Data is FormulaType ft)
            {
                formulaType = ft;
            }
            else if (PrimitiveTypesSymbolTable.Instance.TryLookup(token.Name, out var info2) && info2.Data is FormulaType ft2)
            {
                formulaType = ft2;
            }

            return formulaType;
        }
    }
}
