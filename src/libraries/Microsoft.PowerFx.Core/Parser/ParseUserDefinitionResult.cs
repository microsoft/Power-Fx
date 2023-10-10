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

        internal bool HasErrors { get; }

        public ParseUserDefinitionResult(IEnumerable<NamedFormula> namedFormulas, IEnumerable<UDF> uDFs, IEnumerable<DefinedType> definedTypes, IEnumerable<TexlError> errors, IEnumerable<CommentToken> comments)
        {
            NamedFormulas = namedFormulas;
            UDFs = uDFs;
            DefinedTypes = definedTypes;
            Comments = comments;

            if (errors?.Any() ?? false)
            {
                Errors = errors;
                HasErrors = true;
            }
        }
    }

    internal sealed class UserDefinitionsWithTrivia : SourceWithTrivia
    {
        internal Dictionary<string, UDFWithTrivia> UDFs { get; }

        internal Dictionary<string, NamedFormulaWithTrivia> NamedFormulas { get; }

        internal bool HasErrors { get; }

        public UserDefinitionsWithTrivia(Dictionary<string, NamedFormulaWithTrivia> namedFormulas, Dictionary<string, UDFWithTrivia> uDFs, bool hasErrors, ITexlSource triviaAtTheEnd)
            : base(null, triviaAtTheEnd)
        {
            NamedFormulas = namedFormulas;
            UDFs = uDFs;
            HasErrors = hasErrors;
        }
    }

    internal enum UserDefinitionKind
    {
        NamedFormula,
        UserDefinedFunction,
        TypeDefinition
    }
}
