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
    // GetProperties(record:O): *[Name:s,Value:O]
    internal class GetPropertiesFunction_UO : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        internal const string NameColumnName = "Name";
        internal const string ValueColumnName = "Value";

        private static readonly DType _returnType =
            DType.CreateTable(
                new TypedName(DType.String, new DName(NameColumnName)),
                new TypedName(DType.UntypedObject, new DName(ValueColumnName)));

        public GetPropertiesFunction_UO()
            : base("GetProperties", TexlStrings.AboutGetProperties, FunctionCategories.Information, _returnType, 0, 1, 1, DType.UntypedObject)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.GetPropertiesArg1 };
        }
    }
}
