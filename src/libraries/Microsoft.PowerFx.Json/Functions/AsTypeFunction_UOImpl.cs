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
    internal class AsTypeFunction_UOImpl : AsTypeFunction_UO, IAsyncTexlFunction
    {
        public async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
        {
            Contracts.Assert(args.Length == 2);

            var irContext = IRContext.NotInSource(FormulaType.UntypedObject);
            var typeString = (StringValue)args[1];

            try
            {
                return JSONFunctionUtils.ConvertUnTypedObjectToFormulaValue(irContext, args[0], typeString);
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
