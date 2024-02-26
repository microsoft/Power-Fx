// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // ColumnNames(record:O): *[Value:s]
    internal class ColumnNamesFunction_UO : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        private static readonly DType _returnType =
            DType.CreateTable(new TypedName(DType.String, new DName(ColumnName_Value)));

        public ColumnNamesFunction_UO()
            : base("ColumnNames", TexlStrings.AboutColumnNames, FunctionCategories.Information, _returnType, 0, 1, 1, DType.UntypedObject)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.ColumnNamesArg1 };
        }
    }
}
