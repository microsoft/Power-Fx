// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal class AsTypeUOFunctionImpl : AsTypeUOFunction, IAsyncTexlFunction
    {
        public async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
        {
            var irContext = IRContext.NotInSource(FormulaType.UntypedObject);
            var arg0 = args[0];

            if (arg0 is BlankValue || arg0 is ErrorValue)
            {
                return arg0;
            }
            else if (arg0 is not UntypedObjectValue)
            {
                return new ErrorValue(irContext, new ExpressionError()
                {
                    Message = "Runtime type mismatch",
                    Span = irContext.SourceContext,
                    Kind = ErrorKind.InvalidArgument
                });
            }

            var untypedObjectValue = (UntypedObjectValue)arg0;
            var typeString = (StringValue)args[1];

            if (!DType.TryParse(typeString.Value, out DType dtype))
            {
                return new ErrorValue(irContext, new ExpressionError()
                {
                    Message = $"Internal error: Unable to parse type argument",
                    Span = irContext.SourceContext,
                    Kind = ErrorKind.Internal
                });
            }

            var uo = untypedObjectValue.Impl;
            var jsElement = ((JsonUntypedObject)uo)._element;

            try
            {
                var fv = FormulaValueJSON.FromJson(jsElement, FormulaType.Build(dtype));
                return fv;
            }
            catch (JsonException je)
            {
                return new ErrorValue(IRContext.NotInSource(FormulaType.Build(dtype)), new ExpressionError()
                {
                    Message = $"{je.GetType().Name} {je.Message}",
                    Span = irContext.SourceContext,
                    Kind = ErrorKind.InvalidArgument
                });
            }
        }
    }
}
