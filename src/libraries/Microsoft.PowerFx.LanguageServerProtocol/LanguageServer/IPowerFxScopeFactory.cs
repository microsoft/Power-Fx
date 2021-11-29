// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Public;

namespace Microsoft.PowerFx.Core
{
    public interface IPowerFxScopeFactory
    {
        IPowerFxScope GetOrCreateInstance(string documentUri);
    }
}
