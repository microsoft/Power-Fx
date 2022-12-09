// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Utils;
using static Microsoft.PowerFx.Functions.Library;

namespace Microsoft.PowerFx.Core.Functions
{
    // Associates a TexlFunction with its corresponding AsyncFunctionPtr
    internal class TexlFunctionImplementation : ITexlFunction
    {
        public string Name => function.Name;

        public DPath Namespace => function.Namespace;

        public string LocaleInvariantName => function.LocaleInvariantName;

        internal TexlFunction function;
        internal AsyncFunctionPtr functionPtr;

        public TexlFunctionImplementation(TexlFunction func, AsyncFunctionPtr funcptr)
        {
            function = func;
            functionPtr = funcptr;
        }

        public TexlFunction ToTexlFunctions()
        {
            return function;
        }
    }
}
