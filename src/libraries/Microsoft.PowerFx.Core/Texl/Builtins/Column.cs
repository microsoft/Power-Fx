// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Column(record:O, property_name:s): O
    internal class ColumnFunction_UO : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override ArgPreprocessor GetArgPreprocessor(int index, int argCount)
        {
            if (index == 1)
            {
                // Replace blank with empty string
                return base.GetGenericArgPreprocessor(index);
            }

            return base.GetArgPreprocessor(index, argCount);
        }

        private static readonly DType _returnType =
            DType.CreateTable(new TypedName(DType.String, new DName(ColumnName_Value)));

        public ColumnFunction_UO()
            : base("Column", TexlStrings.AboutColumn, FunctionCategories.Information, DType.UntypedObject, 0, 2, 2, DType.UntypedObject, DType.String)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.ColumnArg1, TexlStrings.ColumnArg2 };
        }
    }
}
