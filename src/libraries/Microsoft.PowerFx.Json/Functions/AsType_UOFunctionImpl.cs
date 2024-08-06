// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal class AsType_UOFunctionImpl : AsType_UOFunction, IAsyncTexlFunction
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

            var settings = new FormulaValueJsonSerializerSettings { AllowUnknownRecordFields = false };
            var fv = FormulaValueJSON.FromJson(jsElement, settings, FormulaType.Build(dtype));
            return fv;
        }
    }
}
