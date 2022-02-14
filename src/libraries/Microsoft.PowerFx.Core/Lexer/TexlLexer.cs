// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

// Used as a temporary storage for LexerImpl class.
// LexerImpl is private, so we cannot define 'using' for it here - TexlLexer instead.
using StringBuilderCache = Microsoft.PowerFx.Core.Utils.StringBuilderCache<Microsoft.PowerFx.Core.Lexer.TexlLexer>;

namespace Microsoft.PowerFx.Core.Lexer
{
    // TEXL expression lexer
    internal sealed class TexlLexer
    {
        [Flags]
        public enum Flags
        {
            None,
            AllowReplaceableTokens = 1 << 0
        }

        // List and decimal separators.
        // These are the global settings, borrowed from the OS, and will be settable by the user according to their preferences.
        // If there is a collision between the two, the list separator automatically becomes ;.
        public string LocalizedPunctuatorDecimalSeparator { get; }

        public string LocalizedPunctuatorListSeparator { get; }

        // The chaining operator has to be disambiguated accordingly.
        public string LocalizedPunctuatorChainingSeparator { get; }

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
        public const string UnicodePrefix = "U+";

        // These puntuators are related to commenting in the formula bar
        public const string PunctuatorBlockComment = "/*";
        public const string PunctuatorLineComment = "//";

        public const string LocalizedTokenDelimiterStr = "##";
        public const string ContextDependentTokenDelimiterStr = "%";
        private const char LocalizedTokenDelimiterChar = '#';
        private const char ContextDependentTokenDelimiterChar = '%';

        // Defaults and options for disambiguation
        private const string PunctuatorCommaDefault = PunctuatorCommaInvariant;
        private const string PunctuatorSemicolonDefault = PunctuatorSemicolonInvariant;
        private const string PunctuatorSemicolonAlt1 = ";;";
        private const string PunctuatorSemicolonAlt2 = PunctuatorCommaInvariant;

        // Thousands separators are not currently supported by the language (in any locale).
        private readonly char _thousandSeparator = '\0';
        private readonly Dictionary<string, TokKind> _keywords;
        private readonly Dictionary<string, TokKind> _punctuators;
        private readonly char _decimalSeparator;

        // These are the decimal separators supported by the language in V1.
        // Unicode 00B7 represents mid-dot.
        // For anything else we'll fall back to invariant.
        private const string SupportedDecimalSeparators = ".,;`\u00b7";

        // Limits the StringBuilderCache TLS memory usage for LexerImpl.
        // Usually our tokens are less than 128 characters long, unless it's a large string.
        private const int DesiredStringBuilderSize = 128;

        public CultureInfo Culture { get; private set; }

        private Tuple<string, Flags, Token[]> _cache;

        // We store a list of cached Lexers, based on locale, so we can create new ones much more efficiently
        // In normal app usage we only have two locales anyway (null and the user's) but Tests use more
        // The Key to this dictionary is the CultureName
        private static Dictionary<string, TexlLexer> _prebuiltLexers = new Dictionary<string, TexlLexer>();

        private static volatile TexlLexer _lex;

        private string[] _unaryOperatorKeywords;
        private string[] _binaryOperatorKeywords;
        private string[] _operatorKeywordsPrimitive;
        private string[] _operatorKeywordsAggregate;
        private string[] _constantKeywordsDefault;
        private string[] _constantKeywordsGetParent;
        private IDictionary<string, string> _punctuatorsAndInvariants;

        // Pretty Print defaults
        public const string FourSpaces = "    ";
        public const string LineBreakAndfourSpaces = "\n    ";

        static TexlLexer()
        {
            StringBuilderCache.SetMaxBuilderSize(DesiredStringBuilderSize);
        }

        public static TexlLexer LocalizedInstance
        {
            get
            {
                if (_lex == null)
                {
                    Interlocked.CompareExchange(ref _lex, new TexlLexer((ILanguageSettings)null), null);
                }

                return _lex;
            }

            set
            {
                Contracts.AssertValue(value);
                _lex = value;
            }
        }

        public static IList<string> GetKeywordDictionary()
        {
            IList<string> strList = new List<string>(LocalizedInstance._keywords.Count);

            var keywords = LocalizedInstance._keywords;

            foreach (var keyword in keywords.Keys)
            {
                strList.Add(keyword);
            }

            return strList;
        }

