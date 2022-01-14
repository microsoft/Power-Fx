// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.Core.Public
{
    /// <summary>
    /// Provide display name translation.
    /// </summary>
    public interface IPowerFxScopeDisplayName
    {
        /// <summary>
        /// Translate entity logical name to display name.
        /// </summary>
        string TranslateToDisplayName(string expression);
    }
}
