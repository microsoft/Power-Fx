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
    internal class ParseJSONWithTypeImpl : ParseJSONWithType, IAsyncTexlFunction
    {
        public async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
        {
            var arg0 = (StringValue)args[0];
            var arg1 = (StringValue)args[1];

            var json = arg0.Value;
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

                if (DType.TryParse(arg1.Value, out DType dtype))
                {
                    var fv = FormulaValueJSON.FromJson(result, FormulaType.Build(dtype));
                    return fv;
                }

                return FormulaValue.NewError(new ExpressionError());
            }
            catch (JsonException ex)
            {
                return FormulaValue.NewError(new ExpressionError());
            }
        }
    }
}
