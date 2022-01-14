// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.PowerFx.Core.Localization
{
    internal interface ILanguageSettings : INamedLanguageSettings
    {
        // Empty maps are equivalent to identity maps.
        // This is relevant for Beta and Beta2 documents (which are always invariant).
        Dictionary<string, string> LocToInvariantFunctionMap { get; }

        Dictionary<string, string> LocToInvariantPunctuatorMap { get; }

        // Reverse maps
        Dictionary<string, string> InvariantToLocFunctionMap { get; }

        Dictionary<string, string> InvariantToLocPunctuatorMap { get; }

        ILanguageSettings GetIdentitySettingsForInvariantLanguage();
    }
}
