// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Intellisense
{
    /// <summary>
    /// An entity with methods to compute ordered or unordered enumeration enumeration of token text spans in a expression rule with their start and end indices and token type.
    /// </summary>
    internal static class Tokenization
    {
        /// <summary>
        /// Returns an ordered enumeration of token text spans in a expression rule with their start and end indices and token type. Orders token by default comparer.
        /// </summary>
        /// <param name="expression">Expression which would be tokenized.</param>
        /// <param name="binding">Binding which would be used to tokenize the operation and determine the type of each token.</param>
        /// <param name="comments">Colllection of comment tokens extracted from the given expression.</param>
        /// <returns>Ordered collection of tokens.</returns>
        internal static ICollection<ITokenTextSpan> Tokenize(string expression, TexlBinding binding, IEnumerable<CommentToken> comments = null)
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
        /// <param name="computeHiddenTokens">Optional flag to indicate whether to compute whether token can be hidden or not.</param>
        /// <returns>Ordered or unordered collection of tokens.</returns>
        internal static ICollection<ITokenTextSpan> Tokenize(string expression, TexlBinding binding, IEnumerable<CommentToken> comments = null, IComparer<ITokenTextSpan> comparer = null, bool computeHiddenTokens = false)
        {
            var tokens = new List<ITokenTextSpan>();
            if (binding == null)
            {
                return tokens;
            }

            Span span;
            var complilerGeneratedCallNodes = new HashSet<int>();

            foreach (var firstName in binding.GetFirstNames())
            {
                var textSpan = firstName.Node.GetCompleteSpan();
                span = new Span(textSpan.Min, textSpan.Lim);
                TokenType type = MapBindKindToTokenType(firstName.Kind);

                // Try to set the canHide flag if the FirstName type is Enum
                var canHide = computeHiddenTokens && CanHideNamespaceOfDottedName(firstName, binding);

                // If the first name can be hidden, Create and Add a token for the dot.
                if (canHide && expression.Length > span.Lim && (TexlLexer.PunctuatorDot + TexlLexer.PunctuatorBang).IndexOf(expression.Substring(span.Lim, 1), StringComparison.Ordinal) >= 0)
                {
                    tokens.Add(new TokenTextSpan(TexlLexer.PunctuatorDot, span.Lim, span.Lim + 1, TokenType.Punctuator, canHide: true));
                }

                // Add a token for the first name.
                tokens.Add(new TokenTextSpan(firstName.Name, span.Min, span.Lim, type, canHide));
            }

            foreach (var dottedName in binding.GetDottedNames())
            {
                DType type = binding.GetType(dottedName.Node);

                TokenType dottedNameType = TokenType.DottedNamePart;
                if (type.IsControl)
                {
                    dottedNameType = TokenType.Control;
                }

                span = dottedName.Node.GetTextSpan();
                tokens.Add(new TokenTextSpan(dottedName.Node.Right.Name, span.Min + 1, span.Lim, dottedNameType, /* canHide */ false));
            }

            // String interpolations
            ExtractTokensFromStringInterpolationNodesList(tokens, binding, complilerGeneratedCallNodes);

            foreach (var call in binding.GetCalls())
            {
                // Skip the compiler generated nodes as they don't appear in the actual source code and should not show up as a token
                if (complilerGeneratedCallNodes.Contains(call.Node.Id))
                {
                    continue;
                }

                int spanStart = call.Node.Head.Token.Span.Min;
                int spanEnd = call.Node.Head.Token.Span.Lim; // span.Lim

                // Close the Function token at the end of the call word instead of the right closing parenthesis.
                Contracts.Assert(spanEnd >= spanStart, "The Span End value should be larger than the Span Start.");
                tokens.Add(new TokenTextSpan(call.Node.Head.Name, spanStart, spanEnd, TokenType.Function, /* canHide */ false));

                // Parse the call arguments delimiters
                if (call.Node.Args.Delimiters != null)
                {
                    string delimiterName = null;
                    foreach (var delimiter in call.Node.Args.Delimiters)
                    {
                        delimiterName = call.Node.Head.Name + "_delimeter";
                        tokens.Add(new TokenTextSpan(delimiterName, delimiter.Span.Min, delimiter.Span.Lim, TokenType.Delimiter, /* canHide */ false));
                    }
                }
            }

            if (comments != null)
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
                tokens.Add(new TokenTextSpan(info.Name, span.Min, span.Lim, type, false));
            }

            // Binary Operators
            ExtractTokensFromNodesList(tokens, binding.GetBinaryOperators(), TokenizerConstants.BinaryOp);

            // Variadic Operators
            ExtractTokensFromNodesList(tokens, binding.GetVariadicOperators(), TokenizerConstants.VariadicOp);

            // Boolean Literals
            ExtractTokensFromNodesList(tokens, binding.GetBooleanLiterals(), TokenizerConstants.BooleanLiteral);

            // Numeric Literals
            ExtractTokensFromNodesList(tokens, binding.GetNumericLiterals(), TokenizerConstants.NumericLiteral);

            // Decimal Literal Nodes
            ExtractTokensFromNodesList(tokens, binding.GetDecimalLiterals(), TokenizerConstants.DecimalLiteral);

            // String Literals
            ExtractTokensFromNodesList(tokens, binding.GetStringLiterals(), TokenizerConstants.StringLiteral);

            // Unary Operators ie Currently just used for Percentages
            ExtractTokensFromNodesList(tokens, binding.GetUnaryOperators(), TokenizerConstants.UnaryOp);

            if (comparer != null)
            {
                tokens.Sort(comparer);
            }

            return tokens;
        }

        /// <summary>
        /// Determines if namespaces can be hidden to reduce complexity when the right-hand side of a dotted name is unique.
        /// When there is a conflict, we need to show the fully qualified name.
        /// </summary>
        /// <param name="nameInfo">NameInfo.</param>
        /// <param name="binding">TexlBinding.</param>
        /// <returns>True if the namespace can be hidden or false otherwise.</returns>
        private static bool CanHideNamespaceOfDottedName(NameInfo nameInfo, TexlBinding binding)
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
        private static void ExtractTokensFromStringInterpolationNodesList(ICollection<ITokenTextSpan> tokens, TexlBinding binding, ICollection<int> compilerGeneratedNodes)
        {
            Contracts.AssertValue(tokens);

            var nodes = binding.GetStringInterpolations();
            Contracts.AssertValue(nodes);

            foreach (var node in nodes)
            {
                // As of now, we generate transient nodes for strinterpstart node that do not appear in source
                TrackCompilerGeneratedNodes(compilerGeneratedNodes, node, binding);
                foreach (var token in node.SourceList.Tokens)
                {
                    var span = token.Span;
                    switch (token.Kind)
                    {
                        case TokKind.StrInterpStart:
                            tokens.Add(new TokenTextSpan(TokenizerConstants.StringInterpolationStart, span.Min, span.Lim, TokenType.StrInterpStart, false));
                            break;
                        case TokKind.StrInterpEnd:
                            tokens.Add(new TokenTextSpan(TokenizerConstants.StringInterpolationEnd, span.Min, span.Lim, TokenType.StrInterpEnd, false));
                            break;
                        case TokKind.IslandStart:
                            tokens.Add(new TokenTextSpan(TokenizerConstants.IslandStart, span.Min, span.Lim, TokenType.IslandStart, false));
                            break;
                        case TokKind.IslandEnd:
                            tokens.Add(new TokenTextSpan(TokenizerConstants.IslandEnd, span.Min, span.Lim, TokenType.IslandEnd, false));
                            break;
                        default:
                            break;
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
        private static void ExtractTokensFromNodesList(ICollection<ITokenTextSpan> tokens, IEnumerable<TexlNode> nodes, string name)
        {
            Span span;
            foreach (var node in nodes)
            {
                span = node.GetTextSpan();
                tokens.Add(new TokenTextSpan(name, span.Min, span.Lim, MapNodeKindToTokenType(node.Kind), false));
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
    }
}
