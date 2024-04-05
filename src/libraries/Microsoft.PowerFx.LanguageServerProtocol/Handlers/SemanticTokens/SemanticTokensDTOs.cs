// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Texl.Intellisense;

namespace Microsoft.PowerFx.LanguageServerProtocol.Handlers
{
    /// <summary>
    /// Small DTO to hold the context needed for the GetTokens operation.
    /// <param name="tokenTypesToSkip">The token types to skip.</param>
    /// <param name="documentUri">The semantic tokens parameters.</param>
    /// <param name="expression">The expression to get tokens for.</param>
    /// </summary>
    public record GetTokensContext(HashSet<TokenType> tokenTypesToSkip, string documentUri, string expression);
}
