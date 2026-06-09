// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Syntax.SourceInformation;

namespace Microsoft.PowerFx.Core.Parser
{
    internal sealed class ParseUserDefinitionResult
    {
        internal IEnumerable<UDF> UDFs { get; }

        internal IEnumerable<NamedFormula> NamedFormulas { get; }

        internal IEnumerable<DefinedType> DefinedTypes { get; }

        internal IEnumerable<TexlError> Errors { get; }

        internal IEnumerable<CommentToken> Comments { get; }

        // This is used to preserve the order of user definitions and all their source trivia (like comments), to be used by Pretty Print and other similar operations.
        internal IEnumerable<UserDefinitionSourceInfo> UserDefinitionSourceInfos { get; }

        // Trivia consumed after the terminating semicolon of the last user definition (whitespace and/or comments).
        // Preserved so pretty printing can emit a trailing comment like "MyNF = Pi()/2; // Half PI".
        internal ITexlSource TrailingTrivia { get; }

        internal bool HasErrors { get; }

        // There is a good chance that the script contained user definitions.
        // This looks for :=, f(a:type), and other parse structures that are unique to user definitions.
        // This determination is not definitive, but if true, user definition errors should be returned instead of standard parse errors.
        internal bool DefinitionsLikely { get; }

        // Attributes that were parsed but not attached to any UDF or named formula because the
        // attribute group is incomplete, or no definition name followed it (e.g. the maker is
        // still typing "[RecordLink("). Exposed so IntelliSense can offer suggestions for the
        // attribute and its arguments.
        internal IEnumerable<Attribute> IncompleteAttributes { get; }

        public ParseUserDefinitionResult(IEnumerable<NamedFormula> namedFormulas, IEnumerable<UDF> uDFs, IEnumerable<DefinedType> definedTypes, IEnumerable<TexlError> errors, IEnumerable<CommentToken> comments, IEnumerable<UserDefinitionSourceInfo> userDefinitionSourceInfos, bool definitionsLikely, ITexlSource trailingTrivia = null, IEnumerable<Attribute> incompleteAttributes = null)
        {
            NamedFormulas = namedFormulas;
            UDFs = uDFs;
            DefinedTypes = definedTypes;
            Comments = comments;
            UserDefinitionSourceInfos = userDefinitionSourceInfos;
            DefinitionsLikely = definitionsLikely;
            TrailingTrivia = trailingTrivia;
            IncompleteAttributes = incompleteAttributes ?? Enumerable.Empty<Attribute>();

            if (errors?.Any() ?? false)
            {
                Errors = errors;
                HasErrors = true;
            }
        }
    }
}
