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
    internal class IsTypeFunction_UOImpl : IsTypeFunction_UO, IAsyncTexlFunction4
    {
        public async Task<FormulaValue> InvokeAsync(TimeZoneInfo timezoneInfo, FormulaType ft, FormulaValue[] args, Action checkCancellation)
        {
            Contracts.Assert(args.Length == 2);
            checkCancellation();

            var irContext = IRContext.NotInSource(FormulaType.UntypedObject);
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
