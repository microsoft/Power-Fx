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
        private readonly Dictionary<string, string> _locToInvariantFunctionMap;
        private readonly Dictionary<string, string> _locToInvariantPunctuatorMap;
        private readonly Dictionary<string, string> _invariantToLocFunctionMap;
        private readonly Dictionary<string, string> _invariantToLocPunctuatorMap;
        private readonly string _cultureName;
        private readonly string _uiCultureName;

        private LanguageSettings _cachedInvariantSettings;
        private int _cacheStamp;

        public string CultureName => _cultureName;
        public string UICultureName => _uiCultureName;

        // Locale-specific to Invariant maps
        public Dictionary<string, string> LocToInvariantFunctionMap => _locToInvariantFunctionMap;
        public Dictionary<string, string> LocToInvariantPunctuatorMap => _locToInvariantPunctuatorMap;

        // Reverse maps
        public Dictionary<string, string> InvariantToLocFunctionMap => _invariantToLocFunctionMap;
        public Dictionary<string, string> InvariantToLocPunctuatorMap => _invariantToLocPunctuatorMap;

        public void AddFunction(string loc, string invariant)
        {
            Contracts.AssertNonEmpty(loc);
            Contracts.AssertNonEmpty(invariant);

            _locToInvariantFunctionMap[loc] = invariant;
            _invariantToLocFunctionMap[invariant] = loc;

            _cacheStamp++;
        }

        public void AddPunctuator(string loc, string invariant)
        {
            Contracts.AssertNonEmpty(loc);
            Contracts.AssertNonEmpty(invariant);

            _locToInvariantPunctuatorMap[loc] = invariant;
            _invariantToLocPunctuatorMap[invariant] = loc;

            _cacheStamp++;
        }

        public ILanguageSettings GetIdentitySettingsForInvariantLanguage()
        {
            if (_cachedInvariantSettings == null || NeedsRefresh(_cachedInvariantSettings))
            {
                _cachedInvariantSettings = new LanguageSettings("en-US", "en-US");

                _cachedInvariantSettings._cacheStamp = _cacheStamp;

                foreach (var kvp in _locToInvariantFunctionMap)
                    _cachedInvariantSettings.AddFunction(kvp.Value, kvp.Value);
                foreach (var kvp in _locToInvariantPunctuatorMap)
                    _cachedInvariantSettings.AddPunctuator(kvp.Value, kvp.Value);
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

            _cultureName = cultureName;
            _uiCultureName = uiCultureName;

            _locToInvariantFunctionMap = new Dictionary<string, string>();
            _locToInvariantPunctuatorMap = new Dictionary<string, string>();
            _invariantToLocFunctionMap = new Dictionary<string, string>();
            _invariantToLocPunctuatorMap = new Dictionary<string, string>();

            _cacheStamp = 0;
            _cachedInvariantSettings = null;

            if (addPunctuators)
            {
                string dec;
                string comma;
                string list;
                TexlLexer.ChoosePunctuators(this, out dec, out comma, out list);
                AddPunctuator(dec, TexlLexer.PunctuatorDecimalSeparatorInvariant);
                AddPunctuator(comma, TexlLexer.PunctuatorCommaInvariant);
                AddPunctuator(list, TexlLexer.PunctuatorSemicolonInvariant);
            }
        }
    }
}
