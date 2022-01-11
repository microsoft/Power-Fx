// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Localization
{
    /// <summary>
    /// Language Settings class encapsulates essential details related to a specific
    /// loc + glob environment.
    /// </summary>
    internal sealed class LanguageSettings : ILanguageSettings
    {
        private LanguageSettings _cachedInvariantSettings;
        private int _cacheStamp;

        public string CultureName { get; }
        public string UICultureName { get; }

        // Locale-specific to Invariant maps
        public Dictionary<string, string> LocToInvariantFunctionMap { get; }
        public Dictionary<string, string> LocToInvariantPunctuatorMap { get; }

        // Reverse maps
        public Dictionary<string, string> InvariantToLocFunctionMap { get; }
        public Dictionary<string, string> InvariantToLocPunctuatorMap { get; }

        public void AddFunction(string loc, string invariant)
        {
            Contracts.AssertNonEmpty(loc);
            Contracts.AssertNonEmpty(invariant);

            LocToInvariantFunctionMap[loc] = invariant;
            InvariantToLocFunctionMap[invariant] = loc;

            _cacheStamp++;
        }

        public void AddPunctuator(string loc, string invariant)
        {
            Contracts.AssertNonEmpty(loc);
            Contracts.AssertNonEmpty(invariant);

            LocToInvariantPunctuatorMap[loc] = invariant;
            InvariantToLocPunctuatorMap[invariant] = loc;

            _cacheStamp++;
        }

        public ILanguageSettings GetIdentitySettingsForInvariantLanguage()
        {
            if (_cachedInvariantSettings == null || NeedsRefresh(_cachedInvariantSettings))
            {
                _cachedInvariantSettings = new LanguageSettings("en-US", "en-US")
                {
                    _cacheStamp = _cacheStamp
                };

                foreach (var kvp in LocToInvariantFunctionMap)
                {
                    _cachedInvariantSettings.AddFunction(kvp.Value, kvp.Value);
                }

                foreach (var kvp in LocToInvariantPunctuatorMap)
                {
                    _cachedInvariantSettings.AddPunctuator(kvp.Value, kvp.Value);
                }
            }

            return _cachedInvariantSettings;
        }

        private bool NeedsRefresh(LanguageSettings cache)
        {
            Contracts.AssertValue(cache);

            return cache._cacheStamp != _cacheStamp;
        }

        public LanguageSettings(string cultureName, string uiCultureName, bool addPunctuators = false)
        {
            Contracts.AssertNonEmpty(cultureName);

            CultureName = cultureName;
            UICultureName = uiCultureName;

            LocToInvariantFunctionMap = new Dictionary<string, string>();
            LocToInvariantPunctuatorMap = new Dictionary<string, string>();
            InvariantToLocFunctionMap = new Dictionary<string, string>();
            InvariantToLocPunctuatorMap = new Dictionary<string, string>();

            _cacheStamp = 0;
            _cachedInvariantSettings = null;

            if (addPunctuators)
            {
                TexlLexer.ChoosePunctuators(this, out var dec, out var comma, out var list);
                AddPunctuator(dec, TexlLexer.PunctuatorDecimalSeparatorInvariant);
                AddPunctuator(comma, TexlLexer.PunctuatorCommaInvariant);
                AddPunctuator(list, TexlLexer.PunctuatorSemicolonInvariant);
            }
        }
    }
}
