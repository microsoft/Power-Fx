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
    internal class IsTypeFunction_UOImpl : IsTypeFunction_UO, IAsyncTexlFunction4
    {
        public async Task<FormulaValue> InvokeAsync(TimeZoneInfo timezoneInfo, FormulaType ft, FormulaValue[] args, CancellationToken cancellationToken)
        {
            Contracts.Assert(args.Length == 2);
            cancellationToken.ThrowIfCancellationRequested();

            var irContext = IRContext.NotInSource(FormulaType.UntypedObject);
            var typeString = (StringValue)args[1];

            if (args[0] is ErrorValue)
            {
                return args[0];
            }

            if (args[0] is BlankValue)
            {
                // Blank is not of the given type
                return BooleanValue.New(false);
            }

            try
            {
                var fv = JSONFunctionUtils.ConvertUntypedObjectToFormulaValue(irContext, args[0], typeString, timezoneInfo);

                if (fv is ErrorValue)
                {
                    return BooleanValue.New(false);
                }

                if (fv is BlankValue)
                {
                    return fv;
                }

                return BooleanValue.New(true);
            }
            catch
            {
                return BooleanValue.New(false);
            }
        }
    }
}
