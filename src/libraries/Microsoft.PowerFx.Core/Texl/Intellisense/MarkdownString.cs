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
        /// <param name="markdown"></param>
        /// <returns></returns>
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
        /// <param name="plainText"></param>
        /// <returns></returns>
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
        /// Get the raw markdown string. 
        /// </summary>
        public string Markdown { get; init; }

        /// <summary>
        /// Concatenate two markdown strings. 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static MarkdownString operator +(MarkdownString left, MarkdownString right) => Add(left, right);

        /// <summary>
        /// Concatenate two markdown strings. 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static MarkdownString Add(MarkdownString left, MarkdownString right)
        {
            return new MarkdownString
            {
                Markdown = left.Markdown + right.Markdown
            };
        }
    }
}
