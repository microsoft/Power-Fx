// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Intellisense
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
