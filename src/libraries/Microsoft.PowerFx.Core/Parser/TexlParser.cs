// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Syntax.SourceInformation;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Parser
{
    internal sealed class TexlParser
    {
        [Flags]
        public enum Flags
        {
            None = 0,

            // When specified, expression chaining is allowed (e.g. in the context of behavior rules).
            EnableExpressionChaining = 1 << 0,

            // When specified, this is a named formula to be parsed. Mutually exclusive to EnableExpressionChaining.
            NamedFormulas = 1 << 1
        }

        private readonly TokenCursor _curs;
        private readonly Flags _flags;
        private List<TexlError> _errors;

        // Nodes are assigned an integer id that is used to index into arrays later.
        private int _idNext;

        // Track the parsing depth and enforce a maximum, to avoid excessive recursion.
        private int _depth;
        private const int MaxAllowedExpressionDepth = 50;

        private readonly List<CommentToken> _comments = new List<CommentToken>();
        private SourceList _before;
        private SourceList _after;

        // Represents temporary extra trivia, for when a parsing method
        // had to parse tailing trivia to do 1-lookahead. Will be
        // collected by the next call to ParseTrivia.
        private ITexlSource _extraTrivia;

        private TexlParser(Token[] tokens, Flags flags)
        {
            Contracts.AssertValue(tokens);

            _depth = 0;
            _curs = new TokenCursor(tokens, flags);
            _flags = flags;
        }

        // Parse the script
        // Parsing strips out parens used to establish precedence, but these may be helpful to the
        // caller, so precedenceTokens provide a list of stripped tokens.
        internal static ParseResult ParseScript(string script, ILanguageSettings loc = null, Flags flags = Flags.None)
        {
            Contracts.AssertValue(script);
            Contracts.AssertValueOrNull(loc);

            var tokens = TokenizeScript(script, loc, flags);
            var parser = new TexlParser(tokens, flags);
            List<TexlError> errors = null;
            var parsetree = parser.Parse(ref errors);

            return new ParseResult(parsetree, errors, errors?.Any() ?? false, parser._comments, parser._before, parser._after);
        }

        public static ParseFormulasResult ParseFormulasScript(string script, ILanguageSettings loc = null)
        {
            Contracts.AssertValue(script);
            Contracts.AssertValueOrNull(loc);

            var formulaTokens = TokenizeScript(script, loc, Flags.NamedFormulas);
            var parser = new TexlParser(formulaTokens, Flags.NamedFormulas);

            return parser.ParseFormulas(script);
        }

        private ParseFormulasResult ParseFormulas(string script)
        {
            var namedFormulas = new List<NamedFormula>();
            ParseTrivia();
            var offset = _curs.CurrentCharIndex;
            while (_curs.TokCur.Kind != TokKind.Eof)
            {
                // Verify identifier
                var thisIdentifier = TokEat(TokKind.Ident);
         
                if (thisIdentifier != null)
                {
                    ParseTrivia();

                    // Verify "="
                    var thisEq = TokEat(TokKind.Equ);
                    if (thisEq != null)
                    {
                        ParseTrivia();

                        // Extract expression
                        while (_curs.TidCur != TokKind.Semicolon)
                        {
                            // Check if we're at EOF before a semicolon is found
                            if (_curs.TidCur == TokKind.Eof)
                            {
                                CreateError(_curs.TokCur, TexlStrings.ErrNamedFormula_MissingSemicolon);
                                return new ParseFormulasResult(namedFormulas, _errors);
                            }

                            // Parse expression
                            var result = ParseExpr(Precedence.None);

                            namedFormulas.Add(new NamedFormula(thisIdentifier.As<IdentToken>(), result, offset));
                        }

                        _curs.TokMove();
                        offset = _curs.CurrentCharIndex;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }

                ParseTrivia();
            }

            return new ParseFormulasResult(namedFormulas, _errors);
        }

        private static Token[] TokenizeScript(string script, ILanguageSettings loc = null, Flags flags = Flags.None)
        {
            Contracts.AssertValue(script);
            Contracts.AssertValueOrNull(loc);

            var lexerFlags = TexlLexer.Flags.None;

            if (loc == null)
            {
                return TexlLexer.LocalizedInstance.LexSource(script, lexerFlags);
            }

            return TexlLexer.NewInstance(loc).LexSource(script, lexerFlags);
        }

        private TexlNode Parse(ref List<TexlError> errors)
        {
            Contracts.AssertValueOrNull(errors);

            _errors = errors;
            TexlNode node;
            var firstToken = _curs.TokCur;
            _before = new SourceList(ParseTrivia());

            if (_curs.TidCur == TokKind.Eof)
            {
                if (firstToken.Kind == TokKind.Comment && firstToken.As<CommentToken>().IsOpenBlock)
                {
                    // This provides an error message for when a block comment missing a closing '*/' is the only token in the formula bar
                    PostBlockCommentMissingClosingError();
                    errors = _errors;
                }

                node = new BlankNode(ref _idNext, _curs.TokCur);
            }
            else
            {
                node = ParseExpr(Precedence.None);
                if (_curs.TidCur != TokKind.Eof)
                {
                    PostError(_curs.TokCur, TexlStrings.ErrBadToken);
                }

                _after = _after == null ? new SourceList(ParseTrivia()) : new SourceList(new SpreadSource(_after.Sources), new SpreadSource(ParseTrivia()));

                // This checks for and provides an error message for any block comments missing a closing '*/'
                PostBlockCommentMissingClosingError();

                errors = _errors;
            }

            // The top node (of the parse tree) should end up with the biggest id. We use this fact when binding.
            Contracts.Assert(node.Id == _idNext - 1);

            return node;
        }

        private void PostBlockCommentMissingClosingError()
        {
            var openBlockComment = _comments.LastOrDefault(cm => cm.IsOpenBlock == true);

            if (openBlockComment != null)
            {
                PostError(openBlockComment, TexlStrings.ErrMissingEndOfBlockComment);
            }
        }

        private ITexlSource ParseTrivia(TokenCursor cursor = null)
        {
            cursor = cursor ?? _curs;
            var sources = new List<ITexlSource>();

            if (_extraTrivia != null)
            {
                sources.Add(_extraTrivia);
                _extraTrivia = null;
            }

            bool triviaFound;
            do
            {
                triviaFound = false;
                var tokens = cursor.SkipWhitespace();
                if (tokens.Any())
                {
                    sources.Add(new WhitespaceSource(tokens));
                    triviaFound = true;
                }

                if (cursor.TidCur == TokKind.Comment)
                {
                    var comment = cursor.TokMove().As<CommentToken>();
                    sources.Add(new TokenSource(comment));

                    if (comment.IsOpenBlock)
                    {
                        PostError(comment, TexlStrings.ErrMissingEndOfBlockComment);
                    }

                    _comments.Add(comment);
                    triviaFound = true;
                }
            }
            while (triviaFound);

            if (sources.Count() == 1)
            {
                return sources.Single();
            }
            else
            {
                return new SpreadSource(sources);
            }
        }

        private void AddExtraTrivia(ITexlSource trivia)
        {
            if (_extraTrivia == null)
            {
                _extraTrivia = trivia;
            }
            else
            {
                _extraTrivia = new SpreadSource(_extraTrivia, trivia);
            }
        }

        // Parses the next (maximal) expression with precedence >= precMin.
        private TexlNode ParseExpr(Precedence precMin, TexlNode node = null)
        {
            // ParseOperand may accept PrefixUnary and higher, so ParseExpr should never be called
            // with precMin > Precedence.PrefixUnary - it will not correctly handle those cases.
            Contracts.Assert(precMin >= Precedence.None && precMin <= Precedence.PrefixUnary);

            try
            {
                // The parser is recursive. Deeply nested invocations (over 200 deep) and other
                // intentionally miscrafted rules can throw it off, causing stack overflows.
                // Ensure the product doesn't crash in such situations, but instead post
                // corresponding parse errors.
                if (node == null)
                {
                    if (++_depth > MaxAllowedExpressionDepth)
                    {
                        return CreateError(_curs.TokMove(), TexlStrings.ErrRuleNestedTooDeeply);
                    }

                    // Get the left operand.
                    node = ParseOperand();
                }

                // Process operators and right operands as long as the precedence bound is satisfied.
                for (; ;)
                {
                    var leftTrivia = ParseTrivia();
                    Contracts.AssertValue(node);
                    Token tok;
                    TexlNode right;
                    Identifier identifier;
                    ITexlSource rightTrivia;
                    switch (_curs.TidCur)
                    {
                        case TokKind.PercentSign:
                            Contracts.Assert(precMin <= Precedence.PostfixUnary);
                            tok = _curs.TokMove();
                            node = new UnaryOpNode(
                                ref _idNext,
                                tok,
                                new SourceList(new NodeSource(node), new TokenSource(tok)),
                                UnaryOp.Percent,
                                node);
                            break;
                        case TokKind.Dot:
                        case TokKind.Bang:
                            Contracts.Assert(precMin <= Precedence.Primary);
                            if (node is DottedNameNode leftDotted && leftDotted.Token.Kind != _curs.TidCur && leftDotted.Token.Kind != TokKind.BracketOpen)
                            {
                                // Can't mix and match separators. E.g. A.B!C is invalid.
                                goto case TokKind.False;
                            }

                            tok = _curs.TokMove();
                            rightTrivia = ParseTrivia();
                            identifier = ParseIdentifier();
                            node = new DottedNameNode(
                                ref _idNext,
                                tok,
                                new SourceList(
                                    new NodeSource(node),
                                    new TokenSource(tok),
                                    new SpreadSource(rightTrivia),
                                    new IdentifierSource(identifier)),
                                node,
                                identifier,
                                null);
                            if (node.Depth > MaxAllowedExpressionDepth)
                            {
                                return CreateError(node.Token, TexlStrings.ErrRuleNestedTooDeeply);
                            }

                            break;

                        case TokKind.Caret:
                            Contracts.Assert(precMin <= Precedence.Power);
                            node = ParseBinary(node, leftTrivia, BinaryOp.Power, Precedence.PrefixUnary);
                            break;

                        case TokKind.Mul:
                            if (precMin > Precedence.Mul)
                            {
                                goto default;
                            }

                            node = ParseBinary(node, leftTrivia, BinaryOp.Mul, Precedence.Mul + 1);
                            break;

                        case TokKind.Div:
                            if (precMin > Precedence.Mul)
                            {
                                goto default;
                            }

                            tok = _curs.TokMove();
                            rightTrivia = ParseTrivia();
                            right = ParseExpr(Precedence.Mul + 1);
                            node = MakeBinary(BinaryOp.Div, node, leftTrivia, tok, rightTrivia, right);
                            break;

                        case TokKind.Sub:
                            if (precMin > Precedence.Add)
                            {
                                goto default;
                            }

                            tok = _curs.TokMove();
                            rightTrivia = ParseTrivia();
                            right = ParseExpr(Precedence.Add + 1);
                            right = new UnaryOpNode(ref _idNext, tok, right.SourceList, UnaryOp.Minus, right);
                            node = MakeBinary(BinaryOp.Add, node, leftTrivia, tok, rightTrivia, right);
                            break;

                        case TokKind.Add:
                            if (precMin > Precedence.Add)
                            {
                                goto default;
                            }

                            node = ParseBinary(node, leftTrivia, BinaryOp.Add, Precedence.Add + 1);
                            break;

                        case TokKind.Ampersand:
                            if (precMin > Precedence.Concat)
                            {
                                goto default;
                            }

                            node = ParseBinary(node, leftTrivia, BinaryOp.Concat, Precedence.Concat + 1);
                            break;

                        case TokKind.KeyAnd:
                        case TokKind.And:
                            if (precMin > Precedence.And)
                            {
                                goto default;
                            }

                            node = ParseBinary(node, leftTrivia, BinaryOp.And, Precedence.And + 1);
                            break;

                        case TokKind.KeyOr:
                        case TokKind.Or:
                            if (precMin > Precedence.Or)
                            {
                                goto default;
                            }

                            node = ParseBinary(node, leftTrivia, BinaryOp.Or, Precedence.Or + 1);
                            break;

                        // Comparison operators
                        // expr = expr
                        // expr <> expr
                        case TokKind.Equ:
                            if (precMin > Precedence.Compare)
                            {
                                goto default;
                            }

                            node = ParseBinary(node, leftTrivia, BinaryOp.Equal, Precedence.Compare + 1);
                            break;

                        case TokKind.LssGrt:
                            if (precMin > Precedence.Compare)
                            {
                                goto default;
                            }

                            node = ParseBinary(node, leftTrivia, BinaryOp.NotEqual, Precedence.Compare + 1);
                            break;

                        // expr < expr
                        case TokKind.Lss:
                            if (precMin > Precedence.Compare)
                            {
                                goto default;
                            }

                            node = ParseBinary(node, leftTrivia, BinaryOp.Less, Precedence.Compare + 1);
                            break;

                        // expr <= expr
                        case TokKind.LssEqu:
                            if (precMin > Precedence.Compare)
                            {
                                goto default;
                            }

                            node = ParseBinary(node, leftTrivia, BinaryOp.LessEqual, Precedence.Compare + 1);
                            break;

                        // expr > expr
                        case TokKind.Grt:
                            if (precMin > Precedence.Compare)
                            {
                                goto default;
                            }

                            node = ParseBinary(node, leftTrivia, BinaryOp.Greater, Precedence.Compare + 1);
                            break;

                        // expr >= expr
                        case TokKind.GrtEqu:
                            if (precMin > Precedence.Compare)
                            {
                                goto default;
                            }

                            node = ParseBinary(node, leftTrivia, BinaryOp.GreaterEqual, Precedence.Compare + 1);
                            break;

                        case TokKind.Ident:
                        case TokKind.NumLit:
                        case TokKind.StrLit:
                        case TokKind.True:
                        case TokKind.False:
                            PostError(_curs.TokCur, TexlStrings.ErrOperatorExpected);
                            tok = _curs.TokMove();
                            rightTrivia = ParseTrivia();
                            right = ParseExpr(Precedence.Error);
                            node = MakeBinary(BinaryOp.Error, node, leftTrivia, tok, rightTrivia, right);
                            break;

                        case TokKind.ParenOpen:
                            if (node is not DottedNameNode dotted ||
                                !dotted.HasPossibleNamespaceQualifier)
                            {
                                goto default;
                            }

                            node = ParseInvocationWithNamespace(dotted);
                            break;

                        case TokKind.In:
                            if (precMin > Precedence.In)
                            {
                                goto default;
                            }

                            node = ParseBinary(node, leftTrivia, BinaryOp.In, Precedence.In + 1);
                            break;

                        case TokKind.Exactin:
                            if (precMin > Precedence.In)
                            {
                                goto default;
                            }

                            node = ParseBinary(node, leftTrivia, BinaryOp.Exactin, Precedence.In + 1);
                            break;

                        case TokKind.As:
                            if (precMin > Precedence.As)
                            {
                                goto default;
                            }

                            node = ParseAs(node, leftTrivia);
                            break;

                        case TokKind.Semicolon:
                            if (_flags.HasFlag(Flags.NamedFormulas))
                            {
                                goto default;
                            }

                            // Only allow this when expression chaining is enabled (e.g. in behavior rules).
                            if ((_flags & Flags.EnableExpressionChaining) == 0)
                            {
                                goto case TokKind.False;
                            }

                            if (precMin > Precedence.None)
                            {
                                goto default;
                            }

                            node = ParseExprChain(node, leftTrivia);
                            break;

                        case TokKind.BracketOpen:
                            // Note we explicitly forbid [@foo][@bar], and also A!B!C[@foo], since these are syntactically nonsensical at the moment.
                            if (node is not FirstNameNode first || first.Ident.AtToken != null || _curs.TidPeek() != TokKind.At)
                            {
                                goto default;
                            }

                            node = ParseScopeField(first);
                            break;

                        case TokKind.Comment:
                            Contracts.Assert(false, "A stray comment was found");
                            _curs.TokMove();
                            return node;

                        case TokKind.Eof:
                            if (_after == null)
                            {
                                _after = new SourceList(leftTrivia);
                            }
                            else
                            {
                                _after = new SourceList(new SpreadSource(_after.Sources), new SpreadSource(leftTrivia));
                            }

                            return node;
                        default:
                            AddExtraTrivia(leftTrivia);
                            return node;
                    }
                }
            }
            finally
            {
                --_depth;
            }
        }

        private TexlNode ParseBinary(TexlNode left, ITexlSource leftTrivia, BinaryOp op, Precedence precedence)
        {
            var opToken = _curs.TokMove();
            var rightTrivia = ParseTrivia();
            var right = ParseExpr(precedence);

            return MakeBinary(op, left, leftTrivia, opToken, rightTrivia, right);
        }

        private TexlNode MakeBinary(BinaryOp op, TexlNode left, ITexlSource leftTrivia, Token opToken, ITexlSource rightTrivia, TexlNode right)
        {
            return new BinaryOpNode(
                ref _idNext,
                opToken,
                new SourceList(
                    new NodeSource(left),
                    new SpreadSource(leftTrivia),
                    new TokenSource(opToken),
                    new SpreadSource(rightTrivia),
                    new NodeSource(right)),
                op,
                left,
                right);
        }

        private TexlNode ParseAs(TexlNode left, ITexlSource leftTrivia)
        {
            var opToken = _curs.TokMove();
            var rightTrivia = ParseTrivia();
            var rhsIdentifier = ParseIdentifier();

            return new AsNode(
                ref _idNext,
                opToken,
                new SourceList(
                    new NodeSource(left),
                    new SpreadSource(leftTrivia),
                    new TokenSource(opToken),
                    new SpreadSource(rightTrivia),
                    new IdentifierSource(rhsIdentifier)),
                left,
                rhsIdentifier);
        }

        private TexlNode ParseOperand()
        {
            ITexlSource trivia;
            switch (_curs.TidCur)
            {
                // (Expr)
                case TokKind.ParenOpen:
                    return ParseParenExpr();

                // {id:Expr*}
                case TokKind.CurlyOpen:
                    return ParseRecordExpr(new SpreadSource());

                // [Expr*]
                // [@name]
                case TokKind.BracketOpen:
                    if (_curs.TidPeek() == TokKind.At)
                    {
                        return ParseBracketIdentifierAsFirstName(accountForAllPrecedenceTokens: true);
                    }

                    return ParseTableExpr();

                // -Expr
                case TokKind.Sub:
                    return ParseUnary(UnaryOp.Minus);

                // not Expr
                case TokKind.KeyNot:
                case TokKind.Bang:
                    return ParseUnary(UnaryOp.Not);

                // Literals
                case TokKind.NumLit:
                    return new NumLitNode(ref _idNext, _curs.TokMove().As<NumLitToken>());
                case TokKind.True:
                case TokKind.False:
                    return new BoolLitNode(ref _idNext, _curs.TokMove());
                case TokKind.StrInterpStart:
                    var res = ParseStringInterpolation();
                    var tokCur = _curs.TokCur;
                    if (FeatureFlags.StringInterpolation)
                    {
                        return res;
                    }

                    return CreateError(tokCur, TexlStrings.ErrBadToken);
                case TokKind.StrLit:
                    return new StrLitNode(ref _idNext, _curs.TokMove().As<StrLitToken>());

                // Names
                case TokKind.Ident:
                    var ident = ParseIdentifier();

                    if (AfterSpaceTokenId() == TokKind.ParenOpen)
                    {
                        trivia = ParseTrivia();
                        return ParseInvocation(ident, trivia, null);
                    }

                    if (AfterSpaceTokenId() == TokKind.At)
                    {
                        trivia = ParseTrivia();
                        return ParseRecordExpr(trivia, ident);
                    }

                    return new FirstNameNode(ref _idNext, ident.Token, ident);

                // Parent
                case TokKind.Parent:
                    return new ParentNode(ref _idNext, _curs.TokMove());

                // Self
                case TokKind.Self:
                    return new SelfNode(ref _idNext, _curs.TokMove());

                case TokKind.Eof:
                    return CreateError(_curs.TokCur, TexlStrings.ErrOperandExpected);

                case TokKind.Error:
                    var errorToken = _curs.TokMove().As<ErrorToken>();
                    var args = errorToken.ResourceKeyFormatStringArgs;
                    if (args == null || args.Length == 0)
                    {
                        return CreateError(errorToken, errorToken.DetailErrorKey ?? TexlStrings.ErrBadToken);
                    }

                    return CreateError(errorToken, errorToken.DetailErrorKey ?? TexlStrings.ErrBadToken, args);

                case TokKind.Comment:
                    Contracts.Assert(false, "A stray comment was found");
                    _curs.TokMove();
                    return ParseOperand();

                // Any other input should cause parsing errors.
                default:
                    return CreateError(_curs.TokMove(), TexlStrings.ErrBadToken);
            }
        }

        private TokKind AfterSpaceTokenId()
        {
            var split = _curs.Split();
            ParseTrivia(split);
            return split.TidCur;
        }

        private TexlNode ParseUnary(UnaryOp op)
        {
            var tok = _curs.TokMove();
            var rightTrivia = ParseTrivia();
            var right = ParseExpr(Precedence.PrefixUnary);

            return new UnaryOpNode(
                ref _idNext,
                tok,
                new SourceList(
                    new TokenSource(tok),
                    rightTrivia,
                    new NodeSource(right)),
                op,
                right);
        }

        // Parses an identifier delimited by brackets, e.g. [@foo]
        private FirstNameNode ParseBracketIdentifierAsFirstName(bool accountForAllPrecedenceTokens = false)
        {
            Contracts.Assert(_curs.TidCur == TokKind.BracketOpen);
            Contracts.Assert(_curs.TidPeek() == TokKind.At);

            var bracketOpen = _curs.TokMove();
            var at = _curs.TokMove();

            var ident = ParseIdentifier(at);
            var bracketClose = _curs.TokMove();

            if (bracketClose.Kind != TokKind.BracketClose)
            {
                ErrorTid(bracketClose, TokKind.BracketClose);
            }

            return new FirstNameNode(
                ref _idNext,
                ident.Token,
                new SourceList(
                    new TokenSource(bracketOpen),
                    new TokenSource(at),
                    new IdentifierSource(ident),
                    new TokenSource(bracketClose)),
                ident);
        }

        // Parses the RHS of a scope field. E.g., [@bar] in "foo[@bar]"
        private DottedNameNode ParseScopeField(FirstNameNode lhs)
        {
            Contracts.AssertValue(lhs);
            Contracts.Assert(_curs.TidCur == TokKind.BracketOpen);
            Contracts.Assert(_curs.TidPeek() == TokKind.At);

            var bracketOpen = _curs.TokCur;

            // Parse the rhs of the dotted name
            var rhs = ParseBracketIdentifierAsFirstName();

            // Form the dotted name
            return new DottedNameNode(
                ref _idNext,
                bracketOpen,
                new SourceList(new NodeSource(lhs), new NodeSource(rhs)),
                lhs,
                rhs.Ident,
                rhs);
        }

        private Identifier ParseIdentifier(Token at = null)
        {
            Contracts.AssertValueOrNull(at);

            IdentToken tok;

            if (_curs.TidCur == TokKind.Ident)
            {
                tok = _curs.TokMove().As<IdentToken>();
                if (tok.HasDelimiterStart && !tok.HasDelimiterEnd)
                {
                    PostError(tok, TexlStrings.ErrClosingBracketExpected);
                }
                else if (tok.IsModified)
                {
                    PostError(tok, TexlStrings.ErrEmptyInvalidIdentifier);
                }
            }
            else
            {
                ErrorTid(_curs.TokCur, TokKind.Ident);
                var ich = _curs.TokCur.Span.Min;
                tok = new IdentToken(string.Empty, new Span(ich, ich));
            }

            return new Identifier(at, tok);
        }

        private TexlNode ParseStringInterpolation()
        {
            Contracts.Assert(_curs.TidCur == TokKind.StrInterpStart);
            var startToken = _curs.TokMove();

            var strInterpStart = startToken;
            var strInterpTrivia = ParseTrivia();

            var arguments = new List<TexlNode>();
            var sourceList = new List<ITexlSource>
            {
                new TokenSource(strInterpStart),
                strInterpTrivia
            };

            if (_curs.TidCur == TokKind.StrInterpEnd)
            {
                var tokenEnd = _curs.TokMove();
                sourceList.Add(new TokenSource(tokenEnd));

                return new StrInterpNode(ref _idNext, strInterpStart, new SourceList(sourceList), new TexlNode[0], tokenEnd);
            }

            for (var i = 0; ; i++)
            {
                if (_curs.TidCur == TokKind.IslandStart)
                {
                    var islandStart = _curs.TokMove();
                    sourceList.Add(new TokenSource(islandStart));
                    sourceList.Add(ParseTrivia());

                    if (_curs.TidCur == TokKind.IslandEnd)
                    {
                        arguments.Add(CreateError(_curs.TokCur, TexlStrings.ErrEmptyIsland));
                    }
                }
                else if (_curs.TidCur == TokKind.IslandEnd)
                {
                    var islandEnd = _curs.TokMove();
                    sourceList.Add(new TokenSource(islandEnd));
                    sourceList.Add(ParseTrivia());
                }
                else if (_curs.TidCur == TokKind.Eof)
                {
                    var error = CreateError(_curs.TokCur, TexlStrings.ErrBadToken);
                    arguments.Add(error);
                    sourceList.Add(new NodeSource(error));
                    sourceList.Add(ParseTrivia());
                    return new StrInterpNode(
                        ref _idNext,
                        strInterpStart,
                        new SourceList(sourceList),
                        arguments.ToArray(),
                        _curs.TokCur);
                }
                else if (_curs.TidCur == TokKind.StrInterpEnd)
                {
                    break;
                }
                else
                {
                    var argument = ParseExpr(Precedence.None);
                    arguments.Add(argument);
                    sourceList.Add(new NodeSource(argument));
                    sourceList.Add(ParseTrivia());
                }
            }

            Contracts.Assert(_curs.TidCur == TokKind.StrInterpEnd || _curs.TidCur == TokKind.Eof);

            Token strInterpEnd = null;
            if (_curs.TidCur == TokKind.StrInterpEnd)
            {
                strInterpEnd = TokEat(TokKind.StrInterpEnd);
            }

            if (strInterpEnd != null)
            {
                sourceList.Add(new TokenSource(strInterpEnd));
            }

            return new StrInterpNode(
                ref _idNext,
                strInterpStart,
                new SourceList(sourceList),
                arguments.ToArray(),
                strInterpEnd);
        }

        // Parse a namespace-qualified invocation, e.g. Facebook.GetFriends()
        private CallNode ParseInvocationWithNamespace(DottedNameNode dotted)
        {
            Contracts.Assert(dotted.HasPossibleNamespaceQualifier);

            var path = dotted.ToDPath();
            Contracts.Assert(path.IsValid);
            Contracts.Assert(!path.IsRoot);
            Contracts.Assert(!path.Parent.IsRoot);

            var head = new Identifier(path.Parent, null, dotted.Right.Token);
            Contracts.Assert(_curs.TidCur == TokKind.ParenOpen);

            return ParseInvocation(head, ParseTrivia(), dotted);
        }

        private CallNode ParseInvocation(Identifier head, ITexlSource headTrivia, TexlNode headNode)
        {
            Contracts.AssertValue(head);
            Contracts.AssertValueOrNull(headNode);
            Contracts.Assert(_curs.TidCur == TokKind.ParenOpen);

            var leftParen = _curs.TokMove();
            var leftTrivia = ParseTrivia();
            if (_curs.TidCur == TokKind.ParenClose)
            {
                var rightParen = _curs.TokMove();
                var right = new ListNode(
                    ref _idNext,
                    _curs.TokCur,
                    new TexlNode[0],
                    null,
                    new SourceList(
                        new TokenSource(leftParen),
                        leftTrivia,
                        new TokenSource(rightParen)));

                var sources = new List<ITexlSource>();
                if (headNode != null)
                {
                    sources.Add(new NodeSource(headNode));
                }
                else
                {
                    sources.Add(new IdentifierSource(head));
                }

                sources.Add(headTrivia);
                sources.Add(new NodeSource(right));

                return new CallNode(
                    ref _idNext,
                    leftParen,
                    new SourceList(sources),
                    head,
                    headNode,
                    right,
                    rightParen);
            }

            var rgtokCommas = new List<Token>();
            var arguments = new List<TexlNode>();
            var sourceList = new List<ITexlSource>
            {
                new TokenSource(leftParen),
                leftTrivia
            };
            for (; ;)
            {
                while (_curs.TidCur == TokKind.Comma)
                {
                    var commaToken = _curs.TokMove();
                    arguments.Add(CreateError(commaToken, TexlStrings.ErrBadToken));
                    sourceList.Add(new TokenSource(commaToken));
                    sourceList.Add(ParseTrivia());
                    rgtokCommas.Add(commaToken);
                }

                var argument = ParseExpr(Precedence.None);
                arguments.Add(argument);
                sourceList.Add(new NodeSource(argument));
                sourceList.Add(ParseTrivia());

                if (_curs.TidCur != TokKind.Comma)
                {
                    break;
                }

                var comma = _curs.TokMove();
                rgtokCommas.Add(comma);
                sourceList.Add(new TokenSource(comma));
                sourceList.Add(ParseTrivia());
            }

            var parenClose = TokEat(TokKind.ParenClose);
            if (parenClose != null)
            {
                sourceList.Add(new TokenSource(parenClose));
            }

            var list = new ListNode(
                ref _idNext,
                leftParen,
                arguments.ToArray(),
                CollectionUtils.ToArray(rgtokCommas),
                new SourceList(sourceList));

            ITexlSource headNodeSource = new IdentifierSource(head);
            if (headNode != null)
            {
                headNodeSource = new NodeSource(headNode);
            }

            return new CallNode(
                ref _idNext,
                leftParen,
                new SourceList(
                    headNodeSource,
                    headTrivia,
                    new NodeSource(list)),
                head,
                headNode,
                list,
                parenClose);
        }

        private TexlNode ParseExprChain(TexlNode node, ITexlSource leftTrivia)
        {
            Contracts.AssertValue(node);
            Contracts.Assert(_curs.TidCur == TokKind.Semicolon);

            var delimiters = new List<Token>(1);
            var expressions = new List<TexlNode>(2)
            {
                node
            };

            var sourceList = new List<ITexlSource>
            {
                new NodeSource(node),
                leftTrivia
            };

            while (_curs.TidCur == TokKind.Semicolon)
            {
                var delimiter = _curs.TokMove();
                delimiters.Add(delimiter);
                sourceList.Add(new TokenSource(delimiter));
                sourceList.Add(ParseTrivia());

                if (_curs.TidCur == TokKind.Eof || _curs.TidCur == TokKind.Comma || _curs.TidCur == TokKind.ParenClose)
                {
                    break;
                }

                // SingleExpr here means we don't want chains on the RHS, but individual expressions.
                var expression = ParseExpr(Precedence.SingleExpr);
                expressions.Add(expression);
                sourceList.Add(new NodeSource(expression));
                sourceList.Add(ParseTrivia());
            }

            return new VariadicOpNode(
                ref _idNext,
                VariadicOp.Chain,
                expressions.ToArray(),
                delimiters.ToArray(),
                new SourceList(sourceList));
        }

        // Parse a record expression of the form: {id:expr, id:expr, ...}
        // or of the form ident@{id:expr, id:expr}
        private RecordNode ParseRecordExpr(ITexlSource sourceRestrictionTrivia, Identifier sourceRestriction = null)
        {
            Contracts.Assert(_curs.TidCur == TokKind.CurlyOpen || _curs.TidCur == TokKind.At);
            Contracts.AssertValueOrNull(sourceRestriction);

            Token curlyClose;

            var commas = new List<Token>();
            var colons = new List<Token>();
            var ids = new List<Identifier>();
            var exprs = new List<TexlNode>();
            var sourceList = new List<ITexlSource>();
            TexlNode sourceRestrictionNode = null;

            var primaryToken = _curs.TokMove();

            if (primaryToken.Kind == TokKind.At)
            {
                Contracts.AssertValue(sourceRestriction);
                sourceList.Add(sourceRestrictionTrivia);
                sourceList.Add(new TokenSource(primaryToken));
                sourceList.Add(ParseTrivia());

                primaryToken = sourceRestriction.Token;
                sourceRestrictionNode = new FirstNameNode(ref _idNext, sourceRestriction.Token, sourceRestriction);

                if (_curs.TidCur != TokKind.CurlyOpen)
                {
                    ErrorTid(_curs.TokCur, TokKind.CurlyOpen);
                    curlyClose = TokEat(TokKind.CurlyClose);
                    return new RecordNode(
                        ref _idNext,
                        sourceRestriction.Token,
                        new SourceList(
                            new SpreadSource(sourceList),
                            curlyClose != null ? (ITexlSource)new TokenSource(curlyClose) : new SpreadSource()),
                        ids.ToArray(),
                        exprs.ToArray(),
                        null,
                        null,
                        curlyClose,
                        sourceRestrictionNode);
                }

                sourceList.Add(new TokenSource(_curs.TokMove()));
                sourceList.Add(ParseTrivia());
            }
            else
            {
                sourceList.Add(new TokenSource(primaryToken));
                sourceList.Add(ParseTrivia());
            }

            while (_curs.TidCur != TokKind.CurlyClose)
            {
                // id
                var ident = ParseIdentifier();
                sourceList.Add(new IdentifierSource(ident));
                sourceList.Add(ParseTrivia());

                // :
                if (_curs.TidCur != TokKind.Colon)
                {
                    ErrorTid(_curs.TokCur, TokKind.Colon);
                    var errorToken = _curs.TokMove();
                    TexlNode errorExp = CreateError(errorToken, TexlStrings.ErrColonExpected);
                    sourceList.Add(new TokenSource(errorToken));
                    sourceList.Add(ParseTrivia());
                    ids.Add(ident);
                    exprs.Add(errorExp);
                    break;
                }

                var colon = _curs.TokMove();
                colons.Add(colon);
                sourceList.Add(new TokenSource(colon));
                sourceList.Add(ParseTrivia());

                // expr
                // SingleExpr here means we don't want chains, but individual expressions.
                var expr = ParseExpr(Precedence.SingleExpr);

                ids.Add(ident);
                exprs.Add(expr);
                sourceList.Add(new NodeSource(expr));
                sourceList.Add(ParseTrivia());

                // ,
                if (_curs.TidCur != TokKind.Comma)
                {
                    break;
                }

                var comma = _curs.TokMove();
                commas.Add(comma);
                sourceList.Add(new TokenSource(comma));
                sourceList.Add(ParseTrivia());

                if (_curs.TidCur == TokKind.CurlyClose)
                {
                    TexlNode errorExp = CreateError(comma, TexlStrings.ErrColonExpected);
                    exprs.Add(errorExp);
                    ids.Add(ParseIdentifier());
                }
            }

            Contracts.Assert(ids.Count == exprs.Count);

            var commaArray = commas?.ToArray();
            var colonArray = colons?.ToArray();

            curlyClose = TokEat(TokKind.CurlyClose);
            if (curlyClose != null)
            {
                sourceList.Add(new TokenSource(curlyClose));
            }

            return new RecordNode(
                ref _idNext,
                primaryToken,
                new SourceList(sourceList),
                ids.ToArray(),
                exprs.ToArray(),
                commaArray,
                colonArray,
                curlyClose,
                sourceRestrictionNode);
        }

        // Parse a table expression. The only currently supported form is: [expr, expr, ...]
        private TableNode ParseTableExpr()
        {
            Contracts.Assert(_curs.TidCur == TokKind.BracketOpen);
            var sourceList = new List<ITexlSource>();

            var tok = _curs.TokMove();
            sourceList.Add(new TokenSource(tok));
            sourceList.Add(ParseTrivia());

            var commas = new List<Token>();
            var expressions = new List<TexlNode>();

            while (_curs.TidCur != TokKind.BracketClose)
            {
                // expr
                // SingleExpr here means we don't want chains, but individual expressions.
                var expression = ParseExpr(Precedence.SingleExpr);
                expressions.Add(expression);
                sourceList.Add(new NodeSource(expression));
                sourceList.Add(ParseTrivia());

                // ,
                if (_curs.TidCur != TokKind.Comma)
                {
                    break;
                }

                var comma = _curs.TokMove();
                commas.Add(comma);
                sourceList.Add(new TokenSource(comma));
                sourceList.Add(ParseTrivia());
            }

            var commaArray = commas?.ToArray();

            var bracketClose = TokEat(TokKind.BracketClose);
            if (bracketClose != null)
            {
                sourceList.Add(new TokenSource(bracketClose));
            }

            return new TableNode(
                ref _idNext,
                tok,
                new SourceList(sourceList),
                expressions.ToArray(),
                commaArray,
                bracketClose);
        }

        private TexlNode ParseParenExpr()
        {
            Contracts.Assert(_curs.TidCur == TokKind.ParenOpen);

            var open = _curs.TokMove();
            var before = ParseTrivia();

            // SingleExpr here means we don't want chains, but individual expressions.
            var node = ParseExpr(Precedence.SingleExpr);
            var after = ParseTrivia();
            var close = TokEat(TokKind.ParenClose);

            var sources = new List<ITexlSource>
            {
                new TokenSource(open),
                before,
                new SpreadSource(node.SourceList.Sources),
                after
            };
            if (close != null)
            {
                sources.Add(new TokenSource(close));
            }

            node.Parser_SetSourceList(new SourceList(new SpreadSource(sources)));
            return node;
        }

        private ErrorNode CreateError(Token tok, ErrorResourceKey errKey, object[] args)
        {
            Contracts.AssertValue(tok);
            Contracts.AssertValue(args);

            var err = PostError(tok, errKey, args);
            return new ErrorNode(ref _idNext, tok, err.ShortMessage, args);
        }

        private ErrorNode CreateError(Token tok, ErrorResourceKey errKey)
        {
            Contracts.AssertValue(tok);

            var err = PostError(tok, errKey);
            return new ErrorNode(ref _idNext, tok, err.ShortMessage);
        }

        private TexlError PostError(Token tok, ErrorResourceKey errKey)
        {
            Contracts.AssertValue(tok);
            Contracts.AssertValue(errKey.Key);

            var err = new TexlError(tok, DocumentErrorSeverity.Critical, errKey);
            CollectionUtils.Add(ref _errors, err);
            return err;
        }

        private TexlError PostError(Token tok, ErrorResourceKey errKey, params object[] args)
        {
            Contracts.AssertValue(tok);
            Contracts.AssertValue(errKey.Key);
            Contracts.AssertValueOrNull(args);

            var err = new TexlError(tok, DocumentErrorSeverity.Critical, errKey, args);
            CollectionUtils.Add(ref _errors, err);

            return err;
        }

        // Eats a token of the given kind.
        // If the token is not the right kind, reports an error and leaves it.
        private bool EatTid(TokKind tid)
        {
            if (_curs.TidCur == tid)
            {
                _curs.TokMove();
                return true;
            }

            ErrorTid(_curs.TokCur, tid);
            return false;
        }

        // Returns the current token if it's of the given kind and moves to the next token.
        // If the token is not the right kind, reports an error, leaves the token, and returns null.
        private Token TokEat(TokKind tid, int offset = 0)
        {
            if (_curs.TidCur == tid)
            {
                return _curs.TokMove();
            }

            ErrorTid(_curs.TokCur, tid);
            return null;
        }

        private void ErrorTid(Token tok, TokKind tidWanted)
        {
            Contracts.Assert(tidWanted != tok.Kind);

            PostError(tok, TexlStrings.ErrExpectedFound_Ex_Fnd, tidWanted, tok);
        }

        // Gets the string corresponding to token kinds used in binary or unary nodes.
        internal static string GetTokString(TokKind kind)
        {
            switch (kind)
            {
                case TokKind.And:
                    return TexlLexer.PunctuatorAnd;
                case TokKind.Or:
                    return TexlLexer.PunctuatorOr;
                case TokKind.Bang:
                    return TexlLexer.PunctuatorBang;
                case TokKind.Add:
                    return TexlLexer.PunctuatorAdd;
                case TokKind.Sub:
                    return TexlLexer.PunctuatorSub;
                case TokKind.Mul:
                    return TexlLexer.PunctuatorMul;
                case TokKind.Div:
                    return TexlLexer.PunctuatorDiv;
                case TokKind.Caret:
                    return TexlLexer.PunctuatorCaret;
                case TokKind.Ampersand:
                    return TexlLexer.PunctuatorAmpersand;
                case TokKind.PercentSign:
                    return TexlLexer.PunctuatorPercent;
                case TokKind.Equ:
                    return TexlLexer.PunctuatorEqual;
                case TokKind.Lss:
                    return TexlLexer.PunctuatorLess;
                case TokKind.LssEqu:
                    return TexlLexer.PunctuatorLessOrEqual;
                case TokKind.Grt:
                    return TexlLexer.PunctuatorGreater;
                case TokKind.GrtEqu:
                    return TexlLexer.PunctuatorGreaterOrEqual;
                case TokKind.LssGrt:
                    return TexlLexer.PunctuatorNotEqual;
                case TokKind.Dot:
                    return TexlLexer.PunctuatorDot;
                case TokKind.In:
                    return TexlLexer.KeywordIn;
                case TokKind.Exactin:
                    return TexlLexer.KeywordExactin;
                case TokKind.BracketOpen:
                    return TexlLexer.PunctuatorBracketOpen;
                case TokKind.KeyOr:
                    return TexlLexer.KeywordOr;
                case TokKind.KeyAnd:
                    return TexlLexer.KeywordAnd;
                case TokKind.KeyNot:
                    return TexlLexer.KeywordNot;
                case TokKind.As:
                    return TexlLexer.KeywordAs;
                default:
                    return string.Empty;
            }
        }

        public static string Format(string text)
        {
            var result = ParseScript(
                text,
                flags: Flags.EnableExpressionChaining);

            // Can't pretty print a script with errors.
            if (result.HasError)
            {
                return text;
            }

            return PrettyPrintVisitor.Format(result.Root, result.Before, result.After, text);
        }
    }
}
