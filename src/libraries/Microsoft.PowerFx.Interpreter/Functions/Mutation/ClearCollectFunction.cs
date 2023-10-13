// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx.Functions
{
    /// <summary>
    /// The ClearCollect function is a combination of Clear + Collect.
    /// </summary>
    internal class ClearCollectFunction : CollectFunction
    {
        public override bool IsSelfContained => false;

        public ClearCollectFunction()
            : base("ClearCollect", AboutClearCollect)
        {
        }

        public override IEnumerable<StringGetter[]> GetSignatures()
        {
            yield return new[] { ClearCollectDataSourceArg, ClearCollectRecordArg };
        }
    }

    internal class ClearCollectFunctionImpl : CollectFunctionImpl
    {
        private readonly ClearFunctionImpl _clearFunction;

        public ClearCollectFunctionImpl(ClearFunctionImpl clearFunction)            
        {
            _clearFunction = clearFunction;
        }

        public override async Task<FormulaValue> InvokeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            FormulaValue[] args = serviceProvider.GetService<FunctionExecutionContext>().Arguments;
            if (args[0] is LambdaFormulaValue arg0lazy)
            {
                args[0] = await arg0lazy.EvalAsync().ConfigureAwait(false);
            }            

            var cleared = await _clearFunction.InvokeAsync(args, cancellationToken).ConfigureAwait(false);

            if (cleared is ErrorValue)
            {
                return cleared;
            }

            return await base.InvokeAsync(args, cancellationToken).ConfigureAwait(false);
        }
    }
}
