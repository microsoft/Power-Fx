// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Entities
{
    // Used to identify Table or Record values which are refreshable.
    // Refresh() function will only work if this interface is implemented.
    public interface IRefreshable
    {
        void Refresh();
    }
}
