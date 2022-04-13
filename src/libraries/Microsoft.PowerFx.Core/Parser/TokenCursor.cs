// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Utils;
using Conditional = System.Diagnostics.ConditionalAttribute;

namespace Microsoft.PowerFx.Core.Parser
{
    internal sealed class TokenCursor
    {
        private readonly Token[] _tokens;
        private readonly int _tokenCount;
        private int _offset;
        private int _currentTokenIndex;
        private Token _currentToken;
        private TokKind _currentTokenId;
        private int _currentCharIndex;
        private bool _shouldUseOffset;

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
            var split = new TokenCursor(_tokens)
            {
                _currentTokenIndex = _currentTokenIndex,
                _currentToken = _currentToken,
                _currentTokenId = _currentTokenId,
                _offset = _offset
            };
            return split;
        }

        [Conditional("DEBUG")]
        private void AssertValid()
        {
            Contracts.AssertValue(_tokens);
            Contracts.Assert(_tokenCount > 0 && _tokenCount <= _tokens.Length);
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

        public int CurrentCharIndex
        {
            get
            {
                AssertValid();
                return _currentCharIndex;
            }
        }

        public int ResetOffset()
        {
            _offset = 0;
            _shouldUseOffset = true;
            return _currentCharIndex;
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
            {
                MoveTo(_currentTokenIndex + 1);
            }

            var tokenLength = tok.Span.Lim - tok.Span.Min;
            _currentCharIndex += tokenLength;

            if (_shouldUseOffset)
            {
                tok = tok.Clone(new Localization.Span(_offset, _offset + tokenLength));
                if (tok.Kind == TokKind.Semicolon)
                {
                    _shouldUseOffset = false;
                }

                _offset = _offset + tokenLength;
            }

            return tok;
        }

        public TokKind TidPeek()
        {
            AssertValid();
            int itok;
            if ((itok = _currentTokenIndex + 1) < _tokenCount)
            {
                return _tokens[itok].Kind;
            }

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
            while (_currentTokenId == TokKind.Whitespace)
            {
                tokens.Add(TokMove());
            }

            return tokens.ToArray();
        }

        public IEnumerable<Token> SkipWhitespace(IEnumerable<Token> initial)
        {
            var tokens = new List<Token>();
            while (_currentTokenId == TokKind.Whitespace)
            {
                tokens.Add(TokMove());
            }

            return initial.Concat(tokens);
        }

        private int ItokPeek(int ditok)
        {
            AssertValid();

            var itokPeek = _currentTokenIndex + ditok;
            if (itokPeek >= _tokenCount)
            {
                return _tokenCount - 1;
            }

            if (itokPeek < 0)
            {
                return ditok <= 0 ? 0 : _tokenCount - 1;
            }

            return itokPeek;
        }
    }
}