        // When loc is null, this creates a new lexer instance for the current locale & language settings.
        public static TexlLexer NewInstance(ILanguageSettings loc)
        {
            Contracts.AssertValueOrNull(loc);

            if (loc != null)
            {
                if (_prebuiltLexers.TryGetValue(loc.CultureName, out var lexer))
                {
                    // In the common case we can built a fresh Lexer based on an existing one using the same locale
                    return new TexlLexer(lexer);
                }

                // Locale never seen before, so make a fresh Lexer the slow way
                lexer = new TexlLexer(loc);
                _prebuiltLexers.Add(loc.CultureName, lexer);
                return lexer;
            }

            return new TexlLexer(loc);
        }

        public static void Reset()
        {
            _prebuiltLexers = new Dictionary<string, TexlLexer>();
        }

        // If we are passed an invalid culture, lets fallback to something safe
        private static CultureInfo CreateCultureInfo(string locale)
        {
            try
            {
                return new CultureInfo(locale);
            }
            catch
            {
                return new CultureInfo("en-US");
            }
        }

        // Used to control the current locale for tests.
        public void SetLocale_TestOnly(string loc)
        {
            Contracts.AssertValue(loc);

            Culture = CreateCultureInfo(loc);
        }

        private TexlLexer(ILanguageSettings loc)
        {
            Contracts.AssertValueOrNull(loc);

            var fallBack = false;
            if (loc != null)
            {
                // Use the punctuators specified by the given ILanguageSettings instance.
                // If any are missing, fall back to the default settings.
                if (!loc.InvariantToLocPunctuatorMap.TryGetValue(PunctuatorDecimalSeparatorInvariant, out var locDecSeparator) ||
                    !loc.InvariantToLocPunctuatorMap.TryGetValue(PunctuatorCommaInvariant, out var locListSeparator) ||
                    !loc.InvariantToLocPunctuatorMap.TryGetValue(PunctuatorSemicolonInvariant, out var locChainSeparator))
                {
                    locDecSeparator = string.Empty;
                    locListSeparator = string.Empty;
                    locChainSeparator = string.Empty;
                    fallBack = true;
                }

                LocalizedPunctuatorDecimalSeparator = locDecSeparator;
                LocalizedPunctuatorListSeparator = locListSeparator;
                LocalizedPunctuatorChainingSeparator = locChainSeparator;
            }

            if (fallBack || loc == null)
            {
                // For V1 we'll pull the glob settings for the current language, and
                // we'll adjust the ones we can't support. For example, we won't be able to
                // support '(' for a decimal separator.
                Culture = CreateCultureInfo(loc != null ? loc.CultureName : CultureInfo.CurrentCulture.Name);

                // List and decimal separators.
                // These are the default global settings. If there is a collision between the two,
                // the list separator automatically becomes ;.
                LocalizedPunctuatorDecimalSeparator = ChooseDecimalSeparator(Culture.NumberFormat.NumberDecimalSeparator);
                LocalizedPunctuatorListSeparator = ChooseCommaPunctuator(Culture.TextInfo.ListSeparator);

                // The chaining operator has to be disambiguated accordingly.
                LocalizedPunctuatorChainingSeparator = ChooseChainingPunctuator();
            }
            else
            {
                // Form a culture object suitable for parsing numerics.
                Culture = CreateCultureInfo(loc.CultureName);
            }

            // Tweak the culture so it respects the lexer's disambiguated separators.
            Culture.NumberFormat.NumberDecimalSeparator = LocalizedPunctuatorDecimalSeparator;
            Culture.TextInfo.ListSeparator = LocalizedPunctuatorListSeparator;

            // Only one-character strings are supported for the decimal separator.
            Contracts.Assert(LocalizedPunctuatorDecimalSeparator.Length == 1);
            _decimalSeparator = LocalizedPunctuatorDecimalSeparator[0];

            _keywords = new Dictionary<string, TokKind>();
            _punctuators = new Dictionary<string, TokKind>();
            _cache = null;

            // Punctuators
            AddPunctuator(PunctuatorOr, TokKind.Or);
            AddPunctuator(PunctuatorAnd, TokKind.And);
            AddPunctuator(PunctuatorBang, TokKind.Bang);
            AddPunctuator(PunctuatorAdd, TokKind.Add);
            AddPunctuator(PunctuatorSub, TokKind.Sub);
            AddPunctuator(PunctuatorMul, TokKind.Mul);
            AddPunctuator(PunctuatorDiv, TokKind.Div);
            AddPunctuator(PunctuatorCaret, TokKind.Caret);
            AddPunctuator(PunctuatorParenOpen, TokKind.ParenOpen);
            AddPunctuator(PunctuatorParenClose, TokKind.ParenClose);
            AddPunctuator(PunctuatorEqual, TokKind.Equ);
            AddPunctuator(PunctuatorLess, TokKind.Lss);
            AddPunctuator(PunctuatorLessOrEqual, TokKind.LssEqu);
            AddPunctuator(PunctuatorGreater, TokKind.Grt);
            AddPunctuator(PunctuatorGreaterOrEqual, TokKind.GrtEqu);
            AddPunctuator(PunctuatorNotEqual, TokKind.LssGrt);
            AddPunctuator(LocalizedPunctuatorListSeparator, TokKind.Comma);
            AddPunctuator(PunctuatorDot, TokKind.Dot);
            AddPunctuator(PunctuatorColon, TokKind.Colon);
            AddPunctuator(PunctuatorCurlyOpen, TokKind.CurlyOpen);
            AddPunctuator(PunctuatorCurlyClose, TokKind.CurlyClose);
            AddPunctuator(PunctuatorBracketOpen, TokKind.BracketOpen);
            AddPunctuator(PunctuatorBracketClose, TokKind.BracketClose);
            AddPunctuator(PunctuatorAmpersand, TokKind.Ampersand);
            AddPunctuator(PunctuatorPercent, TokKind.PercentSign);
            AddPunctuator(LocalizedPunctuatorChainingSeparator, TokKind.Semicolon);
            AddPunctuator(PunctuatorAt, TokKind.At);

            //Commenting punctuators
            AddPunctuator(PunctuatorBlockComment, TokKind.Comment);
            AddPunctuator(PunctuatorLineComment, TokKind.Comment);

            // Keywords
            AddKeyword(KeywordTrue, TokKind.True);
            AddKeyword(KeywordFalse, TokKind.False);
            AddKeyword(KeywordIn, TokKind.In);
            AddKeyword(KeywordExactin, TokKind.Exactin);
            AddKeyword(KeywordSelf, TokKind.Self);
            AddKeyword(KeywordParent, TokKind.Parent);
            AddKeyword(KeywordAnd, TokKind.KeyAnd);
            AddKeyword(KeywordOr, TokKind.KeyOr);
            AddKeyword(KeywordNot, TokKind.KeyNot);
            AddKeyword(KeywordAs, TokKind.As);

            PopulateKeywordArrays();
        }

