// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx
{
    // Backwards compat shim 
    [Obsolete("Use UserInfo")]
    public interface IUserInfo
    {
        string FullName { get; }

        string Email { get; }

        string Id { get; }
    }
}
