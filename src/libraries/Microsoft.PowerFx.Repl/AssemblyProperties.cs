// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Repl
{
    internal static class AssemblyRegistration
    {
        internal static bool IsRegistered = IsRegistered || AssemblyRegistrar.Register(typeof(AssemblyRegistration).Assembly);
    }
}
