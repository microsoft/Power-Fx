// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Intellisense
{
    /// <summary>
    /// An entity with methods to compute enumeration of tokens in a expression with their start and end indices and token type.
    /// </summary>
    internal static class Tokenization
    {
        /// <summary>
        /// Collection of all token types that make up interpolated strings.
        /// </summary>
        private static readonly IReadOnlyCollection<TokenType> AllStringInterpolationRelatedTokenTypes = new TokenType[] { TokenType.StrInterpStart, TokenType.StrInterpEnd, TokenType.IslandStart, TokenType.IslandEnd };

        /// <summary>
        /// Returns an ordered enumeration of token text spans in a expression rule with their start and end indices and token type. Orders token by default comparer.
        /// </summary>
        /// <param name="expression">Expression which would be tokenized.</param>
        /// <param name="binding">Binding which would be used to tokenize the operation and determine the type of each token.</param>
        /// <param name="comments">Colllection of comment tokens extracted from the given expression.</param>
        /// <returns>Ordered collection of tokens.</returns>
        internal static IEnumerable<ITokenTextSpan> Tokenize(string expression, TexlBinding binding, IEnumerable<CommentToken> comments = null)
        {
            return Tokenize(expression, binding, comments, new TokenTextSpanComparer(), true);
        }

        /// <summary>
        /// Returns an enumeration of token text spans in a expression rule with their start and end indices and token type.
        /// </summary>
        /// <param name="expression">Expression which would be tokenized.</param>
        /// <param name="binding">Binding which would be used to tokenize the operation and determine the type of each token.</param>
        /// <param name="comments">Colllection of comment tokens extracted from the given expression.</param>
        /// <param name="comparer">optional comparer to sort tokens.</param>
        /// <param name="allowTokenHiding">Optional: This flag determines whether to compute the value of a CanBeHidden property for tokens generated out of first names present in the expression.</param>
        /// <param name="tokenTypesToSkip">Options: Collection of token types to potentially skip in the final result. Tokens with these token types may or may not be skipped depending on their types.</param>
        /// <returns> Enumerable of tokens. Tokens are ordered only if comparer is provided.</returns>
        // allowTokenHiding flag is off by default as we don't want to compute hiddenness of the tokens for the new formula bar as we are not going to support that.
        // However, we want to keep this for old formula bar as long as it exists and once we consume the changes in Canvas App backend,
        // we would update the tokenization logic behind old formula bar to use this one instead and allowTokenHiding would be true for old formula bar.
        internal static IEnumerable<ITokenTextSpan> Tokenize(string expression, TexlBinding binding, IEnumerable<CommentToken> comments = null, IComparer<ITokenTextSpan> comparer = null, bool allowTokenHiding = false, IReadOnlyCollection<TokenType> tokenTypesToSkip = null)
        {
            var tokens = new List<ITokenTextSpan>();
            if (expression == null || binding == null)
            {
                return tokens;
            }

            Span span;
            var compilerGeneratedCallNodes = new HashSet<int>();

            foreach (var firstName in binding.GetFirstNames())
            {
                var textSpan = firstName.Node.GetCompleteSpan();
                span = new Span(textSpan.Min, textSpan.Lim);
                TokenType type = MapBindKindToTokenType(firstName, binding);

                // Try to set the canHide flag if the FirstName type is Enum
                var canHide = allowTokenHiding && type == TokenType.Enum && CanHideLeftHandSideOfDottedName(firstName, binding);

                // If the first name can be hidden, Create and Add a token for the dot.
                if (canHide && expression.Length > span.Lim && (TexlLexer.PunctuatorDot + TexlLexer.PunctuatorBang).IndexOf(expression.Substring(span.Lim, 1), StringComparison.Ordinal) >= 0)
                {
                    tokens.AddTokenIfPossible(new TokenTextSpan(TexlLexer.PunctuatorDot, span.Lim, span.Lim + 1, TokenType.Punctuator, canHide: true), tokenTypesToSkip);
                }

                // Add a token for the first name.
                tokens.AddTokenIfPossible(new TokenTextSpan(firstName.Name, span.Min, span.Lim, type, canHide), tokenTypesToSkip);
            }

            foreach (var dottedName in binding.GetDottedNames())
            {
                DType type = binding.GetType(dottedName.Node);

                TokenType dottedNameType = TokenType.DottedNamePart;
                if (type.IsControl)
                {
                    dottedNameType = TokenType.Control;
                }

                if (dottedNameType.ShouldBeSkipped(tokenTypesToSkip))
                {
                    continue;
                }

                span = dottedName.Node.GetTextSpan();
                tokens.Add(new TokenTextSpan(dottedName.Node.Right.Name, span.Min + 1, span.Lim, dottedNameType, /* canHide */ false));
            }

            // String interpolations
            ExtractTokensFromStringInterpolationNodesList(tokens, binding, compilerGeneratedCallNodes, tokenTypesToSkip);

            foreach (var call in binding.GetCalls())
            {
                // Skip the compiler generated nodes as they don't appear in the actual source code and should not show up as a token
                if (compilerGeneratedCallNodes.Contains(call.Node.Id))
                {
                    continue;
                }

                int spanStart = call.Node.Head.Token.Span.Min;
                int spanEnd = call.Node.Head.Token.Span.Lim; // span.Lim

                // Close the Function token at the end of the call word instead of the right closing parenthesis.
                Contracts.Assert(spanEnd >= spanStart, "The Span End value should be larger than the Span Start.");
                tokens.AddTokenIfPossible(new TokenTextSpan(call.Node.Head.Name, spanStart, spanEnd, TokenType.Function, /* canHide */ false), tokenTypesToSkip, expression);

                // Parse the call arguments delimiters
                if (call.Node.Args.Delimiters != null && !TokenType.Delimiter.ShouldBeSkipped(tokenTypesToSkip))
                {
                    string delimiterName = null;
                    foreach (var delimiter in call.Node.Args.Delimiters)
                    {
                        delimiterName = call.Node.Head.Name + "_delimeter";
                        tokens.Add(new TokenTextSpan(delimiterName, delimiter.Span.Min, delimiter.Span.Lim, TokenType.Delimiter, /* canHide */ false));
                    }
                }
            }

            // If comment token type can be skipped, then no need to process comment tokens
            if (comments != null && !TokenType.Comment.ShouldBeSkipped(tokenTypesToSkip))
            {
                foreach (var cm in comments)
                {
                    span = new Span(cm.Span.Min, cm.Span.Lim);
                    tokens.Add(new TokenTextSpan(TexlLexer.PunctuatorBlockComment, span.Min, span.Lim, TokenType.Comment, false));
                }
            }

            foreach (var info in binding.GetControlKeywordInfos())
            {
                var textSpan = info.Node.GetCompleteSpan();
                span = new Span(textSpan.Min, textSpan.Lim);
                TokenType type = MapNodeKindToTokenType(info.Node.Kind);

                // Add a token for the keyword.
                tokens.AddTokenIfPossible(new TokenTextSpan(info.Name, span.Min, span.Lim, type, false), tokenTypesToSkip);
            }

            // Binary Operators
            ExtractTokensFromNodesList(tokens, binding.GetBinaryOperators(), TokenizerConstants.BinaryOp, tokenTypesToSkip);

            // Variadic Operators
            ExtractTokensFromNodesList(tokens, binding.GetVariadicOperators(), TokenizerConstants.VariadicOp, tokenTypesToSkip);

            // Boolean Literals
            ExtractTokensFromNodesList(tokens, binding.GetBooleanLiterals(), TokenizerConstants.BooleanLiteral, tokenTypesToSkip);

            // Numeric Literals
            ExtractTokensFromNodesList(tokens, binding.GetNumericLiterals(), TokenizerConstants.NumericLiteral, tokenTypesToSkip);

            // Decimal Literal Nodes
            ExtractTokensFromNodesList(tokens, binding.GetDecimalLiterals(), TokenizerConstants.DecimalLiteral, tokenTypesToSkip);

            // String Literals
            ExtractTokensFromNodesList(tokens, binding.GetStringLiterals(), TokenizerConstants.StringLiteral, tokenTypesToSkip);

            // Unary Operators ie Currently just used for Percentages
            ExtractTokensFromNodesList(tokens, binding.GetUnaryOperators(), TokenizerConstants.UnaryOp, tokenTypesToSkip);

            if (comparer != null)
            {
                tokens.Sort(comparer);
            }

            return tokens;
        }

        /// <summary>
        /// Determines if left-hand side of the dotted name can be hidden to reduce complexity when the right-hand side of a dotted name is unique.
        /// When there is a conflict, we need to show the fully qualified name.
        /// </summary>
        /// <param name="nameInfo">NameInfo.</param>
        /// <param name="binding">TexlBinding.</param>
        /// <returns>True if the left-hand side of the dotted name can be hidden or false otherwise.</returns>
        private static bool CanHideLeftHandSideOfDottedName(NameInfo nameInfo, TexlBinding binding)
        {
            DottedNameNode dottedNameParent;
            if (binding?.Document?.GlobalScope == null || nameInfo?.Node?.Parent == null || (dottedNameParent = nameInfo.Node.Parent.AsDottedName()) == null)
            {
                return false;
            }

            return binding.Document.GlobalScope.IsNameAvailable(dottedNameParent.Right.Name.Value);
        }

        /// <summary>
        /// Converts given string interpoation nodes to equivalent tokens.
        /// </summary>
        /// <param name="tokens">Collection in which tokens created from th given nodes would be stored.</param>
        /// <param name="binding">Binding which would be used to tokenize the operation and determine the type of each token.</param>
        /// <param name="compilerGeneratedNodes">Collection that contains ids of compiler generate nodes.</param>
        /// <param name="tokenTypesToSkip">Options: Collection of token types to potentially skip in the final result. Tokens with these token types may or may not be skipped depending on their types.</param>
        private static void ExtractTokensFromStringInterpolationNodesList(ICollection<ITokenTextSpan> tokens, TexlBinding binding, ICollection<int> compilerGeneratedNodes, IReadOnlyCollection<TokenType> tokenTypesToSkip = null)
        {
            Contracts.AssertValue(tokens);

            // When all string interpolation token types should be skipped
            // There's no point in doing expensive computation towards the end of the function (going through all source tokens)
            // In this case, just go through string interpolated nodes and rememeber/track the compiler generated nodes for these nodes
            // We are bailing out early but we still need to track the Concate call nodes generated by compiler for string interpolation
            // So that these generated call nodes do no show up as Function tokens if TokenType.Function was not skipped
            if (AllStringInterpolationRelatedTokenTypes.All(tokenType => tokenType.ShouldBeSkipped(tokenTypesToSkip)))
            {
                foreach (var stringInterpolationNode in binding.GetStringInterpolations())
                {
                    TrackCompilerGeneratedNodes(compilerGeneratedNodes, stringInterpolationNode, binding);
                }

                return;
            }

            // Nested string interpolated nodes start/end before the outermost string interpolated node (terminated/unterminated)
            // This means that spans of nested string interpolated nodes overlap with the root (outermost) string interpolated node
            // Outermost string interpolated node starts before any nested string interpolated nodes or have higher depth
            // Processing all the nodes captured in binding from to left to right in a sorted order of their start indexes.
            // Allows us to drop overlapping nodes, thus avoiding duplicated tokens
            // GetCompleteSpan() accurately computes span even if the interpolated string was unterminated
            var nodes = binding.GetStringInterpolations().OrderBy(node => node.GetCompleteSpan().Min);

            Contracts.AssertValue(nodes);

            TexlNode currStringInterpNode = null;
            Span currentNodeSpan = null;
            foreach (var node in nodes)
            {
                // As of now, we generate transient nodes for strinterpstart node that do not appear in source
                TrackCompilerGeneratedNodes(compilerGeneratedNodes, node, binding);

                var nodeSpan = node.GetCompleteSpan();

                // Only process outermost string interpolated node
                // Overlapping nodes would be considered when processing outermost node as we go through all the tokens
                // that make up the outermost node. The tokens for nested string interpolation nodes are a subset of tokens of outermost node
                // which results into duplicate tokens
                // Multiple outermost nodes do not overlap
                if (currStringInterpNode == null || currentNodeSpan.Lim <= nodeSpan.Min)
                {
                    currentNodeSpan = nodeSpan;
                    currStringInterpNode = node;
                    foreach (var token in currStringInterpNode.SourceList.Tokens)
                    {
                        var span = token.Span;
                        switch (token.Kind)
                        {
                            case TokKind.StrInterpStart:
                                tokens.AddTokenIfPossible(new TokenTextSpan(TokenizerConstants.StringInterpolationStart, span.Min, span.Lim, TokenType.StrInterpStart, false), tokenTypesToSkip);
                                break;
                            case TokKind.StrInterpEnd:
                                tokens.AddTokenIfPossible(new TokenTextSpan(TokenizerConstants.StringInterpolationEnd, span.Min, span.Lim, TokenType.StrInterpEnd, false), tokenTypesToSkip);
                                break;
                            case TokKind.IslandStart:
                                tokens.AddTokenIfPossible(new TokenTextSpan(TokenizerConstants.IslandStart, span.Min, span.Lim, TokenType.IslandStart, false), tokenTypesToSkip);
                                break;
                            case TokKind.IslandEnd:
                                tokens.AddTokenIfPossible(new TokenTextSpan(TokenizerConstants.IslandEnd, span.Min, span.Lim, TokenType.IslandEnd, false), tokenTypesToSkip);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks and tracks if a compiler generated transient node for the given node. 
        /// Currently, Transient Nodes are only generated for StrInterpNode nodes.
        /// Update the logic in this file to reuse this function as more transient nodes are generated for other node types.
        /// Since Transient nodes don't appear in the source, they should not be treated as tokens.
        /// </summary>
        /// <param name="compilerGeneratedNodes">Collection that contains ids of compiler generate nodes.</param>
        /// <param name="node">Node for which transient node may have been generated during binding.</param>
        /// <param name="binding">Binding which would be used to tokenize the operation and determine the type of each token.</param>
        private static void TrackCompilerGeneratedNodes(ICollection<int> compilerGeneratedNodes, TexlNode node, TexlBinding binding)
        {
            var compilerGeneratedNode = binding.GetCompilerGeneratedCallNode(node);
            if (compilerGeneratedNode != null)
            {
                compilerGeneratedNodes.Add(compilerGeneratedNode.Id);
            }
        }

        /// <summary>
        /// Converts given nodes into the tokens and stories them into the provided tokens list. Each node is mapped to an equivalent token based on its Kind.
        /// </summary>
        /// <param name="tokens">Collection in which tokens created from th given nodes would be stored.</param>
        /// <param name="nodes">Collection of nodes that would be converted to equivalent tokens.</param>
        /// <param name="name">Name for each generated token.</param>
        /// <param name="tokenTypesToSkip">Optional: Token Types to be skipped.</param>
        private static void ExtractTokensFromNodesList(ICollection<ITokenTextSpan> tokens, IEnumerable<TexlNode> nodes, string name, IReadOnlyCollection<TokenType> tokenTypesToSkip = null)
        {
            Span span;
            foreach (var node in nodes)
            {
                var tokenType = MapNodeKindToTokenType(node.Kind);

                // All the nodes are of same TokenType and none of them are of type TokenType.Function
                // It is sufficient to check if the token type should be skipped or not
                // And if yes then bail out early
                if (tokenType.ShouldBeSkipped(tokenTypesToSkip))
                {
                    return;
                }

                span = node.GetTextSpan();
                tokens.Add(new TokenTextSpan(name, span.Min, span.Lim, tokenType, false));
            }
        }

        /// <summary>
        /// Maps the given bind kind to TokenType (BinKind -> TokenType).
        /// </summary>
        /// <param name="kind">BindKind.</param>
        /// <returns>TokenType.</returns>
        private static TokenType MapBindKindToTokenType(BindKind kind)
        {
            switch (kind)
            {
                case BindKind.Alias:
                    return TokenType.Alias;
                case BindKind.ScopeVariable:
                    return TokenType.ScopeVariable;
                case BindKind.Control:
                    return TokenType.Control;
                case BindKind.Data:
                case BindKind.ScopeCollection:
                    return TokenType.Data;
                case BindKind.Enum:
                case BindKind.OptionSet:
                case BindKind.View:
                    return TokenType.Enum;
                case BindKind.ThisItem:
                case BindKind.DeprecatedImplicitThisItem:
                    return TokenType.ThisItem;
                default:
                    return TokenType.Unknown;
            }
        }

        private static TokenType MapBindKindToTokenType(NameInfo nameInfo, TexlBinding binding)
        {
            var dType = binding.GetType(nameInfo.Node);
            if (dType?.IsRecord ?? false)
            {
                return TokenType.Record;
            }
            else if (dType?.IsTable ?? false)
            {
                return TokenType.Data;
            }

            return MapBindKindToTokenType(nameInfo.Kind);
        }

        /// <summary>
        /// Maps the given node kind to the token type (NodeKind -> TokenType).
        /// </summary>
        /// <param name="kind">NodeKind.</param>
        /// <returns>TokenType.</returns>
        private static TokenType MapNodeKindToTokenType(NodeKind kind)
        {
            switch (kind)
            {
                case NodeKind.BinaryOp:
                    return TokenType.BinaryOp;

                // Currently the only Unary op used for tokenizing is percentages which, color-wise, can be handled as NumLits
                case NodeKind.UnaryOp:
                    return TokenType.NumLit;
                case NodeKind.VariadicOp:
                    return TokenType.VariadicOp;
                case NodeKind.BoolLit:
                    return TokenType.BoolLit;
                case NodeKind.NumLit:
                    return TokenType.NumLit;
                case NodeKind.DecLit:
                    return TokenType.DecLit;
                case NodeKind.StrLit:
                    return TokenType.StrLit;
                case NodeKind.Self:
                    return TokenType.Self;
                case NodeKind.Parent:
                    return TokenType.Parent;
                default:
                    return TokenType.Unknown;
            }
        }

        /// <summary>
        /// Adds token to the given list of tokens if it is feasible (token should not be skipped).
        /// </summary>
        /// <param name="tokens">List of tokens to which given token might be added.</param>
        /// <param name="token">Token to be added if feasible.</param>
        /// <param name="tokenTypesToSkip">Optional: Token types to be skipped.</param>
        /// <param name="expression">Optional: Expression.</param>
        private static void AddTokenIfPossible(this ICollection<ITokenTextSpan> tokens, ITokenTextSpan token, IReadOnlyCollection<TokenType> tokenTypesToSkip = null, string expression = null)
        {
            if (token != null && !ShouldBeSkipped(token, tokenTypesToSkip, expression))
            {
                tokens.Add(token);
            }
        }

        /// <summary>
        /// Determines where the given token type should be skipped or not.
        /// </summary>
        /// <param name="tokenType">Token type to be skipped.</param>
        /// <param name="tokenTypesToSkip">Optional: Token types to be skipped.</param>
        /// <returns>True if this token type should be skipped or false otherwise.</returns>
        private static bool ShouldBeSkipped(this TokenType tokenType, IReadOnlyCollection<TokenType> tokenTypesToSkip = null)
        {
            return tokenTypesToSkip != null && tokenTypesToSkip.Contains(tokenType);
        }

        /// <summary>
        /// Determines where the given token should be skipped or not.
        /// </summary>
        /// <param name="token">Token to be skipped.</param>
        /// <param name="tokenTypesToSkip">Optional: Token types to be skipped.</param>
        /// <param name="expression">Optional: Expression.</param>
        /// <returns>True if this token should be skipped or false otherwise.</returns>
        private static bool ShouldBeSkipped(ITokenTextSpan token, IReadOnlyCollection<TokenType> tokenTypesToSkip = null, string expression = null)
        {
            var shouldBeSkippedBasedOnTokenType = token.TokenType.ShouldBeSkipped(tokenTypesToSkip);
            
            // Client requesting to skip function type means that it can successfully recognize function tokens that are immediately 
            // followed by open paren (
            // In that case, "only" skip function tokens that are immediately followed by open paren
            // For example, Max in "Max(1,2,3)" would be skipped while Max in "Max (1,2,3)" would not be skipped as there's a space between 
            // Max and open paren (
            // Function name immediately followed by open parent is the most common case
            // Thus, Doing this drastically reduces the number of tokens we have to send back to client
            // thus increasing the performance of semantic tokenization
            if (shouldBeSkippedBasedOnTokenType && token.TokenType == TokenType.Function)
            {
                return expression != null && token.EndIndex < expression.Length && expression[token.EndIndex].ToString() == TexlLexer.PunctuatorParenOpen;
            }

            return shouldBeSkippedBasedOnTokenType;
        }
    }
}
