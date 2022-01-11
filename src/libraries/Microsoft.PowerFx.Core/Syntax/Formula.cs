// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Syntax
{
    using Conditional = System.Diagnostics.ConditionalAttribute;

    // This encapsulates a Texl formula, its parse tree and any parse errors. Note that
    // it doesn't include TexlBinding information, since that depends on context, while parsing
    // does not.
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

        public Formula(string script, ILanguageSettings loc = null)
        {
            Contracts.AssertValue(script);
            Contracts.AssertValueOrNull(loc);

            Script = script;
            Loc = loc;
            AssertValid();
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
                ParseTree = result.Root;
                _errors = result.Errors;
                Comments = result.Comments;
                HasParseErrors = result.HasError;
                Contracts.AssertValue(ParseTree);
                AssertValid();
            }

            return _errors == null;
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