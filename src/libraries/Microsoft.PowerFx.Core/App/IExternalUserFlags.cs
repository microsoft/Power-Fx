// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.App
{
    /// <summary>
    /// All hosts should use <see cref="DefaultUserFlags"/> except for Canvas Apps (legacy flag support)
    /// This interface and the flags in it should be deprecated once the below flags are removed from Canvas Apps
    /// DO NOT add flags to this clas without very strong justification. We do not want to allow PowerFx
    /// behavior to be different between target platforms. 
    /// </summary>
    internal interface IExternalUserFlags
    {
        bool EnforceSelectPropagationLimit { get; }
    }

    internal sealed class DefaultUserFlags : IExternalUserFlags
    {
        public bool EnforceSelectPropagationLimit => true;
    }
}
