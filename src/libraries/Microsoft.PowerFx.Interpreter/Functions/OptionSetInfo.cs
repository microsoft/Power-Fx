// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Interpreter.Localization;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter
{
    internal sealed class OptionSetInfoFunction : TexlFunction, IAsyncTexlFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public OptionSetInfoFunction()
            : base(DPath.Root, "OptionSetInfo", "OptionSetInfo", TexlStrings.AboutOptionSetInfo, FunctionCategories.Text, DType.String, 0, 1, 1, DType.OptionSetValue)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.AboutOptionSetInfoArg1 };
        }

        public async Task<FormulaValue> InvokeAsync(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            runner.CheckCancel();

            switch (args[0])
            {
                case ErrorValue errorValue:
                    return errorValue;
                case BlankValue:
                    return new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty);
                case OptionSetValue osv:
                    return new StringValue(IRContext.NotInSource(FormulaType.String), osv.Option);
            }

            return CommonErrors.InvalidArgumentError(args[0].IRContext, RuntimeStringResources.ErrInvalidArgument);
        }
    }
}
