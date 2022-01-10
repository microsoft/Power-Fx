// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Parser
{
    using Conditional = System.Diagnostics.ConditionalAttribute;

    internal sealed class TokenCursor
    {
        private readonly Token[] _tokens;
        private readonly int _tokenCount;

        private int _currentTokenIndex;
        private Token _currentToken;
        private TokKind _currentTokenId;

        public TokenCursor(Token[] rgtok)
        {
            Contracts.AssertValue(rgtok);
            Contracts.Assert(rgtok.Length > 0 && rgtok[rgtok.Length - 1].Kind == TokKind.Eof);
            _tokens = rgtok;
            _tokenCount = _tokens.Length;

            _currentToken = _tokens[0];
            _currentTokenId = _currentToken.Kind;
        }

        public TokenCursor Split()
        {
            var split = new TokenCursor(_tokens);
            split._currentTokenIndex = _currentTokenIndex;
            split._currentToken = _currentToken;
            split._currentTokenId = _currentTokenId;
            return split;
        }

        [Conditional("DEBUG")]
        private void AssertValid()
        {
            Contracts.AssertValue(_tokens);
            Contracts.Assert(0 < _tokenCount && _tokenCount <= _tokens.Length);
            Contracts.Assert(_tokens[_tokenCount - 1].Kind == TokKind.Eof);

            Contracts.AssertIndex(_currentTokenIndex, _tokenCount);
            Contracts.Assert(_currentToken == _tokens[_currentTokenIndex]);
            Contracts.Assert(_currentTokenId == _currentToken.Kind);
        }

        public int ItokLim
        {
            get
            {
                AssertValid();
                return _tokenCount;
            }
        }

        public int ItokCur
        {
            get
            {
                AssertValid();
                return _currentTokenIndex;
            }
        }

        public Token TokCur
        {
            get
            {
                AssertValid();
                return _currentToken;
            }
        }

        public TokKind TidCur
        {
            get
            {
                AssertValid();
                return _currentTokenId;
            }
        }

        public void MoveTo(int tokenIndex)
        {
            AssertValid();
            Contracts.AssertIndex(_currentTokenIndex, _tokenCount);
            _currentTokenIndex = tokenIndex;
            _currentToken = _tokens[_currentTokenIndex];
            _currentTokenId = _currentToken.Kind;
            AssertValid();
        }

        public Token TokMove()
        {
            AssertValid();
            var tok = _currentToken;
            if (_currentTokenId != TokKind.Eof)
                MoveTo(_currentTokenIndex + 1);
            return tok;
        }

        public TokKind TidPeek()
        {
            AssertValid();
            int itok;
            if ((itok = _currentTokenIndex + 1) < _tokenCount)
                return _tokens[itok].Kind;
            Contracts.Assert(_currentTokenId == TokKind.Eof);
            return _currentTokenId;
        }

        public TokKind TidPeek(int ditok)
        {
            AssertValid();

            var itok = ItokPeek(ditok);
            Contracts.AssertIndex(_currentTokenIndex, _tokenCount);
            return _tokens[itok].Kind;
        }

        public Token[] SkipWhitespace()
        {
            var tokens = new List<Token>();
            while(_currentTokenId == TokKind.Whitespace)
                tokens.Add(TokMove());
            return tokens.ToArray();
        }

        public IEnumerable<Token> SkipWhitespace(IEnumerable<Token> initial)
        {
            var tokens = new List<Token>();
            while (_currentTokenId == TokKind.Whitespace)
                tokens.Add(TokMove());
            return initial.Concat(tokens);
        }

        private int ItokPeek(int ditok)
        {
            AssertValid();

            var itokPeek = _currentTokenIndex + ditok;
            if (itokPeek >= _tokenCount)
                return _tokenCount - 1;
            if (itokPeek < 0)
                return ditok <= 0 ? 0 : _tokenCount - 1;
            return itokPeek;
        }
    }
}