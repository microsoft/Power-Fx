// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal class LanguageFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool HasPreciseErrors => true;

        public override bool SupportsParamCoercion => true;

        public LanguageFunction()
            : base("Language", TexlStrings.AboutLanguage, FunctionCategories.Information, DType.String, 0, 0, 0)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            return EnumerableUtils.Yield<TexlStrings.StringGetter[]>();
        }
    }
}
