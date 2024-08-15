// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Parser
{
    internal sealed class ParseUserDefinitionResult
    {
        internal IEnumerable<UDF> UDFs { get; }

        internal IEnumerable<NamedFormula> NamedFormulas { get; }

        internal IEnumerable<DefinedType> DefinedTypes { get; }

        internal IEnumerable<TexlError> Errors { get; }

        internal IEnumerable<CommentToken> Comments { get; }

        internal IEnumerable<(int, UserDefinitionType, IdentToken, string)> Indices { get; }

        internal bool HasErrors { get; }

        public TexlNode Root { get; }

        public ParseUserDefinitionResult(IEnumerable<NamedFormula> namedFormulas, IEnumerable<UDF> uDFs, IEnumerable<DefinedType> definedTypes, IEnumerable<TexlError> errors, IEnumerable<CommentToken> comments, TexlNode root = null, IEnumerable<(int, UserDefinitionType, IdentToken, string)> indices = null)
        {
            NamedFormulas = namedFormulas;
            UDFs = uDFs;
            DefinedTypes = definedTypes;
            Comments = comments;
            Root = root;
            Indices = indices;

            if (errors?.Any() ?? false)
            {
                Errors = errors;
                HasErrors = true;
            }
        }
    }
}
