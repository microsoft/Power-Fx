// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.AppMagic.Transport;
using Microsoft.PowerFx.Core.Utils;
using StringBuilderCache = Microsoft.PowerFx.Core.Utils.StringBuilderCache<Microsoft.PowerFx.Syntax.Span>;

namespace Microsoft.PowerFx.Syntax
{
    /// <summary>
    /// Span in the text formula.
    /// </summary>
    [TransportType(TransportKind.ByValue)]
    [ThreadSafeImmutable]
    public sealed class Span
    {
        /// <summary>
        /// Start index of this span.
        /// </summary>
        public int Min { get; }

        /// <summary>
        /// End index of this span.
        /// </summary>
        public int Lim { get; }

        internal Span(int min, int lim)
        {
            Contracts.CheckParam(min >= 0, nameof(min));
            Contracts.CheckParam(lim >= min, nameof(lim));

            Min = min;
            Lim = lim;
        }

        internal Span(Span span)
        {
            Contracts.CheckParam(span.Min >= 0, "min");
            Contracts.CheckParam(span.Lim >= span.Min, "lim");

            Min = span.Min;
            Lim = span.Lim;
        }

        /// <summary>
        /// Get fragment of the text denoted by this span.
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        [TransportDisabled]
        public string GetFragment(string script)
        {
            Contracts.AssertValue(script);
            Contracts.Assert(Lim <= script.Length);
            return script.Substring(Min, Lim - Min);
        }

        /// <inheritdoc/>
        [TransportDisabled]
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "({0},{1})", Min, Lim);
        }

        [TransportDisabled]
        internal bool StartsWith(string script, string match)
        {
            Contracts.AssertValue(script);
            Contracts.Assert(Min <= script.Length);
            Contracts.AssertValue(match);

            return Min + match.Length <= script.Length && string.CompareOrdinal(script, Min, match, 0, match.Length) == 0;
        }

        [TransportDisabled]
        internal static string ReplaceSpans(string script, IEnumerable<KeyValuePair<Span, string>> worklist)
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

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is Span span &&
                   Min == span.Min &&
                   Lim == span.Lim;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = -1160472096;
            hashCode = (hashCode * -1521134295) + Min.GetHashCode();
            hashCode = (hashCode * -1521134295) + Lim.GetHashCode();
            return hashCode;
        }
    }
}
