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
    internal class ParseJSONFunctionImpl : ParseJSONFunction, IAsyncTexlFunctionService
    {
        public async Task<FormulaValue> InvokeAsync(IServiceProvider runtimeServiceProvider, IRContext irContext, FormulaValue[] args, CancellationToken cancellationToken)
        {
            var irContextUO = IRContext.NotInSource(FormulaType.UntypedObject);
            var arg = args[0];

            if (arg is BlankValue || arg is ErrorValue)
            {
                return arg;
            }
            else if (arg is not StringValue)
            {
                return new ErrorValue(irContextUO, new ExpressionError()
                {
                    Message = "Runtime type mismatch",
                    Span = irContextUO.SourceContext,
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

                return new UntypedObjectValue(irContextUO, new JsonUntypedObject(result));
            }
            catch (JsonException ex)
            {
                return new ErrorValue(irContextUO, new ExpressionError()
                {
                    Message = $"The Json could not be parsed: {ex.Message}",
                    Span = irContextUO.SourceContext,
                    Kind = ErrorKind.InvalidJSON
                });
            }
        }
    }
}
