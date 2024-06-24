// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Utils;
using static System.Net.Mime.MediaTypeNames;

namespace Microsoft.PowerFx.Syntax
{
    /// <summary>
    /// Options to control minification of the script.
    /// </summary>
    /// <param name="flags">Lexer Flags.</param>
    /// <param name="removeComments">Indicates if comments shall be removed from the script.</param>
    internal sealed record MinificationOptions(TexlLexer.Flags flags = TexlLexer.Flags.None, bool removeComments = false);

    internal static class ScriptMinification
    {
        /// <summary>
        /// Given the expression and list of tokens that make up the expression, generates the minified version of the given expression.
        /// </summary>
        /// <param name="text">Expression to minify.</param>
        /// <param name="tokens">List of tokens that make up the given expression.</param>
        /// <param name="options">Options for minification.</param>
        /// <returns>Minified Expression.</returns>
        public static string GetMinifiedScript(string text, List<Token> tokens, MinificationOptions options = null)
        {
            Contracts.AssertValue(text);
            Contracts.AssertValue(tokens);

            options ??= new MinificationOptions();  
            var stringBuilder = new StringBuilder();

            for (var i = 0; i < tokens.Count; i++)
            {
                var currentToken = tokens[i];
                if (currentToken.Kind == TokKind.Comment)
                {
                    if (!options.removeComments)
                    {
                        stringBuilder.Append(currentToken.Span.GetFragment(text));
                    }
                }
                else if (RequiresWhiteSpace(currentToken))
                {
                    stringBuilder.Append(" " + currentToken.Span.GetFragment(text) + " ");
                }
                else
                {
                    var tokenString = currentToken.Span.GetFragment(text);
                    var shouldNotTrim = IsStringPartOfALargerInterpolatedString(i - 1 >= 0 ? tokens[i - 1] : null, currentToken, i + 1 < tokens.Count ? tokens[i + 1] : null);
                    var newString = shouldNotTrim ? tokenString : tokenString.Trim();

                    stringBuilder.Append(newString);
                }
            }

            var result = stringBuilder.ToString();
            return result;
        }

        public static bool RequiresWhiteSpace(Token tk)
        {
            bool result;
            switch (tk.Kind)
            {
                case TokKind.True:
                case TokKind.False:
                case TokKind.In:
                case TokKind.Exactin:
                case TokKind.Parent:
                case TokKind.KeyAnd:
                case TokKind.KeyNot:
                case TokKind.KeyOr:
                case TokKind.As:
                    result = true;
                    break;
                default:
                    result = false;
                    break;
            }

            return result;
        }

        /// <summary>
        /// Determines if current token is string literal inside a larger interpolated string.
        /// </summary>
        /// <param name="precedingToken">A token before the current token.</param>
        /// <param name="currentToken">Current token that might be a string literal inside a larger interpolated string.</param>
        /// <param name="followingToken">A token after the current token.</param>
        /// <returns>True if current token represents a string literal inside a larger interpolated string or false otherwise.</returns>
        private static bool IsStringPartOfALargerInterpolatedString(Token precedingToken, Token currentToken, Token followingToken)
        {
            if (currentToken == null || currentToken.Kind != TokKind.StrLit)
            {
                return false;
            }

            // See if current token is preceded by start of the interpolated string or an end of an island
            // Example $"<current token>, }<current token>
            if (precedingToken != null && (precedingToken.Kind == TokKind.StrInterpStart || precedingToken.Kind == TokKind.IslandEnd))
            {
                return true;
            }

            // See if current token is followed by end of the interpolated string or the start of an island
            // Example <current token>", <current token>{
            if (followingToken != null && (followingToken.Kind == TokKind.StrInterpEnd || followingToken.Kind == TokKind.IslandStart))
            {
                return true;
            }

            return false;
        }
    }
}
