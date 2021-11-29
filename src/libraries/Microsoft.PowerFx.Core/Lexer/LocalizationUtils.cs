// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Lexer
{
    internal sealed class LocalizationUtils
    {
        // The following properties/methods are referred to from JS (in Authoring mode) and should NOT be removed:
        // currentLocaleDot (intellisenseViewModel.js)
        // currentLocaleListSeparator (utility.js, testhooks.js)
        // currentLocaleDecimalSeparator (utility.js, testhooks.js)
        // currentLocaleChainingOperator (utility.js, testhooks.js)

        // references from TS code come via AuthoringCore.d.ts and that needs to be kept current with this file

        public static string CurrentLocaleDecimalSeparator { get { return TexlLexer.LocalizedInstance.LocalizedPunctuatorDecimalSeparator; } }
        public static string CurrentLocaleListSeparator { get { return TexlLexer.LocalizedInstance.LocalizedPunctuatorListSeparator; } }
        public static string CurrentLocaleChainingOperator { get { return TexlLexer.LocalizedInstance.LocalizedPunctuatorChainingSeparator; } }
        public static string CurrentLocalePositiveSymbol { get { return TexlLexer.PunctuatorAdd; } }
        public static string CurrentLocaleNegativeSymbol { get { return TexlLexer.PunctuatorSub; } }
        public static string CurrentLocaleMultiplySymbol { get { return TexlLexer.PunctuatorMul; } }
        public static string CurrentLocaleDivideSymbol { get { return TexlLexer.PunctuatorDiv; } }
        public static string CurrentLocaleEqual { get { return TexlLexer.PunctuatorEqual; } }
        public static string CurrentLocaleParenOpen { get { return TexlLexer.PunctuatorParenOpen; } }
        public static string CurrentLocaleParenClose { get { return TexlLexer.PunctuatorParenClose; } }
        public static string CurrentLocaleBracketOpen { get { return TexlLexer.PunctuatorBracketOpen; } }
        public static string CurrentLocaleBracketClose { get { return TexlLexer.PunctuatorBracketClose; } }
        public static string CurrentLocaleCurlyOpen { get { return TexlLexer.PunctuatorCurlyOpen; } }
        public static string CurrentLocaleCurlyClose { get { return TexlLexer.PunctuatorCurlyClose; } }
        public static string CurrentLocalePercent { get { return TexlLexer.PunctuatorPercent; } }
        public static string CurrentLocaleBang { get { return TexlLexer.PunctuatorBang; } }
        public static string CurrentLocaleDot { get { return TexlLexer.PunctuatorDot; } }
        public static string CurrentLocaleCaret { get { return TexlLexer.PunctuatorCaret; } }
        public static string CurrentLocaleOr { get { return TexlLexer.PunctuatorOr; } }
        public static string CurrentLocaleAnd { get { return TexlLexer.PunctuatorAnd; } }
        public static string CurrentLocaleAmpersand { get { return TexlLexer.PunctuatorAmpersand; } }
        public static string CurrentLocaleGreater { get { return TexlLexer.PunctuatorGreater; } }
        public static string CurrentLocaleLess { get { return TexlLexer.PunctuatorLess; } }
        public static string CurrentLocaleGreaterOrEqual { get { return TexlLexer.PunctuatorGreaterOrEqual; } }
        public static string CurrentLocaleLessOrEqual { get { return TexlLexer.PunctuatorLessOrEqual; } }
        public static string PunctuatorDotInvariant { get { return TexlLexer.PunctuatorDot; } }

        internal static string ComposeSingleQuotedList(IEnumerable<string> listItems)
        {
            Contracts.AssertValue(listItems);
            Contracts.AssertNonEmpty(listItems);

            string singleQuoteFormat = StringResources.Get("ListItemSingleQuotedFormat");
            string listSeparator = LocalizationUtils.CurrentLocaleListSeparator + " ";
            return string.Join(listSeparator, listItems.Select(item => string.Format(singleQuoteFormat, item)));
        }
    }
}
