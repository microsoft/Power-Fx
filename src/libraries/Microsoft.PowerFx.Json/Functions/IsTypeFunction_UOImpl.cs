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
    internal class IsTypeFunction_UOImpl : IsTypeFunction_UO, IAsyncTexlFunctionService
    {
        public async Task<FormulaValue> InvokeAsync(IServiceProvider runtimeServiceProvider, IRContext irContext, FormulaValue[] args, CancellationToken cancellationToken)
        {
            Contracts.Assert(args.Length == 2);
            cancellationToken.ThrowIfCancellationRequested();

            TimeZoneInfo timezoneInfo = runtimeServiceProvider.GetService(typeof(TimeZoneInfo)) as TimeZoneInfo ?? throw new InvalidOperationException("TimeZoneInfo is required");
            var typeString = (StringValue)args[1];

            try 
            {
                var fv = JSONFunctionUtils.ConvertUntypedObjectToFormulaValue(irContext, args[0], typeString, timezoneInfo);
                if (fv is BlankValue || fv is ErrorValue)
                {
                    return fv;
                }
                else
                {
                    return BooleanValue.New(true);
                }
            }
            catch
            {
                return BooleanValue.New(false);
            }
        }
    }
}
