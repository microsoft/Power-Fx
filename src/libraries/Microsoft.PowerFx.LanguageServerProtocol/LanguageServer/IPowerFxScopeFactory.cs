// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Intellisense;

namespace Microsoft.PowerFx.Core
{
    public interface IPowerFxScopeFactory
    {
        IPowerFxScope GetOrCreateInstance(string documentUri);
    }
}
