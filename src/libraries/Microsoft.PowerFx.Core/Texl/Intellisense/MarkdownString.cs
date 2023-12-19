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
    public class MarkdownString
    {
        // private constructor to force use of factory method.
        private MarkdownString() 
        {
        }

        public static MarkdownString FromMarkdown(string markdown)
        {
            return new MarkdownString
            {
                Markdown = markdown
            };
        }

        // See escaping rules: https://github.com/mattcone/markdown-guide/blob/master/_basic-syntax/escaping-characters.md
        // Escape with a \
        private static readonly HashSet<char> _ecapeChars = new HashSet<char>
        {
            '\\', '`', '*', '_', '{', '}', '[', ']', '<', '>', '(', ')', '#', '+', '-', '.', '!', '|'
        };

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

        public string Markdown { get; init; }

        public static MarkdownString operator +(MarkdownString left, MarkdownString right) => Add(left, right);

        public static MarkdownString Add(MarkdownString left, MarkdownString right)
        {
            return new MarkdownString
            {
                Markdown = left.Markdown + right.Markdown
            };
        }
    }
}
