// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Syntax
{
    internal sealed class LocalizationUtils
    {
        // The following properties/methods are referred to from JS (in Authoring mode) and should NOT be removed:
        // currentLocaleDot (intellisenseViewModel.js)
        // currentLocaleListSeparator (utility.js, testhooks.js)
        // currentLocaleDecimalSeparator (utility.js, testhooks.js)
        // currentLocaleChainingOperator (utility.js, testhooks.js)

        // references from TS code come via AuthoringCore.d.ts and that needs to be kept current with this file

        public static string CurrentLocaleDecimalSeparator => TexlLexer.GetLocalizedInstance(CultureInfo.CurrentCulture).LocalizedPunctuatorDecimalSeparator;

        public static string CurrentLocaleListSeparator => TexlLexer.GetLocalizedInstance(CultureInfo.CurrentCulture).LocalizedPunctuatorListSeparator;

        public static string CurrentLocaleChainingOperator => TexlLexer.GetLocalizedInstance(CultureInfo.CurrentCulture).LocalizedPunctuatorChainingSeparator;

        public static string CurrentLocalePositiveSymbol => TexlLexer.PunctuatorAdd;

        public static string CurrentLocaleNegativeSymbol => TexlLexer.PunctuatorSub;

        public static string CurrentLocaleMultiplySymbol => TexlLexer.PunctuatorMul;

        public static string CurrentLocaleDivideSymbol => TexlLexer.PunctuatorDiv;

        public static string CurrentLocaleEqual => TexlLexer.PunctuatorEqual;

        public static string CurrentLocaleParenOpen => TexlLexer.PunctuatorParenOpen;

        public static string CurrentLocaleParenClose => TexlLexer.PunctuatorParenClose;

        public static string CurrentLocaleBracketOpen => TexlLexer.PunctuatorBracketOpen;

        public static string CurrentLocaleBracketClose => TexlLexer.PunctuatorBracketClose;

        public static string CurrentLocaleCurlyOpen => TexlLexer.PunctuatorCurlyOpen;

        public static string CurrentLocaleCurlyClose => TexlLexer.PunctuatorCurlyClose;

        public static string CurrentLocalePercent => TexlLexer.PunctuatorPercent;

        public static string CurrentLocaleBang => TexlLexer.PunctuatorBang;

        public static string CurrentLocaleDot => TexlLexer.PunctuatorDot;

        public static string CurrentLocaleCaret => TexlLexer.PunctuatorCaret;

        public static string CurrentLocaleOr => TexlLexer.PunctuatorOr;

        public static string CurrentLocaleAnd => TexlLexer.PunctuatorAnd;

        public static string CurrentLocaleAmpersand => TexlLexer.PunctuatorAmpersand;

        public static string CurrentLocaleGreater => TexlLexer.PunctuatorGreater;

        public static string CurrentLocaleLess => TexlLexer.PunctuatorLess;

        public static string CurrentLocaleGreaterOrEqual => TexlLexer.PunctuatorGreaterOrEqual;

        public static string CurrentLocaleLessOrEqual => TexlLexer.PunctuatorLessOrEqual;

        public static string PunctuatorDotInvariant => TexlLexer.PunctuatorDot;

        internal static string ComposeSingleQuotedList(IEnumerable<string> listItems)
        {
            Contracts.AssertValue(listItems);
            Contracts.AssertNonEmpty(listItems);

            var singleQuoteFormat = StringResources.Get("ListItemSingleQuotedFormat");
            var listSeparator = CurrentLocaleListSeparator + " ";
            return string.Join(listSeparator, listItems.Select(item => string.Format(CultureInfo.InvariantCulture, singleQuoteFormat, item)));
        }
    }
}
