// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Types
{
    internal sealed class DTypeSpecLexer
    {
        private int _cursor;
        private readonly string _typeSpec;

        public DTypeSpecLexer(string typeSpec)
        {
            Contracts.AssertNonEmpty(typeSpec);
            _typeSpec = typeSpec;
            _cursor = 0;
        }

        public bool Eol => _cursor >= _typeSpec.Length;

        private char CurChar
        {
            get
            {
                Contracts.Assert(!Eol);
                return _typeSpec[_cursor];
            }
        }

        public bool TryNextToken(out string token)
        {
            while (!Eol && CharacterUtils.IsSpace(CurChar))
                ++_cursor;

            if (Eol)
            {
                token = null;
                return false;
            }

            const string punctuators = "*!%:[],";
            if (punctuators.IndexOf(CurChar) >= 0)
            {
                token = CurChar.ToString();
                _cursor++;
            }
            else
            {
                StringBuilder tok = new StringBuilder();

                char quote = '0';
                while (!Eol)
                {
                    char c = CurChar;
                    if ((c == '"' && (quote == '"' || quote == '0')) ||
                        (c == '\'' && (quote == '\'' || quote == '0')))
                    {
                        if (quote == '0')
                            quote = c;
                        else
                        {
                            tok.Append(c);
                            ++_cursor;

                            // If the quote character is not being escaped (examples of
                            // escaping: 'apos''trophe', or "quo""te"), then we end the token.
                            if (Eol || CurChar != c)
                            {
                                quote = '0';
                                break;
                            }
                            // else we let the fall-through logic append c once more.
                        }
                    }
                    else if ((quote == '0') && (CharacterUtils.IsSpace(c) || punctuators.IndexOf(c) >= 0))
                        break;

                    tok.Append(c);
                    ++_cursor;
                }

                if (quote != '0')
                {
                    token = null;
                    return false;
                }

                token = tok.ToString();
            }

            while (!Eol && CharacterUtils.IsSpace(CurChar))
                ++_cursor;

            return true;
        }
    }
}