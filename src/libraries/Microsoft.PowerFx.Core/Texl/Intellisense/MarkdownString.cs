// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.PowerFx.Intellisense
{
    /// <summary>
    /// A string that represents Github flavored markdown. 
    /// For use in displaying in intellisense. 
    /// </summary>
    [DebuggerDisplay("MARKDOWN: {Markdown}")]
    [ThreadSafeImmutable]
    public class MarkdownString
    {
        // private constructor to force use of factory method.
        private MarkdownString() 
        {
        }

        /// <summary>
        /// Create an instance over existing markdown. This doesn't validate and the markdown string must be valid.
        /// </summary>
        /// <param name="markdown">The markdown string to wrap.</param>
        /// <returns>A <see cref="MarkdownString"/> instance containing the provided markdown.</returns>
        public static MarkdownString FromMarkdown(string markdown)
        {
            return new MarkdownString
            {
                Markdown = markdown
            };
        }

        /// <summary>
        /// Newline in markdown. 
        /// </summary>
        public static readonly MarkdownString Newline = MarkdownString.FromMarkdown("\n\n");

        // See escaping rules: https://github.com/mattcone/markdown-guide/blob/master/_basic-syntax/escaping-characters.md
        // Escape with a \
        private static readonly ISet<char> _ecapeChars = new HashSet<char>
        {
            '\\', '`', '*', '_', '{', '}', '[', ']', '<', '>', '(', ')', '#', '+', '-', '.', '!', '|'
        };

        /// <summary>
        /// Create an instance over plain text. This will escape the plaintext if needed. 
        /// </summary>
        /// <param name="plainText">The plain text to convert to markdown, escaping as needed.</param>
        /// <returns>A <see cref="MarkdownString"/> instance containing the escaped plain text.</returns>
        public static MarkdownString FromString(string plainText)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var c in plainText)
            {
                if (_ecapeChars.Contains(c))
                {
                    sb.Append('\\');
                }

                sb.Append(c);
            }

            return new MarkdownString
            {
                Markdown = sb.ToString()
            };
        }

        /// <summary>
        /// Gets the raw markdown string. 
        /// </summary>
        public string Markdown { get; init; }

        /// <summary>
        /// Concatenate two markdown strings. 
        /// </summary>
        /// <param name="left">The first <see cref="MarkdownString"/> to concatenate.</param>
        /// <param name="right">The second <see cref="MarkdownString"/> to concatenate.</param>
        /// <returns>A new <see cref="MarkdownString"/> representing the concatenation of <paramref name="left"/> and <paramref name="right"/>.</returns>
        public static MarkdownString operator +(MarkdownString left, MarkdownString right) => Add(left, right);

        /// <summary>
        /// Concatenate two markdown strings. 
        /// </summary>
        /// <param name="left">The first <see cref="MarkdownString"/> to concatenate.</param>
        /// <param name="right">The second <see cref="MarkdownString"/> to concatenate.</param>
        /// <returns>A new <see cref="MarkdownString"/> representing the concatenation of <paramref name="left"/> and <paramref name="right"/>.</returns>
        public static MarkdownString Add(MarkdownString left, MarkdownString right)
        {
            return new MarkdownString
            {
                Markdown = left.Markdown + right.Markdown
            };
        }
    }
}
