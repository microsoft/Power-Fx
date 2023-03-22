// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

// Used as a temporary storage for LexerImpl class.
// LexerImpl is private, so we cannot define 'using' for it here - TexlLexer instead.
using StringBuilderCache = Microsoft.PowerFx.Core.Utils.StringBuilderCache<Microsoft.PowerFx.Syntax.TexlLexer>;

namespace Microsoft.PowerFx.Syntax
{
    [ThreadSafeImmutable]
    internal sealed class TexlLexer
    {
        [Flags]
        public enum Flags
        {
            None,
        }

        // Locale-invariant syntax.
        public const string KeywordTrue = "true";
        public const string KeywordFalse = "false";
        public const string KeywordIn = "in";
        public const string KeywordExactin = "exactin";
        public const string KeywordSelf = "Self";
        public const string KeywordParent = "Parent";
        public const string KeywordAnd = "And";
        public const string KeywordOr = "Or";
        public const string KeywordNot = "Not";
        public const string KeywordAs = "As";
        public const string PunctuatorDecimalSeparatorInvariant = ".";
        public const string PunctuatorCommaInvariant = ",";
        public const string PunctuatorSemicolonInvariant = ";";
        public const string PunctuatorAnd = "&&";
        public const string PunctuatorOr = "||";
        public const string PunctuatorDot = ".";
        public const string PunctuatorBang = "!";
        public const string PunctuatorAdd = "+";
        public const string PunctuatorSub = "-";
        public const string PunctuatorMul = "*";
        public const string PunctuatorDiv = "/";
        public const string PunctuatorCaret = "^";
        public const string PunctuatorAmpersand = "&";
        public const string PunctuatorPercent = "%";
        public const string PunctuatorEqual = "=";
        public const string PunctuatorNotEqual = "<>";
        public const string PunctuatorGreater = ">";
        public const string PunctuatorGreaterOrEqual = ">=";
        public const string PunctuatorLess = "<";
        public const string PunctuatorLessOrEqual = "<=";
        public const string PunctuatorParenOpen = "(";
        public const string PunctuatorParenClose = ")";
        public const string PunctuatorCurlyOpen = "{";
        public const string PunctuatorCurlyClose = "}";
        public const string PunctuatorBracketOpen = "[";
        public const string PunctuatorBracketClose = "]";
        public const string PunctuatorColon = ":";
        public const string PunctuatorAt = "@";
        public const char IdentifierDelimiter = '\'';
        public const string PunctuatorDoubleBarrelArrow = "=>";

        // These puntuators are related to commenting in the formula bar
        public const string PunctuatorBlockComment = "/*";
        public const string PunctuatorLineComment = "//";

        // Defaults and options for disambiguation
        private const string PunctuatorSemicolonDefault = PunctuatorSemicolonInvariant;
        private const string PunctuatorSemicolonAlt1 = ";;";

        // Pretty Print defaults
        public const string FourSpaces = "    ";
        public const string LineBreakAndfourSpaces = "\n    ";

        // Keywords are not locale-specific, populate keyword dictionary statically
        private static readonly IReadOnlyDictionary<string, TokKind> _keywords = new Dictionary<string, TokKind>()
        {
            { KeywordTrue, TokKind.True },
            { KeywordFalse, TokKind.False },
            { KeywordIn, TokKind.In },
            { KeywordExactin, TokKind.Exactin },
            { KeywordSelf, TokKind.Self },
            { KeywordParent, TokKind.Parent },
            { KeywordAnd, TokKind.KeyAnd },
            { KeywordOr, TokKind.KeyOr },
            { KeywordNot, TokKind.KeyNot },
            { KeywordAs, TokKind.As },
        };

        // Limits the StringBuilderCache TLS memory usage for LexerImpl.
        // Usually our tokens are less than 128 characters long, unless it's a large string.
        private const int DesiredStringBuilderSize = 128;

        public static TexlLexer InvariantLexer { get; } = new TexlLexer(PunctuatorDecimalSeparatorInvariant);

        public static TexlLexer CommaDecimalSeparatorLexer { get; } = new TexlLexer(PunctuatorCommaInvariant);

        private static readonly IReadOnlyList<string> _unaryOperatorKeywords;
        private static readonly IReadOnlyList<string> _binaryOperatorKeywords;
        private static readonly IReadOnlyList<string> _operatorKeywordsPrimitive;
        private static readonly IReadOnlyList<string> _operatorKeywordsAggregate;
        private static readonly IReadOnlyList<string> _constantKeywordsDefault;
        private static readonly IReadOnlyList<string> _constantKeywordsGetParent;

        private readonly IReadOnlyDictionary<string, TokKind> _punctuators;
        private readonly char _decimalSeparator;
        private readonly IReadOnlyDictionary<string, string> _punctuatorsAndInvariants;
        private readonly NumberFormatInfo _numberFormatInfo;

        public string LocalizedPunctuatorDecimalSeparator { get; }

        public string LocalizedPunctuatorListSeparator { get; }

        public string LocalizedPunctuatorChainingSeparator { get; }

