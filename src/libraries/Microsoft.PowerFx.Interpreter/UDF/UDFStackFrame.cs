// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter
{
    [DebuggerDisplay("{_func.Name}")]
    internal class UDFStackFrame
    {
        private readonly FormulaValue[] _args;

        // For debug purposes.
        private readonly UserDefinedFunction _func;

        public FormulaValue GetArg(UDFParameterInfo arg) => _args[arg.ArgIndex];

        public UDFStackFrame(UserDefinedFunction func, FormulaValue[] args)
        {
            _func = func;
            _args = args;
        }
        
        private UDFStackFrame()
        {
        }
    }
}
