// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions
{
    internal class TexlFunctionImplementation : ITexlFunction
    {
        public string Name => function.Name;

        public DPath Namespace => function.Namespace;

        public string LocaleInvariantName => function.LocaleInvariantName;

        internal TexlFunction function;
        internal object functionPtr;

        public TexlFunctionImplementation(TexlFunction func, object funcptr)
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
