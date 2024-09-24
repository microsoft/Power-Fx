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
    internal class AsTypeFunction_UOImpl : AsTypeFunction_UO, IAsyncTexlFunction4
    {
        public async Task<FormulaValue> InvokeAsync(TimeZoneInfo timezoneInfo, FormulaType ft, FormulaValue[] args, Action checkCancellation)
        {
            Contracts.Assert(args.Length == 2);
            checkCancellation();

            var irContext = IRContext.NotInSource(ft);
            var typeString = (StringValue)args[1];

            try
            {
                return JSONFunctionUtils.ConvertUntypedObjectToFormulaValue(irContext, args[0], typeString, timezoneInfo);
            }
            catch (Exception e)
            {
                return new ErrorValue(irContext, new ExpressionError()
                {
                    Message = $"{e.GetType().Name} {e.Message}",
                    Span = irContext.SourceContext,
                    Kind = ErrorKind.InvalidArgument
                });
            }
        }
    }
}
