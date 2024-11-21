// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal class TypedParseJSONFunctionImpl : TypedParseJSONFunction, IAsyncTexlFunctionService
    {
        public async Task<FormulaValue> InvokeAsync(IServiceProvider runtimeServiceProvider, IRContext irContext, FormulaValue[] args, CancellationToken cancellationToken)
        {
            Contracts.Assert(args.Length == 2);
            TimeZoneInfo timezoneInfo = runtimeServiceProvider.GetService(typeof(TimeZoneInfo)) as TimeZoneInfo ?? throw new InvalidOperationException("TimeZoneInfo is required");
            cancellationToken.ThrowIfCancellationRequested();

            var typeString = (StringValue)args[1];

            return JSONFunctionUtils.ConvertJSONStringToFormulaValue(irContext, args[0], typeString, timezoneInfo);
        }
    }
}