        static TexlLexer()
        {
            StringBuilderCache.SetMaxBuilderSize(DesiredStringBuilderSize);

            _unaryOperatorKeywords = new[]
            {
                KeywordNot,
                PunctuatorBang
            };

            _binaryOperatorKeywords = new[]
            {
                PunctuatorAmpersand,
                PunctuatorAnd,
                PunctuatorOr,
                PunctuatorAdd,
                PunctuatorSub,
                PunctuatorMul,
                PunctuatorDiv,
                PunctuatorEqual,
                PunctuatorLess,
                PunctuatorLessOrEqual,
                PunctuatorGreater,
                PunctuatorGreaterOrEqual,
                PunctuatorNotEqual,
                PunctuatorCaret,

                KeywordAnd,
                KeywordOr,
                KeywordIn,
                KeywordExactin,
                KeywordAs
            };

            _operatorKeywordsPrimitive = new[]
            {
                PunctuatorAmpersand,
                PunctuatorEqual,
                PunctuatorNotEqual,
                PunctuatorAdd,
                PunctuatorSub,
                PunctuatorMul,
                PunctuatorDiv,
                PunctuatorCaret,
                PunctuatorAnd,
                PunctuatorOr,
                PunctuatorLess,
                PunctuatorLessOrEqual,
                PunctuatorGreater,
                PunctuatorGreaterOrEqual,

                KeywordAnd,
                KeywordOr,
                KeywordIn,
                KeywordExactin,
                KeywordAs
            };

            _operatorKeywordsAggregate = new[]
            {
                KeywordIn, KeywordExactin, KeywordAs
            };

            _constantKeywordsDefault = new[]
            {
                KeywordFalse, KeywordTrue, KeywordSelf
            };

            _constantKeywordsGetParent = new[]
            {
                KeywordFalse, KeywordTrue, KeywordParent, KeywordSelf
            };
        }

        public static TexlLexer GetLocalizedInstance(CultureInfo culture)
        {
            culture ??= CultureInfo.InvariantCulture;
            return culture.NumberFormat.NumberDecimalSeparator == PunctuatorDecimalSeparatorInvariant ? InvariantLexer : CommaDecimalSeparatorLexer;
        }

        public static IReadOnlyList<string> GetKeywords()
        {
            return _keywords.Keys.ToList();
        }

        private TexlLexer(string preferredDecimalSeparator)
        {
            // List and decimal separators.
            // These are the default global settings. If there is a collision between the two,
            // the list separator automatically becomes ;.
            LocalizedPunctuatorDecimalSeparator = ChooseDecimalSeparator(preferredDecimalSeparator);
            LocalizedPunctuatorListSeparator = ChooseListSeparatorPunctuator(LocalizedPunctuatorDecimalSeparator);

            // The chaining operator has to be disambiguated accordingly.
            LocalizedPunctuatorChainingSeparator = ChooseChainingPunctuator(LocalizedPunctuatorListSeparator, LocalizedPunctuatorDecimalSeparator);

            _punctuatorsAndInvariants = new Dictionary<string, string>
            {
                { LocalizedPunctuatorDecimalSeparator,         "." },
                { LocalizedPunctuatorListSeparator,            "," },
                { LocalizedPunctuatorChainingSeparator,        ";" }
            };

            _numberFormatInfo = new NumberFormatInfo() { NumberDecimalSeparator = LocalizedPunctuatorDecimalSeparator };
            _decimalSeparator = LocalizedPunctuatorDecimalSeparator[0];

            var punctuators = new Dictionary<string, TokKind>();

            // Invariant punctuators
            AddPunctuator(punctuators, PunctuatorOr, TokKind.Or);
            AddPunctuator(punctuators, PunctuatorAnd, TokKind.And);
            AddPunctuator(punctuators, PunctuatorBang, TokKind.Bang);
            AddPunctuator(punctuators, PunctuatorAdd, TokKind.Add);
            AddPunctuator(punctuators, PunctuatorSub, TokKind.Sub);
            AddPunctuator(punctuators, PunctuatorMul, TokKind.Mul);
            AddPunctuator(punctuators, PunctuatorDiv, TokKind.Div);
            AddPunctuator(punctuators, PunctuatorCaret, TokKind.Caret);
            AddPunctuator(punctuators, PunctuatorParenOpen, TokKind.ParenOpen);
            AddPunctuator(punctuators, PunctuatorParenClose, TokKind.ParenClose);
            AddPunctuator(punctuators, PunctuatorEqual, TokKind.Equ);
            AddPunctuator(punctuators, PunctuatorLess, TokKind.Lss);
            AddPunctuator(punctuators, PunctuatorLessOrEqual, TokKind.LssEqu);
            AddPunctuator(punctuators, PunctuatorGreater, TokKind.Grt);
            AddPunctuator(punctuators, PunctuatorGreaterOrEqual, TokKind.GrtEqu);
            AddPunctuator(punctuators, PunctuatorNotEqual, TokKind.LssGrt);
            AddPunctuator(punctuators, PunctuatorDot, TokKind.Dot);
            AddPunctuator(punctuators, PunctuatorColon, TokKind.Colon);
            AddPunctuator(punctuators, PunctuatorCurlyOpen, TokKind.CurlyOpen);
            AddPunctuator(punctuators, PunctuatorCurlyClose, TokKind.CurlyClose);
            AddPunctuator(punctuators, PunctuatorBracketOpen, TokKind.BracketOpen);
            AddPunctuator(punctuators, PunctuatorBracketClose, TokKind.BracketClose);
            AddPunctuator(punctuators, PunctuatorAmpersand, TokKind.Ampersand);
            AddPunctuator(punctuators, PunctuatorPercent, TokKind.PercentSign);
            AddPunctuator(punctuators, PunctuatorAt, TokKind.At);
            AddPunctuator(punctuators, PunctuatorDoubleBarrelArrow, TokKind.DoubleBarrelArrow);

            // Commenting punctuators
            AddPunctuator(punctuators, PunctuatorBlockComment, TokKind.Comment);
            AddPunctuator(punctuators, PunctuatorLineComment, TokKind.Comment);

            // Localized
            AddPunctuator(punctuators, LocalizedPunctuatorListSeparator, TokKind.Comma);
            AddPunctuator(punctuators, LocalizedPunctuatorChainingSeparator, TokKind.Semicolon);

            _punctuators = punctuators;
        }

