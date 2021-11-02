// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.Core.Public
{
    /// <summary>
    /// Get tokens flags
    ///
    /// This is used in the following:
    ///  1. Client sends didOpen with "getTokensFlags=3" (UsedInExpression & AllFunctions)
    ///  2. Server sends publishTokens with all supported functions + tokens in existing formula
    ///  3. Client updates normalizedCompletionLookup, which is used in monacoParam.languages.registerOnTypeFormattingEditProvider to do auto case correction
    ///  4. Client updates tokenizer (which maps token to theme color) for syntax highlighting
    ///  5. When formula changes in client, client sends didChange with "getTokensFlags=1" (UsedInExpression)
    ///  6. Server sends publishTokens with tokens in existing formula
    ///  7. Client updates normalizedCompletionLookup & tokenizer
    /// </summary>
    internal enum GetTokensFlags : uint
    {
        /// <summary>
        /// No token
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Tokens only used in the given expression
        /// </summary>
        UsedInExpression = 0x1,

        /// <summary>
        /// All available functions can be used
        /// </summary>
        AllFunctions = 0x2
    }
}
