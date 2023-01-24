// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Utils;
using static Microsoft.PowerFx.Functions.Library;

namespace Microsoft.PowerFx.Interpreter.Functions
{
    internal class TexlFunctionImplementation : ITexlFunction
    {
        public string Name => Function.Name;

        public DPath Namespace => Function.Namespace;

        public string LocaleInvariantName => Function.LocaleInvariantName;

        internal TexlFunction Function;
        internal AsyncFunctionPtr FunctionPtr;

        public TexlFunctionImplementation(TexlFunction func, AsyncFunctionPtr funcptr)
        {
            Function = func;
            FunctionPtr = funcptr;
        }

        public TexlFunction ToTexlFunctions()
        {
            return Function;
        }

        public IEnumerable<string> GetRequiredEnumNames()
        {
            return Function.GetRequiredEnumNames();
        }
    }
}