        private static bool AddPunctuator(Dictionary<string, TokKind> punctuators, string str, TokKind tid)
        {
            Contracts.AssertNonEmpty(str);

            if (punctuators.TryGetValue(str, out var tidCur))
            {
                if (tidCur == tid)
                {
                    return true;
                }

                if (tidCur != TokKind.None)
                {
                    return false;
                }
            }
            else
            {
                // Map all prefixes (that aren't already mapped) to TokKind.None.
                for (var ich = 1; ich < str.Length; ich++)
                {
                    var strTmp = str.Substring(0, ich);
                    if (!punctuators.TryGetValue(strTmp, out _))
                    {
                        punctuators.Add(strTmp, TokKind.None);
                    }
                }
            }

            punctuators[str] = tid;
            return true;
        }

        public IReadOnlyList<Token> LexSource(string text, Flags flags = Flags.None)
        {
            Contracts.AssertValue(text);

            var tokens = new List<Token>();
            StringBuilder sb = null;

            try
            {
                // This StringBuilder is used by the Lexer as a temporary storage for tokenized characters.
                sb = StringBuilderCache.Acquire(Math.Min(text.Length, DesiredStringBuilderSize));

                Token tok;
                var impl = new LexerImpl(this, text, sb, flags);

                while ((tok = impl.GetNextToken()) != null)
                {
                    tokens.Add(tok);
                }

                tokens.Add(impl.GetEof());
            }
            finally
            {
                if (sb != null)
                {
                    StringBuilderCache.Release(sb);
                }
            }

            return tokens;
        }

