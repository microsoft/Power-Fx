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

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal class ParseJSONFunctionImpl : ParseJSONFunction, IAsyncTexlFunction
    {
        public async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
        {
            var irContext = IRContext.NotInSource(FormulaType.UntypedObject);
            var arg = args[0];

            if (arg is BlankValue || arg is ErrorValue)
            {
                return arg;
            }
            else if (arg is not StringValue)
            {
                return new ErrorValue(irContext, new ExpressionError()
                {
                    Message = "Runtime type mismatch",
                    Span = irContext.SourceContext,
                    Kind = ErrorKind.InvalidArgument
                });
            }

            var json = ((StringValue)arg).Value;
            JsonElement result;
            try
            {
                using (var document = JsonDocument.Parse(json))
                {
                    // Clone must be used here because the original element will be disposed
                    result = document.RootElement.Clone();
                }

                // Map null to blank
                if (result.ValueKind == JsonValueKind.Null)
                {
                    return new BlankValue(IRContext.NotInSource(FormulaType.Blank));
                }

                return new UntypedObjectValue(irContext, new JsonUntypedObject(result));
            }
            catch (JsonException ex)
            {
                return new ErrorValue(irContext, new ExpressionError()
                {
                    Message = $"The Json could not be parsed: {ex.Message}",
                    Span = irContext.SourceContext,
                    Kind = ErrorKind.InvalidArgument
                });
            }
        }
    }
}
