// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.Core.Localization
{
    /// <summary>
    ///     A language settings abstraction tied to a specific culture name.
    /// </summary>
    internal interface INamedLanguageSettings
    {
        string CultureName { get; }

        string UICultureName { get; }
    }
}