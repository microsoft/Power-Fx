// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal class TypedParseJSONFunctionImpl : TypedParseJSONFunction, IAsyncTexlFunction4
    {
        public async Task<FormulaValue> InvokeAsync(TimeZoneInfo timezoneInfo, FormulaType ft, FormulaValue[] args, Action checkCancellation)
        {
            Contracts.Assert(args.Length == 2);
            checkCancellation();

            var irContext = IRContext.NotInSource(ft);
            var typeString = (StringValue)args[1];

            return JSONFunctionUtils.ConvertJSONStringToFormulaValue(irContext, args[0], typeString, timezoneInfo);
        }
    }
}
