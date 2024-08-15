// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal class IsTypeFunction_UOImpl : IsTypeFunction_UO, IAsyncTexlFunction
    {
        public async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
        {
            Contracts.Assert(args.Length == 2);

            var irContext = IRContext.NotInSource(FormulaType.UntypedObject);
            var typeString = (StringValue)args[1];

            try 
            {
                var fv = JSONFunctionUtils.ConvertUnTypedObjectToFormulaValue(irContext, args[0], typeString);
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
