// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.Core.Functions.Delegation
{
    internal interface ICustomDelegationFunction
    {
        // This exists to push a feature gate dependence out of PowerFx.
        // Once AllowUserDelegation is cleaned up, this can be removed
        bool IsUserCallNodeDelegable();
    }
}