        // This creates a new Lexer with the same locale as the original, which is a lot less expensive than spinning up a whole new one
        private TexlLexer(TexlLexer original)
        {
            Contracts.AssertValue(original);

            LocalizedPunctuatorDecimalSeparator = original.LocalizedPunctuatorDecimalSeparator;
            LocalizedPunctuatorListSeparator = original.LocalizedPunctuatorListSeparator;
            LocalizedPunctuatorChainingSeparator = original.LocalizedPunctuatorChainingSeparator;

            _thousandSeparator = original._thousandSeparator;

            _cache = null;

            // Note that the following objects are NOT Cloned, they are the same as the originals, as they are never written to after initialization
            _keywords = original._keywords;
            _punctuators = original._punctuators;
            _decimalSeparator = original._decimalSeparator;
            Culture = original.Culture;

            PopulateKeywordArrays();
        }

        private void AddKeyword(string str, TokKind tid)
        {
            Contracts.AssertNonEmpty(str);
            _keywords.Add(str, tid);
        }

        private bool AddPunctuator(string str, TokKind tid)
        {
            Contracts.AssertNonEmpty(str);

            if (_punctuators.TryGetValue(str, out var tidCur))
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
                    if (!_punctuators.TryGetValue(strTmp, out var tidTmp))
                    {
                        _punctuators.Add(strTmp, TokKind.None);
                    }
                }
            }

