// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
        public static FormulaValue Not(IRContext irContext, BooleanValue[] args)
        {
            return new BooleanValue(irContext, !args[0].Value);
        }

        // Lazy evaluation 
        public static async ValueTask<FormulaValue> And(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
        {
            foreach (var arg in args)
            {
                var res = await runner.EvalArgAsync<BooleanValue>(arg, symbolContext, arg.IRContext).ConfigureAwait(false);

                if (res.IsValue)
                {
                    var val = res.Value;
                    if (!val.Value)
                    {
                        return new BooleanValue(irContext, false);
                    }
                }
                else
                {
                    return res.ToFormulaValue();
                }
            }

            return new BooleanValue(irContext, true);
        }

        // Lazy evaluation 
        public static async ValueTask<FormulaValue> Or(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
        {
            foreach (var arg in args)
            {
                runner.CheckCancel();

                var res = await runner.EvalArgAsync<BooleanValue>(arg, symbolContext, arg.IRContext).ConfigureAwait(false);

                if (res.IsValue)
                {
                    var val = res.Value;
                    if (val.Value)
                    {
                        return new BooleanValue(irContext, true);
                    }
                }
                else if (res.IsError)
                {
                    return res.Error;
                }
            }

            return new BooleanValue(irContext, false);
        }
    }
}
