// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.App.Controls
{
    internal interface IExternalRuleScopeResolver
    {
        bool Lookup(DName identName, out ScopedNameLookupInfo scopedNameLookupInfo);
    }
}