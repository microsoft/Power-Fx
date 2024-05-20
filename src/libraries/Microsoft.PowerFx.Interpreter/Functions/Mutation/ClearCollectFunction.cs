// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx.Functions
{
    /// <summary>
    /// The ClearCollect function is a combination of Clear + Collect.
    /// </summary>
    internal class ClearCollectFunction : CollectFunction, IAsyncTexlFunction
    {
        public override bool IsSelfContained => false;

        public ClearCollectFunction()
            : base("ClearCollect", TexlStrings.AboutClearCollect)
        {
        }

        public override IEnumerable<StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.ClearCollectDataSourceArg, TexlStrings.ClearCollectRecordArg };
        }

        public override async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
        {
            if (args[0] is LambdaFormulaValue arg0lazy)
            {
                args[0] = await arg0lazy.EvalAsync().ConfigureAwait(false);
            }

            var clearFunction = new ClearImpl();

            var cleared = await clearFunction.InvokeAsync(FormulaType.Void, args, cancellationToken).ConfigureAwait(false);

            if (cleared is ErrorValue)
            {
                return cleared;
            }

            return await base.InvokeAsync(args, cancellationToken).ConfigureAwait(false);
        }
    }
}
