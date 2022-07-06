// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Syntax
{
    internal class TokenCollection
    {
        public readonly Token[] Contents;

        // The class assumes ownership of newContents
        public TokenCollection(Token[] newContents)
        {
            Contracts.AssertValue(newContents);
            Contents = newContents;
        }

        public TokenCollection(IEnumerable<Token> newContents)
        {
            Contracts.AssertValue(newContents);
            Contents = newContents.ToArray();
        }

        // Provides a set of textspans representing stretches of the current TokenString that are
        // lexically equal to the needle.  Does not provide overlapping matches, and therefore
        // does not provide the complete set of matches
        public IEnumerable<Span> GetAllMatches(TokenCollection needle)
        {
            Contracts.AssertValue(needle);
            Contracts.Assert(needle.Contents.Length > 1);
            Contracts.Assert(needle.Contents[needle.Contents.Length - 1] is EofToken);

            // Ignore the EofToken at the end of the needle tokenstring
            var needleLength = needle.Contents.Length - 1;
            var matches = new Queue<Span>();

            // Cycle through the current TokenString looking for substring matches
            for (var haystackIndex = 0; haystackIndex <= Contents.Length - needleLength;)
            {
                var doMatch = true;

                // Compare the needle with the current region of the haystack looking for a match
                for (var needleIndex = 0; needleIndex < needleLength; needleIndex++)
                {
                    if (!Contents[haystackIndex + needleIndex].Equals(needle.Contents[needleIndex]))
                    {
                        doMatch = false;
                        break;
                    }
                }

                // If they match, add the appropriate tokenspan to the output and increase haystackIndex to avoid
                // possibly recording overlapping matches
                if (doMatch)
                {
                    // The (- 1) portion of the below expression accounts for the fact that we want the
                    // lim of the TextSpan to be the lim of the last token that matches the needle
                    var spanLim = Contents[haystackIndex + needleLength - 1].Span.Lim;
                    var spanMin = Contents[haystackIndex].Span.Min;
                    matches.Enqueue(new Span(spanMin, spanLim));
                    haystackIndex += needle.Contents.Length - 1;
                }
                else
                {
                    haystackIndex++;
                }
            }

            return matches;
        }
    }
}
