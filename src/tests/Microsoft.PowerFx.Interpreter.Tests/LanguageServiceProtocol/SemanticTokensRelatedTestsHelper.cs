// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Texl.Intellisense;
using Microsoft.PowerFx.LanguageServerProtocol;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.Interpreter.Tests.LanguageServiceProtocol
{
    internal static class SemanticTokensRelatedTestsHelper
    {
        /// <summary>
        /// Partially decodes the given encoded tokens. Partially decoded tokens are missing the actual token names and the multiline tokens are not put back together.
        /// </summary>
        /// <param name="tokensResponse">Encoded Tokens Response.</param>
        /// <param name="expression">Expression.</param>
        /// <param name="eol">End od Line.</param>
        /// <returns>Partially Decoded Tokens.</returns>
        public static IEnumerable<ITokenTextSpan> DecodeEncodedSemanticTokensPartially(SemanticTokensResponse tokensResponse, string expression, string eol = "\n")
        {
            return DecodeEncodedSemanticTokensPartially(tokensResponse.Data, expression, eol);
        }

        /// <summary>
        /// Partially decodes the given encoded tokens. Partially decoded tokens are missing the actual token names and the multiline tokens are not put back together.
        /// </summary>
        /// <param name="data">Encoded Tokens.</param>
        /// <param name="expression">Expression.</param>
        /// <param name="eol">End od Line.</param>
        /// <returns>Partially Decoded Tokens.</returns>
        public static IEnumerable<ITokenTextSpan> DecodeEncodedSemanticTokensPartially(IEnumerable<uint> data, string expression, string eol = "\n")
        {
            var lastLineNumber = 0u;
            var lastStartIdx = 0u;
            var decodedTokens = new List<ITokenTextSpan>();
            var encodedTokens = data.ToArray();
            for (var i = 0; i < encodedTokens.Length; i += SemanticTokensEncoder.SlotsPerToken)
            {
                var lineNumber = encodedTokens[i] + lastLineNumber;
                var startIdx = encodedTokens[i] == 0 ? encodedTokens[i + 1] + lastStartIdx : encodedTokens[i + 1];
                var length = encodedTokens[i + 2];
                var decodedTokenType = SemanticTokensEncoder.TestOnly_GetTokenTypeFromEncodedValue(encodedTokens[i + 3]);
                var (flatStartIndex, flatEndIndex) = CreateRange((int)lineNumber, (int)lineNumber, (int)startIdx, (int)(startIdx + length)).ConvertRangeToPositions(expression, eol, false);
                decodedTokens.Add(new TokenTextSpan(string.Empty, flatStartIndex, flatEndIndex, decodedTokenType, false));
                lastLineNumber = lineNumber;
                lastStartIdx = startIdx;
            }

            return decodedTokens;
        }

        public static int DetermineNumberOfLinesThatTokensAreSpreadAcross(SemanticTokensResponse response)
        {
            return response.Data.Where((_, index) => index % SemanticTokensEncoder.SlotsPerToken == 0).Distinct().Count();
        }

        public static Range CreateRange(int startLine, int endLine, int startLineCol, int endLineCol)
        {
            return new Range
            {
                Start = new Position
                {
                    Line = startLine,
                    Character = startLineCol
                },
                End = new Position
                {
                    Line = endLine,
                    Character = endLineCol
                }
            };
        }
    }
}