        public List<Token> GetTokens(string text)
        {
            Contracts.AssertValue(text);

            Token tok;
            var impl = new LexerImpl(this, text, new StringBuilder(), Flags.None);
            var tokens = new List<Token>();
            while ((tok = impl.GetNextToken()) != null)
            {
                tokens.Add(tok);
            }

            return tokens;
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

        public string GetMinifiedScript(string text, List<Token> tokens)
        {
            Contracts.AssertValue(text);
            Contracts.AssertValue(tokens);

            var stringBuilder = new StringBuilder();

            foreach (var tk in tokens)
            {
                if (tk.Kind == TokKind.Comment)
                {
                    stringBuilder.Append(tk.Span.GetFragment(text));
                }
                else if (RequiresWhiteSpace(tk))
                {
                    stringBuilder.Append(" " + tk.Span.GetFragment(text) + " ");
                }
                else
                {
                    var tokenString = tk.Span.GetFragment(text);
                    var newString = tokenString.Trim();

                    stringBuilder.Append(newString);
                }
            }

            var result = stringBuilder.ToString();
            return result;
        }

        public string RemoveWhiteSpace(string text)
        {
            Contracts.AssertValue(text);

            var tokens = GetTokens(text);
            if (tokens.Count == 1)
            {
                return text;
            }

            var textLength = text.Length;
            var result = GetMinifiedScript(text, tokens);

            return result;
        }

        // Enumerate all supported unary operator keywords.
        public static IReadOnlyList<string> GetUnaryOperatorKeywords() => _unaryOperatorKeywords;

        // Enumerate all supported binary operator keywords.
        public static IReadOnlyList<string> GetBinaryOperatorKeywords() => _binaryOperatorKeywords;

        // Enumerate all supported keywords for the given type.
        // Review hekum - should we have leftType and right type seperately?
        public static IReadOnlyList<string> GetOperatorKeywords(DType type)
        {
            Contracts.Assert(type.IsValid);

            if (type.IsPrimitive)
            {
                return _operatorKeywordsPrimitive;
            }

            // TASK 97994: Investigate and Implement the functionality if lhs of  'in' operator is a control type.
            if (type.IsAggregate || type.IsControl)
            {
                return _operatorKeywordsAggregate;
            }

            return new List<string>();
        }

        // Enumerate all supported constant keywords.
        public static IReadOnlyList<string> GetConstantKeywords(bool getParent) => getParent ? _constantKeywordsGetParent : _constantKeywordsDefault;

        // Enumerate all supported localized punctuators and their invariant counterparts.
        public IReadOnlyDictionary<string, string> GetPunctuatorsAndInvariants() => _punctuatorsAndInvariants;

        // Returns true and sets 'tid' if the specified string is a keyword.
        public static bool IsKeyword(string str, out TokKind tid)
        {
            return _keywords.TryGetValue(str, out tid);
        }

        // Returns true and set 'tid' if the specified string is a punctuator.
        // A tid of TokKind.None means it's a prefix of a valid punctuator, but isn't itself a valid punctuator.
        public bool TryGetPunctuator(string str, out TokKind tid)
        {
            return _punctuators.TryGetValue(str, out tid);
        }

        // Returns true if the specified string is a punctuator.
        public bool IsPunctuator(string str)
        {
            Contracts.AssertValue(str);

            return _punctuators.ContainsKey(str);
        }

        // Returns true if the specified character is valid as the first character of an identifier.
        // If an identifier contains any other characters, it has to be surrounded by single quotation marks.
        public static bool IsIdentStart(char ch)
        {
            if (ch >= 128)
            {
                return (CharacterUtils.GetUniCatFlags(ch) & CharacterUtils.UniCatFlags.IdentStartChar) != 0;
            }

            return ((uint)(ch - 'a') < 26) || ((uint)(ch - 'A') < 26) || (ch == '_') || (ch == IdentifierDelimiter);
        }

        // Returns true if the specified character is a valid simple identifier character.
        public static bool IsSimpleIdentCh(char ch)
        {
            if (ch >= 128)
            {
                return (CharacterUtils.GetUniCatFlags(ch) & CharacterUtils.UniCatFlags.IdentPartChar) != 0;
            }

            return ((uint)(ch - 'a') < 26) || ((uint)(ch - 'A') < 26) || ((uint)(ch - '0') <= 9) || (ch == '_');
        }

        // Returns true if the specified character constitutes a valid start for a numeric literal.
        public bool IsNumStart(char ch) => CharacterUtils.IsDigit(ch) || ch == _decimalSeparator;

        // Returns true if the specified character is the start/end identifier delimiter.
        public static bool IsIdentDelimiter(char ch) => ch == IdentifierDelimiter;

        // Returns true if the specified character starts an interpolated string.
        public static bool IsInterpolatedStringStart(char ch, char nextCh) => ch == '$' && nextCh == '\"';

        // Returns true if the specified character is an open curly bracket, used by interpolated strings.
        public static bool IsCurlyOpen(char ch) => ch == '{';

        // Returns true if the specified character is an open curly bracket, used by interpolated strings.
        public static bool IsCurlyClose(char ch) => ch == '}';

        // Returns true if the specified character is a valid string delimiter.
        public static bool IsStringDelimiter(char ch) => ch == '\"';

        // Returns true if the specified character is a new line character.
        public static bool IsNewLineCharacter(char ch) => ch == '\n';

        // Takes a valid name and changes it to an identifier, escaping if needed.
        public static string EscapeName(DName name)
        {
            Contracts.Assert(name.IsValid);
            return EscapeName(name.Value);
        }

        // Takes a valid name and changes it to an identifier, escaping if needed.
        public static string EscapeName(string name)
        {
            Contracts.Assert(DName.IsValidDName(name));

            var nameLen = name.Length;
            Contracts.Assert(nameLen > 0);

            var fEscaping = !IsIdentStart(name[0]) || IsIdentDelimiter(name[0]);
            var fFirst = true;

            StringBuilder sb = null;

            try
            {
                sb = StringBuilderCache.Acquire(nameLen);

                for (var i = fEscaping ? 0 : 1; i < nameLen; i++)
                {
                    var ch = name[i];
                    fEscaping = fEscaping || !IsSimpleIdentCh(ch);

                    if (!fEscaping)
                    {
                        continue;
                    }

                    if (fFirst)
                    {
                        sb.Append(IdentifierDelimiter);
                        sb.Append(name, 0, i);
                        fFirst = false;
                    }

                    if (ch == IdentifierDelimiter)
                    {
                        sb.Append(ch);
                    }

                    sb.Append(ch);
                }

                if (fEscaping)
                {
                    sb.Append(IdentifierDelimiter);
                    return sb.ToString();
                }

                if (!IsKeyword(name, out var kind))
                {
                    return name;
                }

                sb.Length = 0;
                sb.EnsureCapacity(nameLen + 2);

                sb.Append(IdentifierDelimiter);
                sb.Append(name);
                sb.Append(IdentifierDelimiter);

                return sb.ToString();
            }
            finally
            {
                if (sb != null)
                {
                    StringBuilderCache.Release(sb);
                }
            }
        }

        // Takes an escaped string and returns the unescaped version.
        // For ex: 'ab''c' = ab'c
        public static string UnescapeName(string name)
        {
            Contracts.AssertValueOrNull(name);

            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            var len = name.Length;
            StringBuilder sb = null;

            try
            {
                sb = StringBuilderCache.Acquire(len);

                for (var i = 0; i < name.Length; i++)
                {
                    var ch = name[i];

                    if (ch != IdentifierDelimiter)
                    {
                        sb.Append(ch);
                    }
                    else
                    {
                        if (i == 0 || i == len - 1)
                        {
                            continue;
                        }

                        if (name[i + 1] != IdentifierDelimiter)
                        {
                            continue;
                        }

                        sb.Append(ch);
                        i++;
                    }
                }

                return sb.ToString();
            }
            finally
            {
                if (sb != null)
                {
                    StringBuilderCache.Release(sb);
                }
            }
        }

        // Takes a name or an identifier and returns whether it can be a valid name and sets strNameValid to be the parsed name.
        // If the first non-space character in strIn is not a start delimiter then this is treated as a name.
        // Else if the first non-space character in strIn is the start delimiter this is treated as an identifier.
        public static bool TryNameOrIdentifierToName(string strIn, out DName name)
        {
            Contracts.AssertValueOrNull(strIn);

            if (string.IsNullOrEmpty(strIn))
            {
                name = default;
                return false;
            }

            // Find the first non space character.
            StringBuilder sb = null;

            try
            {
                sb = StringBuilderCache.Acquire(strIn.Length);

                var fIdent = false;
                var fName = false;
                int i;
                for (i = 0; i < strIn.Length; i++)
                {
                    var ch = strIn[i];
                    if (!CharacterUtils.IsSpace(ch))
                    {
                        if (ch == IdentifierDelimiter)
                        {
                            // skip the delimiter start.
                            i++;
                            fIdent = true;
                            break;
                        }

                        fName = true;
                        break;
                    }
                }

                if (fName)
                {
                    // Parse as a name.
                    var ichTrailingSpace = -1;
                    var iStart = i;

                    for (; i < strIn.Length; i++)
                    {
                        var ch = strIn[i];
                        if (!CharacterUtils.IsSpace(ch))
                        {
                            ichTrailingSpace = -1;
                        }
                        else if (ichTrailingSpace == -1)
                        {
                            ichTrailingSpace = i;
                        }

                        sb.Append(ch);
                    }

                    // Remove trailing spaces.
                    if (ichTrailingSpace != -1)
                    {
                        sb.Length = ichTrailingSpace - iStart;
                    }

                    name = new DName(sb.ToString());
                    return true;
                }

                if (!fIdent)
                {
                    name = default;
                    return false;
                }

                // Parse as an identifier.
                var fAllWhiteSpace = true;
                var fHasEndDelimiter = false;

                for (; i < strIn.Length; i++)
                {
                    var ch = strIn[i];
                    if (ch == IdentifierDelimiter)
                    {
                        i++;
                        if (i < strIn.Length && strIn[i] == IdentifierDelimiter)
                        {
                            // Escaped end delimiter
                            fAllWhiteSpace = false;
                        }
                        else
                        {
                            // end of identifier
                            fHasEndDelimiter = true;
                            break;
                        }
                    }
                    else if (fAllWhiteSpace && !CharacterUtils.IsSpace(ch))
                    {
                        fAllWhiteSpace = false;
                    }

                    sb.Append(ch);
                }

                if (fAllWhiteSpace || !fHasEndDelimiter)
                {
                    name = default;
                    return false;
                }

                // Check the remaining characters are white space.
                for (; i < strIn.Length; i++)
                {
                    if (!CharacterUtils.IsSpace(strIn[i]))
                    {
                        name = default;
                        return false;
                    }
                }

                name = new DName(sb.ToString());
                return true;
            }
            finally
            {
                if (sb != null)
                {
                    StringBuilderCache.Release(sb);
                }
            }
        }

        // Choose an unambiguous decimal separator.
        private static string ChooseDecimalSeparator(string preferred)
        {
            Contracts.AssertNonEmpty(preferred);

            if (preferred != PunctuatorDecimalSeparatorInvariant)
            {
                return preferred;
            }

            return PunctuatorDecimalSeparatorInvariant;
        }

        // Choose an unambiguous list separator.
        private static string ChooseListSeparatorPunctuator(string decimalSeparator)
        {
            // We can't use the same punctuator, since that would cause an ambiguous grammar:
            //  Foo(1,23, 3,45) could represent two distinct things in a fr-FR locale:
            //      - either the equivalent of Foo(1.23, 3.45)
            //      - or the equivalent of Foo(1, 23, 3, 45)
            if (decimalSeparator == PunctuatorCommaInvariant)
            {
                return PunctuatorSemicolonInvariant;
            }

            return PunctuatorCommaInvariant;
        }

        // Choose an unambiguous chaining punctuator.
        private static string ChooseChainingPunctuator(string listSeparator, string decimalSeparator)
        {
            Contracts.Assert(listSeparator != decimalSeparator);

            if (decimalSeparator == PunctuatorCommaInvariant)
            {
                return PunctuatorSemicolonAlt1;
            }

            Contracts.Assert(decimalSeparator == PunctuatorDecimalSeparatorInvariant);

            return PunctuatorSemicolonDefault;
        }

        private sealed class LexerImpl
        {
            // The Mode of the lexer, required because the behavior of the lexer changes
            // when lexing inside of a String Interpolation, for example $"Hello {"World"}"
            // has special lexing behavior.In theory, you could do this with just 2 modes,
            // but we are using a 3rd mode, Island, to help keep track of when we need
            // to produce IslandStart and IslandEnd tokens, which will be used by the
            // Parser to correctly organize the string interpolation into a function call.
            public enum LexerMode
            {
                Normal,
                Island,
                StringInterpolation
            }

            private readonly TexlLexer _lex;
            private readonly string _text;
            private readonly int _charCount;
            private readonly StringBuilder _sb; // Used while building a token.
            private readonly Stack<LexerMode> _modeStack;

            private int _currentTokenPos; // The start of the current token.

            public LexerImpl(TexlLexer lex, string text, StringBuilder sb, Flags flags)
            {
                Contracts.AssertValue(lex);
                Contracts.AssertValue(text);
                Contracts.AssertValue(sb);

                _lex = lex;
                _text = text;
                _charCount = _text.Length;
                _sb = sb;

                _modeStack = new Stack<LexerMode>();
                _modeStack.Push(LexerMode.Normal);
            }

            // If the mode stack is empty, this is already an parse, use NormalMode as a default
            private LexerMode CurrentMode => _modeStack.Count != 0 ? _modeStack.Peek() : LexerMode.Normal;

            private void EnterMode(LexerMode newMode)
            {
                _modeStack.Push(newMode);
            }

            private void ExitMode()
            {
                if (_modeStack.Count != 0)
                {
                    _modeStack.Pop();
                }
            }

            // Whether we've hit the end of input yet. If this returns true, ChCur will be zero.
            private bool Eof => CurrentPos >= _charCount;

            // The current position.
            private int CurrentPos { get; set; }

            // The current character. Zero if we've hit the end of input.
            private char CurrentChar => CurrentPos < _charCount ? _text[CurrentPos] : '\0';

            // Advance to the next character and returns it.
            private char NextChar()
            {
                Contracts.Assert(CurrentPos < _charCount);

                if (++CurrentPos < _charCount)
                {
                    return _text[CurrentPos];
                }

                CurrentPos = _charCount;
                return '\0';
            }

            // Return the ich character without advancing the current position.
            private char PeekChar(int ich)
            {
                Contracts.AssertIndexInclusive(ich, _text.Length - CurrentPos);
                ich += CurrentPos;
                return (ich < _charCount) ? _text[ich] : '\0';
            }

            // Return the token n away from the current position, and then
            // reset the lexer to the state it was when called
            private Token Lookahead(int n)
            {
                var lookaheadStart = CurrentPos;
                var lookaheadTokenStart = _currentTokenPos;
                Token foundTok = null;
                for (var i = 0; i <= n; i++)
                {
                    if (Eof)
                    {
                        foundTok = null;
                        break;
                    }

                    foundTok = Dispatch(true, true);
                }

                _currentTokenPos = lookaheadTokenStart;
                CurrentPos = lookaheadStart;
                return foundTok;
            }

            // Marks the beginning of the current token.
            private void StartToken()
            {
                _currentTokenPos = CurrentPos;
            }

            // Resets current read position to the beginning of the current token.
            private void ResetToken()
            {
                CurrentPos = _currentTokenPos;
            }

            private Span GetTextSpan()
            {
                return new Span(_currentTokenPos, CurrentPos);
            }

            // Form and return the next token. Returns null to signal end of input.
            public Token GetNextToken()
            {
                for (; ;)
                {
                    if (Eof)
                    {
                        return null;
                    }

                    var tok = Dispatch(true, true);
                    if (tok != null)
                    {
                        return tok;
                    }
                }
            }

            // Call once GetNextToken returns null if you need an Eof token.
            public EofToken GetEof()
            {
                Contracts.Assert(Eof);

                return new EofToken(new Span(_charCount, _charCount));
            }

            /// <summary>
            /// Forms a new token.
            /// </summary>
            /// <param name="allowContextDependentTokens">Enables the <c>%text%</c> expression support.</param>
            /// <param name="allowLocalizableTokens">Enables the <c>##text##</c> expression support.</param>
            private Token Dispatch(bool allowContextDependentTokens, bool allowLocalizableTokens)
            {
                StartToken();
                var ch = CurrentChar;
                var nextCh = PeekChar(1);

                if (CurrentMode == LexerMode.Normal || CurrentMode == LexerMode.Island)
                {
                    if (CurrentMode == LexerMode.Island && IsCurlyClose(ch))
                    {
                        // The LexerMode.Normal mode is pushed onto the mode stack every time the '{' character
                        // appears within the body of an Island, for example when using the Table function inside
                        // an interpolated string. If we are in the Island mode, it means that all the Normal
                        // modes have been popped off, i.e. all the '{' inside the Island are paired with '}'
                        // In that case just end the Island and resume parsing characters as string literals.
                        return LexIslandEnd();
                    }

                    if (_lex.IsNumStart(ch))
                    {
                        return LexNumLit();
                    }

                    if (IsIdentStart(ch))
                    {
                        return LexIdent();
                    }

                    if (IsInterpolatedStringStart(ch, nextCh))
                    {
                        return LexInterpolatedStringStart();
                    }

                    if (IsStringDelimiter(ch))
                    {
                        return LexStringLit();
                    }

                    if (CharacterUtils.IsSpace(ch) || CharacterUtils.IsLineTerm(ch))
                    {
                        return LexSpace();
                    }

                    return LexOther();
                }
                else if (IsStringDelimiter(ch) && !IsStringDelimiter(nextCh))
                {
                    return LexInterpolatedStringEnd();
                }
                else if (IsCurlyOpen(ch) && !IsCurlyOpen(nextCh))
                {
                    return LexIslandStart();
                }
                else
                {
                    return LexInterpolatedStringBody();
                }
            }

            private Token LexOther()
            {
                var punctuatorLength = 0;
                var tidPunc = TokKind.None;

                _sb.Length = 0;
                _sb.Append(CurrentChar);
                for (; ;)
                {
                    var str = _sb.ToString();
                    if (!_lex.TryGetPunctuator(str, out var tidCur))
                    {
                        break;
                    }

                    if (tidCur == TokKind.Comment)
                    {
                        tidPunc = tidCur;
                        punctuatorLength = _sb.Length;

                        return LexComment(_sb.Length);
                    }

                    if (tidCur != TokKind.None)
                    {
                        tidPunc = tidCur;
                        punctuatorLength = _sb.Length;
                    }

                    _sb.Append(PeekChar(_sb.Length));
                }

                if (punctuatorLength == 0)
                {
                    return LexError();
                }

                while (--punctuatorLength >= 0)
                {
                    NextChar();
                }

                if (tidPunc == TokKind.CurlyOpen)
                {
                    EnterMode(LexerMode.Normal);
                }

                if (tidPunc == TokKind.CurlyClose)
                {
                    ExitMode();
                }

                return new KeyToken(tidPunc, GetTextSpan());
            }

            // Called to lex a numeric literal or a Dot token.
            private Token LexNumLit()
            {
                Contracts.Assert(CharacterUtils.IsDigit(CurrentChar) || CurrentChar == _lex._decimalSeparator);

                // A dot that is not followed by a digit is just a Dot.
                if (CurrentChar == _lex._decimalSeparator && !CharacterUtils.IsDigit(PeekChar(1)))
                {
                    return LexOther();
                }

                // Decimal literal (possible floating point).
                return LexDecLit();
            }

            // Lex a decimal (double) literal.
            private Token LexDecLit()
            {
                Contracts.Assert(CharacterUtils.IsDigit(CurrentChar) || (CurrentChar == _lex._decimalSeparator && CharacterUtils.IsDigit(PeekChar(1))));

                bool hasDot = false, isCorrect = true;

                _sb.Length = 0;
                if (CurrentChar == _lex._decimalSeparator)
                {
                    Contracts.Assert(CharacterUtils.IsDigit(PeekChar(1)));
                    hasDot = true;
                }

                _sb.Append(CurrentChar);

                for (; ;)
                {
                    if (NextChar() == _lex._decimalSeparator)
                    {
                        if (hasDot)
                        {
                            isCorrect = false;
                            break;
                        }

                        hasDot = true;
                        _sb.Append(CurrentChar);
                    }
                    else
                    {
                        if (!CharacterUtils.IsDigit(CurrentChar))
                        {
                            break;
                        }

                        // Push leading zeros as well. All digits are important.
                        // We'll let the framework deal with the specifics internally.
                        _sb.Append(CurrentChar);
                    }
                }

                // Check for an exponent.
                if (CurrentChar == 'e' || CurrentChar == 'E')
                {
                    var chTmp = PeekChar(1);
                    if (CharacterUtils.IsDigit(chTmp) || (IsSign(chTmp) && CharacterUtils.IsDigit(PeekChar(2))))
                    {
                        _sb.Append(CurrentChar);
                        NextChar(); // Skip the e.
                        if (IsSign(chTmp))
                        {
                            _sb.Append(chTmp);
                            NextChar(); // Skip the sign
                        }

                        do
                        {
                            _sb.Append(CurrentChar);
                        }
                        while (CharacterUtils.IsDigit(NextChar()));
                    }
                }

                // Parsing in the current culture, to allow the CLR to correctly parse non-arabic numerals.
                if (!double.TryParse(_sb.ToString(), NumberStyles.Float, _lex._numberFormatInfo, out var value) || double.IsNaN(value) || double.IsInfinity(value))
                {
                    return isCorrect ?
                        new ErrorToken(GetTextSpan(), TexlStrings.ErrNumberTooLarge) :
                        new ErrorToken(GetTextSpan());
                }

                return new NumLitToken(value, GetTextSpan());
            }

            private bool IsSign(char ch)
            {
                return ch == PunctuatorAdd[0] || ch == PunctuatorSub[0];
            }

            // Lex an identifier.
            // If this code changes, NameValidation will probably have to change as well.
            private Token LexIdent()
            {
                var str = LexIdentCore(out var fDelimiterStart, out var fDelimiterEnd);

                var spanTok = GetTextSpan();

                // Only lex a keyword if the identifier didn't start with a delimiter.
                if (IsKeyword(str, out var tid) && !fDelimiterStart)
                {
                    // Lookahead to distinguish Keyword "and/or/not" from Function "and/or/not"
                    if ((tid == TokKind.KeyAnd || tid == TokKind.KeyOr || tid == TokKind.KeyNot) &&
                       Lookahead(0)?.Kind == TokKind.ParenOpen)
                    {
                        return new IdentToken(str, spanTok, fDelimiterStart, fDelimiterEnd);
                    }

                    return new KeyToken(tid, spanTok);
                }

                return new IdentToken(str, spanTok, fDelimiterStart, fDelimiterEnd);
            }

            // Core functionality for lexing an identifier.
            private string LexIdentCore(out bool fDelimiterStart, out bool fDelimiterEnd)
            {
                Contracts.Assert(IsIdentStart(CurrentChar));

                _sb.Length = 0;
                fDelimiterStart = IsIdentDelimiter(CurrentChar);
                fDelimiterEnd = false;

                if (!fDelimiterStart)
                {
                    // Simple identifier.
                    while (IsSimpleIdentCh(CurrentChar))
                    {
                        _sb.Append(CurrentChar);
                        NextChar();
                    }

                    return _sb.ToString();
                }

                // Delimited identifier.
                NextChar();
                var ichStrMin = CurrentPos;
                var semicolonIndex = -1;

                // Accept any characters up to the next unescaped identifier delimiter.
                // String will be corrected in the IdentToken if needed.
                for (; ;)
                {
                    if (Eof)
                    {
                        // Ident was never closed, tokenize ident up to semicolon
                        if (semicolonIndex != -1)
                        {
                            CurrentPos = ichStrMin;
                            _sb.Length = 0;
                            while (CurrentPos < semicolonIndex)
                            {
                                _sb.Append(CurrentChar);
                                NextChar();
                            }
                        }

                        break;
                    }

                    if (IsIdentDelimiter(CurrentChar))
                    {
                        if (IsIdentDelimiter(PeekChar(1)))
                        {
                            // Escaped delimiter.
                            _sb.Append(CurrentChar);
                            NextChar();
                            NextChar();
                        }
                        else
                        {
                            // End of the identifier.
                            NextChar();
                            fDelimiterEnd = true;
                            break;
                        }
                    }
                    else if (IsNewLineCharacter(CurrentChar))
                    {
                        // Terminate an identifier on a new line character
                        // Don't include the new line in the identifier
                        fDelimiterEnd = false;
                        break;
                    }
                    else if (CurrentChar.ToString() == PunctuatorSemicolonInvariant)
                    {
                        // This is to enable parser restarting
                        // Don't know if semicolon is end of identifier so we will store for fallback
                        // Some locales use ;;, so we check for match and move shift 2 characters instead of 1
                        if (_lex.TryGetPunctuator(CurrentChar.ToString(), out TokKind tid) && tid == TokKind.Semicolon)
                        {
                            semicolonIndex = semicolonIndex == -1 ? CurrentPos : semicolonIndex;
                        }
                        else if (_lex.TryGetPunctuator(CurrentChar.ToString() + _text[Math.Min(CurrentPos + 1, _charCount)].ToString(), out tid) && tid == TokKind.Semicolon)
                        {
                            semicolonIndex = semicolonIndex == -1 ? CurrentPos : semicolonIndex;
                            _sb.Append(CurrentChar);
                            NextChar();
                        }

                        _sb.Append(CurrentChar);
                        NextChar();
                    }
                    else
                    {
                        _sb.Append(CurrentChar);
                        NextChar();
                    }
                }

                return _sb.ToString();
            }

            // Lex a string.
            private Token LexStringLit()
            {
                Contracts.Assert(IsStringDelimiter(CurrentChar));

                _sb.Length = 0;

                var chDelim = CurrentChar;
                while (!Eof)
                {
                    var ch = NextChar();
                    if (ch == chDelim)
                    {
                        char nextCh;
                        if (Eof || CharacterUtils.IsLineTerm(nextCh = PeekChar(1)) || nextCh != chDelim)
                        {
                            break;
                        }

                        // If we are here, we are seeing a double quote followed immediately by another
                        // double quote. That is actually an escape sequence for double quote characters
                        // within a string literal. Excel supports the exact same escape sequence.
                        // We want to include these characters in the string literal, and keep lexing.
                        _sb.Append(ch);
                        NextChar();
                    }
                    else if (!CharacterUtils.IsFormatCh(ch))
                    {
                        _sb.Append(ch);
                    }
                }

                if (Eof)
                {
                    return new ErrorToken(GetTextSpan());
                }

                NextChar();
                return new StrLitToken(_sb.ToString(), GetTextSpan());
            }

            // Lex an interpolated string body start.
            private Token LexInterpolatedStringStart()
            {
                Contracts.Assert(IsInterpolatedStringStart(CurrentChar, PeekChar(1)));

                NextChar();
                NextChar();
                EnterMode(LexerMode.StringInterpolation);

                return new StrInterpStartToken(GetTextSpan());
            }

            // Lex an interpolated string body end.
            private Token LexInterpolatedStringEnd()
            {
                Contracts.Assert(IsStringDelimiter(CurrentChar));

                NextChar();
                ExitMode();

                return new StrInterpEndToken(GetTextSpan());
            }

            // Lex an interpolated string island start.
            private Token LexIslandStart()
            {
                Contracts.Assert(IsCurlyOpen(CurrentChar));

                NextChar();
                EnterMode(LexerMode.Island);

                return new IslandStartToken(GetTextSpan());
            }

            // Lex an interpolated string island end.
            private Token LexIslandEnd()
            {
                Contracts.Assert(IsCurlyClose(CurrentChar));

                NextChar();
                ExitMode();

                return new IslandEndToken(GetTextSpan());
            }

            // Lex a interpolated string body.
            private Token LexInterpolatedStringBody()
            {
                _sb.Length = 0;

                do
                {
                    var ch = CurrentChar;

                    if (IsStringDelimiter(ch))
                    {
                        char nextCh;
                        if (Eof || CharacterUtils.IsLineTerm(nextCh = PeekChar(1)) || !IsStringDelimiter(nextCh))
                        {
                            // Interpolated string end, do not call NextChar()
                            if (Eof)
                            {
                                return new ErrorToken(GetTextSpan());
                            }

                            return new StrLitToken(_sb.ToString(), GetTextSpan());
                        }

                        // If we are here, we are seeing a double quote followed immediately by another
                        // double quote. That is an escape sequence for double quote characters.
                        _sb.Append(ch);
                        NextChar();
                    }
                    else if (IsCurlyOpen(ch))
                    {
                        char nextCh;
                        if (Eof || CharacterUtils.IsLineTerm(nextCh = PeekChar(1)) || !IsCurlyOpen(nextCh))
                        {
                            // Island start, do not call NextChar()
                            if (Eof)
                            {
                                return new ErrorToken(GetTextSpan());
                            }

                            return new StrLitToken(_sb.ToString(), GetTextSpan());
                        }

                        // If we are here, we are seeing a open curly followed immediately by another
                        // open curly. That is an escape sequence for open curly characters.
                        _sb.Append(ch);
                        NextChar();
                    }
                    else if (IsCurlyClose(ch))
                    {
                        char nextCh;
                        if (Eof || CharacterUtils.IsLineTerm(nextCh = PeekChar(1)) || !IsCurlyClose(nextCh))
                        {
                            var res = new ErrorToken(GetTextSpan());
                            NextChar();
                            return res;
                        }

                        // If we are here, we are seeing a close curly followed immediately by another
                        // close curly. That is an escape sequence for close curly characters.
                        _sb.Append(ch);
                        NextChar();
                    }
                    else if (!CharacterUtils.IsFormatCh(ch))
                    {
                        _sb.Append(ch);
                    }

                    NextChar();
                }
                while (!Eof);

                return new ErrorToken(GetTextSpan());
            }

            // Lex a sequence of spacing characters.
            private Token LexSpace()
            {
                Contracts.Assert(CharacterUtils.IsSpace(CurrentChar) || CharacterUtils.IsLineTerm(CurrentChar));

                _sb.Length = 0;
                while (CharacterUtils.IsSpace(NextChar()) || CharacterUtils.IsLineTerm(CurrentChar))
                {
                    _sb.Append(CurrentChar);
                }

                return new WhitespaceToken(_sb.ToString(), GetTextSpan());
            }

            private Token LexComment(int commentLength)
            {
                _sb.Length = 0;
                _sb.Append(CurrentChar);
                for (var i = 1; i < commentLength; i++)
                {
                    _sb.Append(NextChar());
                }

                Contracts.Assert(_sb.ToString().Equals("/*") || _sb.ToString().Equals("//"));
                var commentEnd = _sb.ToString().StartsWith("/*") ? "*/" : "\n";

                // Comment initiation takes up two chars, so must - 1 to get start
                var startingPosition = CurrentPos - 1;

                while (CurrentPos < _text.Length)
                {
                    _sb.Append(NextChar());
                    var str = _sb.ToString();

                    // "str.Length >= commentLength + commentEnd.Length"  ensures block comment of "/*/"
                    // does not satisfy starts with "/*" and ends with "*/" conditions
                    if (str.EndsWith(commentEnd) && str.Length >= commentLength + commentEnd.Length)
                    {
                        break;
                    }
                }

                // Trailing comment space
                while (CurrentPos < _text.Length)
                {
                    var nxtChar = NextChar();

                    // If nxtChar is not whitespace, no need to handle trailing whitespace
                    if (!char.IsWhiteSpace(nxtChar) || commentEnd != "*/")
                    {
                        break;
                    }

                    // Handle/Preserve trailing white space and line breaks for block comments
                    if (IsNewLineCharacter(nxtChar))
                    {
                        _sb.Append(nxtChar);
                        ++CurrentPos;
                        break;
                    }
                }

                // Preceding,
                while (startingPosition > 0)
                {
                    var previousChar = _text[startingPosition - 1];
                    if (!char.IsWhiteSpace(previousChar))
                    {
                        break;
                    }

                    if (IsNewLineCharacter(previousChar))
                    {
                        _sb.Insert(0, previousChar);
                        _currentTokenPos = --startingPosition;
                        break;
                    }

                    startingPosition--;
                }

                var commentToken = new CommentToken(_sb.ToString(), GetTextSpan());
                if (_sb.ToString().Trim().StartsWith("/*") && !_sb.ToString().Trim().EndsWith("*/"))
                {
                    commentToken.IsOpenBlock = true;
                }

                return commentToken;
            }

            // Returns specialized token for unexpected character errors.
            private Token LexError()
            {
                if (CurrentChar > 255)
                {
                    var position = CurrentPos;
                    var unexpectedChar = Convert.ToUInt16(CurrentChar).ToString("X4");
                    NextChar();
                    return new ErrorToken(GetTextSpan(), TexlStrings.UnexpectedCharacterToken, string.Concat("U+", unexpectedChar), position);
                }
                else
                {
                    NextChar();
                    return new ErrorToken(GetTextSpan());
                }
            }
        }
    }
}
