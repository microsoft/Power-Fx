// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Syntax.SourceInformation;

namespace Microsoft.PowerFx.Core.Parser
{
    /// <summary>
    /// Represents all the user definitions and their trivia
    /// </summary>
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
}
