// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.AppMagic.Transport;
using Microsoft.PowerFx.Core.Utils;
using StringBuilderCache = Microsoft.PowerFx.Core.Utils.StringBuilderCache<Microsoft.PowerFx.Core.Localization.Span>;

namespace Microsoft.PowerFx.Core.Localization
{
    [TransportType(TransportKind.ByValue)]
    public sealed class Span
    {
        public int Min { get; }

        public int Lim { get; }

        public Span(int min, int lim)
        {
            Contracts.CheckParam(min >= 0, "min");
            Contracts.CheckParam(lim >= min, "lim");

            Min = min;
            Lim = lim;
        }

        public Span(Span span)
        {
            Contracts.CheckParam(span.Min >= 0, "min");
            Contracts.CheckParam(span.Lim >= span.Min, "lim");

            Min = span.Min;
            Lim = span.Lim;
        }

        [TransportDisabled]
        public string GetFragment(string script, int offset = 0)
        {
            Contracts.AssertValue(script);
            Contracts.Assert(Lim + offset <= script.Length);
            return script.Substring(Min + offset, Lim - Min);
        }

        [TransportDisabled]
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "({0},{1})", Min, Lim);
        }

        [TransportDisabled]
        public bool StartsWith(string script, string match)
        {
            Contracts.AssertValue(script);
            Contracts.Assert(Min <= script.Length);
            Contracts.AssertValue(match);

            return Min + match.Length <= script.Length && string.CompareOrdinal(script, Min, match, 0, match.Length) == 0;
        }

        // Generic span replacer. Given a set of unordered spans and replacement strings for
        // each, this produces a new string with all the specified spans replaced accordingly.
        [TransportDisabled]
        public static string ReplaceSpans(string script, IEnumerable<KeyValuePair<Span, string>> worklist)
        {
            Contracts.AssertValue(script);
            Contracts.AssertValue(worklist);

            StringBuilder sb = null;
            try
            {
                sb = StringBuilderCache.Acquire(script.Length);
                var index = 0;

                foreach (var pair in worklist.OrderBy(kvp => kvp.Key.Min))
                {
                    sb.Append(script, index, pair.Key.Min - index);
                    sb.Append(pair.Value);
                    index = pair.Key.Lim;
                }

                if (index < script.Length)
                {
                    sb.Append(script, index, script.Length - index);
                }

                return sb.ToString();
            }
            finally
            {
                if (sb != null)
                {
                    StringBuilderCache.Release(sb);
                }
            }
        }

        public override bool Equals(object obj)
        {
            return obj is Span span &&
                   Min == span.Min &&
                   Lim == span.Lim;
        }

        public override int GetHashCode()
        {
            var hashCode = -1160472096;
            hashCode = (hashCode * -1521134295) + Min.GetHashCode();
            hashCode = (hashCode * -1521134295) + Lim.GetHashCode();
            return hashCode;
        }
    }
}