            _punctuators[str] = tid;
            return true;
        }

        private void PopulateKeywordArrays()
        {
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

            _punctuatorsAndInvariants = new Dictionary<string, string>
            {
                { LocalizedPunctuatorDecimalSeparator, "." },
                { LocalizedPunctuatorListSeparator,            "," },
                { LocalizedPunctuatorChainingSeparator,        ";" }
            };
        }

        public Token[] LexSource(string text, Flags flags = Flags.None)
        {
            Contracts.AssertValue(text);

            // Check the cache
            if (_cache != null)
            {
                Contracts.AssertValue(_cache.Item1);
                Contracts.AssertValue(_cache.Item3);

                // Cache hit
                if (text == _cache.Item1 && flags == _cache.Item2)
                {
                    return _cache.Item3;
                }
            }

            // Cache miss
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

            // Update the cache and return the result
            var tokensArr = tokens.ToArray();
            _cache = new Tuple<string, Flags, Token[]>(text, flags, tokensArr);
            return tokensArr;
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
        public string[] GetUnaryOperatorKeywords() => _unaryOperatorKeywords;

        // Enumerate all supported binary operator keywords.
        public string[] GetBinaryOperatorKeywords() => _binaryOperatorKeywords;

        // Enumerate all supported keywords for the given type.
        // Review hekum - should we have leftType and right type seperately?
        public string[] GetOperatorKeywords(DType type)
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

            return new string[0];
        }

        // Enumerate all supported constant keywords.
        public string[] GetConstantKeywords(bool getParent) => getParent ? _constantKeywordsGetParent : _constantKeywordsDefault;

        // Enumerate all supported localized punctuators and their invariant counterparts.
        public IDictionary<string, string> GetPunctuatorsAndInvariants() => _punctuatorsAndInvariants;

        // Returns true and sets 'tid' if the specified string is a keyword.
        public bool IsKeyword(string str, out TokKind tid)
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

        // Returns true if the specified character is a valid context-dependent token delimiter - '%'.
        public static bool IsContextDependentTokenDelimiter(char ch) => ch == ContextDependentTokenDelimiterChar;

        // Returns true if the next and current characters form the localizable token delimiter - '##'.
        public static bool IsLocalizableTokenDelimiter(char ch, char nextCh) => ch == LocalizedTokenDelimiterChar && nextCh == LocalizedTokenDelimiterChar;

        // Takes a valid name and changes it to an identifier, escaping if needed.
        public static string EscapeName(DName name)
        {
            Contracts.Assert(name.IsValid);
            return EscapeName(name.Value);
        }

        // Takes a valid name and changes it to an identifier, escaping if needed.
        public static string EscapeName(string name, TexlLexer instance = null)
        {
            Contracts.Assert(DName.IsValidDName(name));
            Contracts.AssertValueOrNull(instance);

            instance = instance ?? LocalizedInstance;

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

                if (!instance.IsKeyword(name, out var kind))
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
        private string ChooseDecimalSeparator(string preferred)
        {
            Contracts.AssertNonEmpty(preferred);

            if (preferred.Length == 1 && SupportedDecimalSeparators.Contains(preferred))
            {
                return preferred;
            }

            return PunctuatorDecimalSeparatorInvariant;
        }

        // Choose an unambiguous list separator.
        private string ChooseCommaPunctuator(string preferred)
        {
            Contracts.AssertNonEmpty(preferred);

            // We can't use the same punctuator, since that would cause an ambiguous grammar:
            //  Foo(1,23, 3,45) could represent two distinct things in a fr-FR locale:
            //      - either the equivalent of Foo(1.23, 3.45)
            //      - or the equivalent of Foo(1, 23, 3, 45)
            if (preferred != LocalizedPunctuatorDecimalSeparator)
            {
                return preferred;
            }

            // Try to use PunctuatorCommaDefault, if possible.
            if (preferred != PunctuatorCommaDefault)
            {
                return PunctuatorCommaDefault;
            }

            // Both use comma. Choose ; instead.
            return PunctuatorSemicolonDefault;
        }

        // Choose an unambiguous chaining punctuator.
        private string ChooseChainingPunctuator()
        {
            Contracts.Assert(LocalizedPunctuatorListSeparator != LocalizedPunctuatorDecimalSeparator);

            if (LocalizedPunctuatorDecimalSeparator != PunctuatorSemicolonDefault)
            {
                // Common case, for en-US: use the default chaining punctuator if possible.
                if (LocalizedPunctuatorListSeparator != PunctuatorSemicolonDefault)
                {
                    return PunctuatorSemicolonDefault;
                }

                // Use PunctuatorSemicolonAlt1 if possible.
                if (LocalizedPunctuatorDecimalSeparator != PunctuatorSemicolonAlt1)
                {
                    return PunctuatorSemicolonAlt1;
                }

                // Fallback
                return PunctuatorSemicolonAlt2;
            }

            // The default punctuator is not available. Use the PunctuatorSemicolonAlt1 if possible.
            Contracts.Assert(LocalizedPunctuatorDecimalSeparator == PunctuatorSemicolonDefault);
            if (LocalizedPunctuatorListSeparator != PunctuatorSemicolonAlt1)
            {
                return PunctuatorSemicolonAlt1;
            }

            // Fallback
            return PunctuatorSemicolonAlt2;
        }

        internal static void ChoosePunctuators(ILanguageSettings loc, out string dec, out string comma, out string chaining)
        {
            var lexer = NewInstance(loc);
            dec = lexer.LocalizedPunctuatorDecimalSeparator;
            comma = lexer.LocalizedPunctuatorListSeparator;
            chaining = lexer.LocalizedPunctuatorChainingSeparator;
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
            private readonly bool _allowReplaceableTokens;
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
                _allowReplaceableTokens = flags.HasFlag(Flags.AllowReplaceableTokens);

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

                    if (_allowReplaceableTokens)
                    {
                        if (allowContextDependentTokens && IsContextDependentTokenDelimiter(ch))
                        {
                            return LexContextDependentTokenLit();
                        }

                        if (allowLocalizableTokens && IsLocalizableTokenDelimiter(ch, nextCh))
                        {
                            return LexLocalizableTokenLit();
                        }
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
                            // TASK: 69508: Globalization: Thousand separator code is disabled.
                            if (CurrentChar != _lex._thousandSeparator || _lex._thousandSeparator == '\0' || !CharacterUtils.IsDigit(PeekChar(1)))
                            {
                                break;
                            }

                            NextChar();
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
                if (!double.TryParse(_sb.ToString(), NumberStyles.Float, _lex.Culture, out var value) || double.IsNaN(value) || double.IsInfinity(value))
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
                if (_lex.IsKeyword(str, out var tid) && !fDelimiterStart)
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

                // Accept any characters up to the next unescaped identifier delimiter.
                // String will be corrected in the IdentToken if needed.
                for (; ;)
                {
                    if (Eof)
                    {
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

            // Lex a context-dependent token, wrapped with '%'.
            private Token LexContextDependentTokenLit()
            {
                // Minimum non-empty block length.
                const int minStringLength = 3;

                var ch = CurrentChar;
                Contracts.Assert(IsContextDependentTokenDelimiter(ch));

                _sb.Length = 0;
                _sb.Append(ContextDependentTokenDelimiterChar);

                while (!Eof)
                {
                    ch = NextChar();

                    if (IsContextDependentTokenDelimiter(ch))
                    {
                        _sb.Append(ContextDependentTokenDelimiterChar);
                        break;
                    }

                    if (!CharacterUtils.IsFormatCh(ch))
                    {
                        _sb.Append(ch);
                    }
                }

                if (Eof || _sb.Length < minStringLength)
                {
                    ResetToken();
                    return Dispatch(false, true);
                }

                NextChar();
                return new ReplaceableToken(_sb.ToString(), GetTextSpan());
            }

            // Lex a localizable token, wrapped with '##'.
            private Token LexLocalizableTokenLit()
            {
                // Minimum non-empty block length.
                const int minStringLength = 5;

                var ch = CurrentChar;
                var nextCh = Eof ? '\0' : NextChar();
                Contracts.Assert(IsLocalizableTokenDelimiter(ch, nextCh));

                _sb.Length = 0;
                _sb.Append(LocalizedTokenDelimiterStr);

                while (!Eof)
                {
                    ch = NextChar();
                    nextCh = Eof ? '\0' : PeekChar(1);

                    if (IsLocalizableTokenDelimiter(ch, nextCh))
                    {
                        // Make sure to move past the character we peeked before.
                        NextChar();

                        _sb.Append(LocalizedTokenDelimiterStr);
                        break;
                    }

                    if (!CharacterUtils.IsFormatCh(ch))
                    {
                        _sb.Append(ch);
                    }
                }

                if (Eof || _sb.Length < minStringLength)
                {
                    ResetToken();
                    return Dispatch(true, false);
                }

                NextChar();
                return new ReplaceableToken(_sb.ToString(), GetTextSpan());
            }

            // Returns specialized token for unexpected character errors.
            private Token LexError()
            {
                if (CurrentChar > 255)
                {
                    var position = CurrentPos;
                    var unexpectedChar = Convert.ToUInt16(CurrentChar).ToString("X4");
                    NextChar();
                    return new ErrorToken(GetTextSpan(), TexlStrings.UnexpectedCharacterToken, string.Concat(UnicodePrefix, unexpectedChar), position);
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
