// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal class JoinFunction : FilterFunctionBase
    {
        public JoinFunction()
            : base("Join", TexlStrings.AboutJoin, FunctionCategories.Table, DType.EmptyTable, -2, 2, int.MaxValue, DType.EmptyTable, DType.EmptyTable)
        {
            ScopeInfo = new FunctionScopeInfo(this, acceptsLiteralPredicates: false);
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            throw new NotImplementedException();
        }
    }
}
