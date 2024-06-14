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
    internal class AsTypeFunctionImpl : AsTypeUOFunction, IAsyncTexlFunction
    {
        public async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
        {
            var arg0 = (UntypedObjectValue)args[0];
            var arg1 = (StringValue)args[1];

            if (DType.TryParse(arg1.Value, out DType dtype))
            {
                var uo = arg0.Impl;
                var jsElement = ((JsonUntypedObject)uo)._element;
                var fv = FormulaValueJSON.FromJson(jsElement, FormulaType.Build(dtype));
                return fv;
            }

            return FormulaValue.NewError(new ExpressionError());
        }
    }
}
