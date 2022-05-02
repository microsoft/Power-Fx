// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Conditional = System.Diagnostics.ConditionalAttribute;

namespace Microsoft.PowerFx.Syntax
{
    /// <summary>
    /// This encapsulates a Texl formula, its parse tree and any parse errors. Note that
    /// it doesn't include TexlBinding information, since that depends on context, while parsing
    /// does not.
    /// This a <see cref="ParseResult"/> plus the original expression text. 
    /// This is also used by intellisense. 
    /// </summary>
    internal sealed class Formula
    {
        public readonly string Script;

        // The language settings used for parsing this script.
        // May be null if the script is to be parsed in the current locale.
        public readonly ILanguageSettings Loc;
        private List<TexlError> _errors;

        // This may be null if the script hasn't yet been parsed.
        internal TexlNode ParseTree { get; private set; }

        internal List<CommentToken> Comments { get; private set; }

        public Formula(string script, TexlNode tree, ILanguageSettings loc = null)
        {
            Contracts.AssertValue(script);
            Contracts.AssertValueOrNull(loc);

            Script = script;
            ParseTree = tree;
            Loc = loc;
            AssertValid();
        }

        public Formula(string script, ILanguageSettings loc = null)
    : this(script, null, loc)
        {
        }

        [Conditional("DEBUG")]
        private void AssertValid()
        {
            Contracts.AssertValue(Script);
            Contracts.Assert(_errors == null || _errors.Count > 0);
            Contracts.Assert(ParseTree != null || _errors == null);
        }

        public bool HasParseErrors { get; private set; }

        // True if the formula has already been parsed.
        public bool IsParsed => ParseTree != null;

        public bool EnsureParsed(TexlParser.Flags flags)
        {
            AssertValid();

            if (ParseTree == null)
            {
                var result = TexlParser.ParseScript(Script, loc: Loc, flags: flags);
                ApplyParse(result);
            }

            return _errors == null;
        }

        internal void ApplyParse(ParseResult result)
        {
            ParseTree = result.Root;
            _errors = result._errors;
            Comments = result.Comments;
            HasParseErrors = result.HasError;
            Contracts.AssertValue(ParseTree);
            AssertValid();
        }

        public IEnumerable<TexlError> GetParseErrors()
        {
            AssertValid();
            Contracts.Assert(IsParsed, "Should call EnsureParsed() first!");
            return _errors ?? Enumerable.Empty<TexlError>();
        }

        public override string ToString()
        {
            return Script;
        }
    }
}
