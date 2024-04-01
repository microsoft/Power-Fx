// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Syntax;
using static Microsoft.PowerFx.Core.Parser.TexlParser;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Options for parsing an expression.
    /// </summary>
    public class ParserOptions
    {
        /// <summary>
        /// If true, allow parsing a chaining operator. This is only used for side-effecting operations.
        /// </summary>
        public bool AllowsSideEffects { get; set; }

        /// <summary>
        /// If true, numbers are treated as float.  By default, numbers are treated as decimal.
        /// </summary>
        public bool NumberIsFloat { get; set; }

        /// <summary>
        /// If true, parse starts in text literal mode, with string interpolation islands of ${ ... }.
        /// If the starting non-whitespace charatcer is an equal sign ("="), a normal parse is used instead.
        /// </summary>
        public bool TextFirst { get; set; }

        /// <summary>
        /// If true, various words have been reserved and are not available for identifiers.
        /// Will only be set by Canvas apps, all other Power Fx hosts should have reserved keywords always enforced.
        /// </summary>
        internal bool DisableReservedKeywords { get; set; }

        /// <summary>
        /// The culture that an expression will parse with. 
        /// This primarily determines numeric decimal separator character
        /// as well as chaining operator. 
        /// </summary>
        public CultureInfo Culture { get; set; }

        /// <summary>
        /// If greater than 0, enforces a maximum length on a single expression. 
        /// It is an immediate parser error if the expression is too long. 
        /// </summary>
        public int MaxExpressionLength { get; set; }

        /// <summary>
        /// Flag for parse type literals.
        /// </summary>
        internal bool AllowParseAsTypeLiteral { get; set; }

        /// <summary>
        /// Allow parsing of attributes on user definitions
        /// This is an early prototype, and so is internal.
        /// </summary>
        internal bool AllowAttributes { get; set; }

        public ParserOptions()
        {
        }

        public ParserOptions(CultureInfo culture, bool allowsSideEffects = false, int maxExpressionLength = 0)
        {
            Culture = culture;
            AllowsSideEffects = allowsSideEffects;
            MaxExpressionLength = maxExpressionLength;
        }

        internal ParseResult Parse(string script)
        {
            return Parse(script, Features.None);
        }

        private CallNode FindSummarize(TexlNode node)
        {
            if (node is CallNode call)
            {
                if (call.Head.Name.Value == "Summarize")
                {
                    return call;
                }
                else
                {
                    return FindSummarize(call.Args);
                }
            }
            else if (node is VariadicBase var)
            {
                for (int i = 0; i < var.ChildNodes.Count(); i++)
                {
                    var s = FindSummarize(var.Children[i]);
                    if (s != null)
                    {
                        return s;
                    }
                }
            }

            return null;
        }

        private static int summarizeCounter;

        internal ParseResult Parse(string script, Features features)
        {
            if (MaxExpressionLength > 0 && script.Length > MaxExpressionLength)
            {
                // If too long, don't even attempt to lex or parse it. 
                var result2 = ParseResult.ErrorTooLarge(script, MaxExpressionLength);
                result2.Options = this;
                return result2;
            }

            var flags = (AllowsSideEffects ? TexlParser.Flags.EnableExpressionChaining : 0) |
                        (NumberIsFloat ? TexlParser.Flags.NumberIsFloat : 0) |
                        (DisableReservedKeywords ? TexlParser.Flags.DisableReservedKeywords : 0) |
                        (TextFirst ? TexlParser.Flags.TextFirst : 0);

            var result = TexlParser.ParseScript(script, features, Culture, flags);
            result.Options = this;

            if (result.IsSuccess)
            {
                var summarize = FindSummarize(result.Root);

                if (summarize != null)
                {
                    var id = ++summarizeCounter;
                    List<string> groupColumns = new List<string>();
                    Dictionary<string, string> aggregates = new Dictionary<string, string>();
                    var table = summarize.Args.Children[0].GetCompleteSpan().GetFragment(script);

                    for (var i = 1; i < summarize.Args.Count; i++)
                    {
                        switch (summarize.Args.Children[i])
                        {
                            case FirstNameNode fn:
                                groupColumns.Add(fn.Ident.Name);
                                break;
                            case AsNode a:
                                aggregates.Add(a.Right.Name, a.Left.ToString());
                                break;
                            default:
                                return new ParseResult(
                                    summarize,
                                    new List<TexlError>() { new TexlError(summarize, DocumentErrorSeverity.Critical, TexlStrings.ErrSummarizeColumnOrAs) },
                                    true,
                                    new List<CommentToken>(),
                                    null,
                                    null,
                                    script)
                                {
                                    Options = this
                                };
                        }
                    }

                    var script2 = $"With( {{ _table{id}:{table} }},\n ForAll( Distinct( _table{id}, " +
                                $"JSON( {{ {string.Join(",", groupColumns.Select(x => $"{x}:{x}"))} }} ) ) As _distinct{id}, \n" +
                                $"  With( AddColumns( {{ {string.Join(",", groupColumns.Select(x => $"{x}:{(x.StartsWith("num", StringComparison.InvariantCultureIgnoreCase) ? "Value" : "Text")}(ParseJSON(_distinct{id}.Value).{x})"))} }},\n" +
                                $"     ThisGroup, DropColumns( Filter( _table{id} As _filter{id}, {string.Join(" And ", groupColumns.Select(x => $"_filter{id}.{x}={x}"))} ), " +
                                $"{string.Join(",", groupColumns.Select(x => $"{x}"))}) ),\n" +
                                $"{{ {string.Join(", ", groupColumns.Select(x => $"{x}:{x}"))}, {string.Join(", ", aggregates.Keys.Select(x => $"{x}:{aggregates[x]}"))} }} ) ) )";

                    Debug.WriteLine(string.Empty);
                    Debug.Write("Summarize: " + script);
                    Debug.Write("Transform: " + script2);

                    var script3 = script.Substring(0, summarize.GetCompleteSpan().Min) + script2 + script.Substring(summarize.GetCompleteSpan().Lim);
                    return this.Parse(script3, features);
                }
            }

            return result;
        }

        internal Flags GetParserFlags()
        {
            return (AllowsSideEffects ? TexlParser.Flags.EnableExpressionChaining : 0) |
                        (NumberIsFloat ? TexlParser.Flags.NumberIsFloat : 0) |
                        (DisableReservedKeywords ? TexlParser.Flags.DisableReservedKeywords : 0) |
                        (TextFirst ? TexlParser.Flags.TextFirst : 0);
        }
    }
}
