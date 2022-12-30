// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.DLP;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Syntax;
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
            var clearFunction = new ClearFunction();

            var cleared = await clearFunction.InvokeAsync(args, cancellationToken);

            if (cleared is ErrorValue)
            {
                return cleared;
            }

            return await base.InvokeAsync(args, cancellationToken);
        }
    }
}
