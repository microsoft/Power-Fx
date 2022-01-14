// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
        public static FormulaValue Not(IRContext irContext, BooleanValue[] args)
        {
            return new BooleanValue(irContext, !args[0].Value);
        }

        // Lazy evaluation 
        public static FormulaValue And(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
        {
            foreach (var arg in args)
            {
                var res = runner.EvalArg<BooleanValue>(arg, symbolContext, arg.IRContext);

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
        public static FormulaValue Or(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
        {
            foreach (var arg in args)
            {
                var res = runner.EvalArg<BooleanValue>(arg, symbolContext, arg.IRContext);

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
