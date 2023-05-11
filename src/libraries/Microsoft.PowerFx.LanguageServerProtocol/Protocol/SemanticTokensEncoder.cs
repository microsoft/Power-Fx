// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Texl.Intellisense;

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    /// <summary>
    /// An entity with methods to encode the tokens for the responses to the requests for semantic tokens in language server.
    /// </summary>
    internal static class SemanticTokensEncoder
    {
        /// <summary>
        /// There are no token modifiers in powerfx.
        /// </summary>
        private const uint NoTokenModifiersMask = 0u;

        /// <summary>
        /// A constant to indicate how many slots each token would occupy in the encoded tokens collection.
        /// </summary>
        internal const int SlotsPerToken = 5;

        /// <summary>
        /// A mapping from token type to its unsigned integer encoded value. Each token type is encoded using its position in the TokenType enum.
        /// </summary>
        private static readonly IReadOnlyDictionary<TokenType, uint> EncodedTokenTypes = Enum.GetValues(typeof(TokenType))
                                                                                             .OfType<TokenType>()
                                                                                             .Where(tokenType => tokenType == TokenType.Unknown || (tokenType != TokenType.Min && tokenType != TokenType.Lim))
                                                                                             .Distinct()
                                                                                             .Select((tokenType, index) => (tokenType, index))
                                                                                             .ToDictionary((tokenTypeAndIndex) => tokenTypeAndIndex.tokenType, (tokenTypeAndIndex) => (uint)tokenTypeAndIndex.index);

        /// <summary>
        /// Encodes the given collection of tokens into an unsigned integer array. 
        /// <para>Each token is encoded to 5 integer values and therefore the size of the encoded array is 5 * number of given tokens.</para>
        /// <para>1) First integer for each token is the line number relative to the line number of previous token (currentTokenLineNumber - previousTokenLineNumber).</para>
        /// 
        /// <para>2) Second integer for each token is the start index on the line it is on relative to the start index of previous token. 
        /// If prev token and current token are on the same line, then start index is subtracted from prev token's start index else it remains same. </para>
        /// 
        /// <para>3) Third integer for each token is the length computed by subtracting token's end index and start index. </para>
        /// 
        /// <para>4) Fourth integer for each token is the token type encoded to an unsigned integer. 
        /// Usually this is the position of token in the list or enum of all token types. </para>
        /// 
        /// <para>5) Fifth integer is a bit mask representing what token modifiers are applied to each token. this is always zero as powerfx doesn't have token modifiers.</para>
        /// </summary>
        /// <param name="tokens">Collection of tokens that would be encoded.</param>
        /// <param name="expression">Expression from which these tokens were extracted from.</param>
        /// <param name="eol">End of line character indiciating the line breaks in the given expression.</param>
        /// <returns>Encoded Tokens.</returns>
        // This encoding is done according to LSP specification for semantic tokens methods https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#textDocument_semanticTokens
        public static IEnumerable<uint> EncodeTokens(IEnumerable<ITokenTextSpan> tokens, string expression, string eol)
        {
            if (tokens == null)
            {
                return new uint[0];
            }

            var tokensByStartIdxs = new Dictionary<int, ITokenTextSpan>();
            foreach (var token in tokens)
            {
                // Skip the tokens with a non-positive length
                if (token.EndIndex - token.StartIndex <= 0)
                {
                    continue;
                }

                if (tokensByStartIdxs.TryGetValue(token.StartIndex, out var existingTokenAtCurrentTokenStartIdx))
                {
                    // Not likely to occur, but if multiple tokens start at the same index then pick the one with smallest end index
                    // to potentially reduce the possibility of overlapping with tokens starting at different indexes
                    // monaco-editor doesn't support overlapping tokens
                    if (existingTokenAtCurrentTokenStartIdx.EndIndex > token.EndIndex)
                    {
                        tokensByStartIdxs[token.StartIndex] = token;
                    }
                }
                else
                {
                    tokensByStartIdxs.Add(token.StartIndex, token);
                }
            }

            if (tokensByStartIdxs.Count == 0)
            {
                return new uint[0];
            }

            var encodedTokens = new LinkedList<uint>();
            var currentLineNumber = 0u;
            var previousTokenLineNumber = 0u;
            var previousTokenStartIdx = 0u;
            var endOfLineMatchIndex = 0;
            var positionOnCurrentLine = 0u;
            ITokenTextSpan currentToken = null;
            uint? currentTokenStartLine = null;
            uint? currentTokenStartPosition = null;

            for (var i = 0; i < expression.Length; i++)
            {
                if (tokensByStartIdxs.TryGetValue(i, out var tokenAtCurrentIndex) && currentToken == null)
                {
                    currentToken = tokenAtCurrentIndex;
                    currentTokenStartLine = currentLineNumber;
                    currentTokenStartPosition = positionOnCurrentLine;
                }

                // Current token ends here -> encode it and put it into the encoded list
                if (currentToken != null && currentToken.EndIndex - 1 == i && (endOfLineMatchIndex + 1 < eol.Length || eol[endOfLineMatchIndex] != expression[i]))
                {
                    EncodeAndAddToken(encodedTokens, currentToken, currentTokenStartLine, currentTokenStartPosition, positionOnCurrentLine, currentLineNumber, ref previousTokenLineNumber, ref previousTokenStartIdx);
                    (currentToken, currentTokenStartLine, currentTokenStartPosition) = (null, null, null);
                }

                if (endOfLineMatchIndex < eol.Length)
                {
                    if (expression[i] == eol[endOfLineMatchIndex])
                    { 
                        // We have reached the end of the current line
                        if (++endOfLineMatchIndex >= eol.Length)
                        {
                            // If current token is not null, we are dealing with multiline token, consider the current line of token as a separate encoded token

                            EncodeAndAddToken(encodedTokens, currentToken, currentTokenStartLine, currentTokenStartPosition, eol.Length > positionOnCurrentLine ? null : positionOnCurrentLine - (uint)eol.Length, currentLineNumber, ref previousTokenLineNumber, ref previousTokenStartIdx);
                            if (currentToken != null && i == currentToken.EndIndex - 1)
                            {
                                (currentToken, currentTokenStartLine, currentTokenStartPosition) = (null, null, null);
                            }

                            currentLineNumber++;
                            endOfLineMatchIndex = 0;
                            positionOnCurrentLine = 0;
                            continue;
                        }
                    }
                    else
                    {
                        endOfLineMatchIndex = 0;
                    }
                }

                positionOnCurrentLine++;
            }

            return encodedTokens;
        }

        /// <summary>
        /// Encodes the token and adds it the given <paramref name="encodedTokens"/> collection.
        /// </summary>
        /// <param name="encodedTokens">Collection to add encoded token to.</param>
        /// <param name="currentToken">Token to be encoded.</param>
        /// <param name="currentTokenStartLine">Start line number of the token to be encoded.</param>
        /// <param name="currentTokenStartPosition">Starting position of the token to be encoded.</param>
        /// <param name="endPosition">End Position of the token to be encoded.</param>
        /// <param name="currentLineNumber">Curent Line Number in the expression.</param>
        /// <param name="previousTokenLineNumber">Line number of the previous encoded token.</param>
        /// <param name="previousTokenStartIdx">Starting position of the previous encoded token.</param>
        private static void EncodeAndAddToken(ICollection<uint> encodedTokens, ITokenTextSpan currentToken, uint? currentTokenStartLine, uint? currentTokenStartPosition, uint? endPosition, uint currentLineNumber,  ref uint previousTokenLineNumber, ref uint previousTokenStartIdx)
        {
            if (currentToken == null || !currentTokenStartLine.HasValue || !currentTokenStartPosition.HasValue || !endPosition.HasValue)
            {
                return;
            }

            // if token ends on the same line as it starts, pick the position it was first seen on else pick 0 (multi line token)
            var startPosition = currentLineNumber == currentTokenStartLine.Value ? currentTokenStartPosition.Value : 0u;
            var length = (endPosition.Value + 1) - startPosition;
            if (length <= 0)
            {
                return;
            }

            // Line number of current token relative to previous token
            encodedTokens.Add(currentLineNumber - previousTokenLineNumber);

            // Starting position of current token relative to previous token starting position. 
            encodedTokens.Add(currentLineNumber == previousTokenLineNumber ? startPosition - previousTokenStartIdx : startPosition);

            // Length of the token
            encodedTokens.Add(length);

            // Encoded Token Type
            encodedTokens.Add(GetEncodedTokenType(currentToken.TokenType));

            // Bit mask representing token modifiers applied to the current token.
            encodedTokens.Add(NoTokenModifiersMask);

            previousTokenLineNumber = currentLineNumber;
            previousTokenStartIdx = startPosition;
        }

        /// <summary>
        /// Returns the encoded value for the given token type.
        /// </summary>
        /// <param name="tokenType">TokenType.</param>
        /// <returns>Encoded value.</returns>
        private static uint GetEncodedTokenType(TokenType tokenType)
        {
            if (EncodedTokenTypes.TryGetValue(tokenType, out var encodedTokenType))
            {
                return encodedTokenType;
            }

            // For any token type that doesn't have an encoded value in EncodedTokenTypes, use the encoded token type value of TokenType.Unknown
            return EncodedTokenTypes[TokenType.Unknown];
        }

        /// <summary>
        /// Returns the token type from the encoded value of the token type.
        /// <para>Note: Should be used for tests only.</para>
        /// </summary>
        /// <param name="encodedValue">Encoded value of a token.</param>
        /// <returns>TokenType.</returns>
        internal static TokenType TestOnly_GetTokenTypeFromEncodedValue(uint encodedValue)
        {
            var decodedTokenTypes = EncodedTokenTypes.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
            if (decodedTokenTypes.TryGetValue(encodedValue, out var tokenType))
            {
                return tokenType;
            }

            return TokenType.Unknown;
        }
    }
}
