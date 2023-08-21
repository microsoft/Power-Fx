// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Intellisense
{
    internal class IntellisenseSuggestionComparer : IComparer<IntellisenseSuggestion>
    {
        private readonly CultureInfo _culture;

        public IntellisenseSuggestionComparer(CultureInfo culture)
        {
            _culture = culture;
        }

        public int Compare(IntellisenseSuggestion x, IntellisenseSuggestion y)
        {
            if (x == null && y == null)
            {
                return 0;
            }

            if (x == null)
            {
                return 1;
            }

            if (y == null)
            {
                return -1;
            }

            var xText = MaybeRemoveDelimiter(x.Text, _culture);
            var yText = MaybeRemoveDelimiter(y.Text, _culture);

            var thisIsExactMatch = IsExactMatch(xText, MaybeRemoveDelimiter(x.ExactMatch, _culture));
            var otherIsExactMatch = IsExactMatch(yText, MaybeRemoveDelimiter(y.ExactMatch, _culture));

            if (thisIsExactMatch && !otherIsExactMatch)
            {
                return -1;
            }
            else if (!thisIsExactMatch && otherIsExactMatch)
            {
                return 1;
            }

            if (x.SortPriority != y.SortPriority)
            {
                return (int)(y.SortPriority - x.SortPriority);
            }

            if (_culture == null)
            {
#pragma warning disable CA1310 // Specify StringComparison for correctness
                return xText.CompareTo(yText);
#pragma warning restore CA1310 // Specify StringComparison for correctness
            }

            return _culture.CompareInfo.GetStringComparer(CompareOptions.IgnoreCase).Compare(xText, yText);            
        }

        private bool IsExactMatch(string input, string match)
        {
            Contracts.AssertValue(input);
            Contracts.AssertValue(match);

            return input.Equals(match, StringComparison.OrdinalIgnoreCase);
        }

        private static string MaybeRemoveDelimiter(string input, CultureInfo culture)
        {
            Contracts.AssertValue(input);
            if (input.StartsWith(TexlLexer.IdentifierDelimiter.ToString(), true, culture ?? CultureInfo.CurrentCulture) 
                && input.EndsWith(TexlLexer.IdentifierDelimiter.ToString(), true, culture ?? CultureInfo.CurrentCulture))
            {
                return input.Substring(1, input.Length - 2);
            }

            return input;
        }
    }
}
