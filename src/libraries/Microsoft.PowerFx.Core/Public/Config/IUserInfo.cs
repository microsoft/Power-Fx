// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx
{
    public interface IUserInfo
    {
        string FullName { get; }

        string Email { get; }
    }
}
