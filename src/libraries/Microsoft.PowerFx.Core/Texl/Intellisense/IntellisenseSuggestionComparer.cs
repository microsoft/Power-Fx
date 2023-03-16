// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.PowerFx.Core.Utils;

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

            var thisIsExactMatch = IsExactMatch(x.Text, x.ExactMatch);
            var otherIsExactMatch = IsExactMatch(y.Text, y.ExactMatch);

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
                return x.Text.CompareTo(y.Text);
#pragma warning restore CA1310 // Specify StringComparison for correctness
            }
            
            return _culture.CompareInfo.GetStringComparer(CompareOptions.IgnoreCase).Compare(x.Text, y.Text);            
        }

        private bool IsExactMatch(string input, string match)
        {
            Contracts.AssertValue(input);
            Contracts.AssertValue(match);

            return input.Equals(match, StringComparison.OrdinalIgnoreCase);
        }
    }
}
