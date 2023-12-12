// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // GetPropertyNames(record:O): *[Value:s]
    internal class GetPropertyNamesFunction_UO : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        private static readonly DType _returnType =
            DType.CreateTable(new TypedName(DType.String, new DName(ColumnName_Value)));

        public GetPropertyNamesFunction_UO()
            : base("GetPropertyNames", TexlStrings.AboutGetPropertyNames, FunctionCategories.Information, _returnType, 0, 1, 1, DType.UntypedObject)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.GetPropertyNamesArg1 };
        }
    }

    // GetPropertyValue(record:O, property_name:s): O
    internal class GetPropertyValueFunction_UO : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        private static readonly DType _returnType =
            DType.CreateTable(new TypedName(DType.String, new DName(ColumnName_Value)));

        public GetPropertyValueFunction_UO()
            : base("GetPropertyValue", TexlStrings.AboutGetPropertyValue, FunctionCategories.Information, DType.UntypedObject, 0, 2, 2, DType.UntypedObject, DType.String)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.GetPropertyValueArg1, TexlStrings.GetPropertyValueArg2 };
        }
    }
}
