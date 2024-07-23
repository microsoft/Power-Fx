// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
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
using static Microsoft.PowerFx.Syntax.PrettyPrintVisitor;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal class TypedParseJSONFunctionImpl : TypedParseJSONFunction, IAsyncTexlFunction
    {
        public async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
        {
            var irContext = IRContext.NotInSource(FormulaType.UntypedObject);
            var arg0 = args[0];

            if (arg0 is BlankValue || arg0 is ErrorValue)
            {
                return arg0;
            }
            else if (arg0 is not StringValue)
            {
                return new ErrorValue(irContext, new ExpressionError()
                {
                    Message = "Runtime type mismatch",
                    Span = irContext.SourceContext,
                    Kind = ErrorKind.InvalidArgument
                });
            }

            var typeString = (StringValue)args[1];
            var json = ((StringValue)arg0).Value;

            if (!DType.TryParse(typeString.Value, out DType dtype))
            {
                return new ErrorValue(irContext, new ExpressionError()
                {
                    Message = $"Internal error: Unable to parse type argument",
                    Span = irContext.SourceContext,
                    Kind = ErrorKind.Internal
                });
            }

            var fv = FormulaValueJSON.FromJson(json, FormulaType.Build(dtype));
            return fv;
        }
    }
}
