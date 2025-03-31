// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

#if MATCHCOMPARE

// This file implements our Regular Expression functions using PCRE2 instead of .NET.
// We run tests with this to find semantic differences between our regular expression language and what Excel supports.
// To run this code, make sure that pcre2-16.dll in your path, built from https://github.com/PCRE2Project/pcre2
// with cmake-gui, shared library, AnyCRLF, UTF and UDP suppport.  When done properly, "pcre2test -C" will display:
//   C:\>pcre2test -C
//   PCRE2 version 10.44 2024-06-07
//   Compiled with
//     8-bit support
//     16-bit support
//     32-bit support
//     UTF and UCP support (Unicode version 15.0.0)
//     No just-in-time compiler support
//     Default newline sequence is ANYCRLF
//     \R matches all Unicode newlines
//     \C is supported
//     Internal link size = 2
//     Parentheses nest limit = 250
//     Default heap limit = 20000000 kibibytes
//     Default match limit = 10000000
//     Default depth limit = 10000000
//     pcre2test has neither libreadline nor libedit support

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    public class RegEx_PCRE2
    {
        internal abstract class PCRE2_RegexCommonImplementation : Library.RegexCommonImplementation
        {
            internal static class NativeMethods
            {
                // Use version 10.45 or later, as there are bugfixes to pick up that aren't in 10.44
                // Edit the source and comment out any reference to \u180e, about 6 of them. You don't need to worry about pcre2_study.c. PCRE2 treats this as a space character, but few others do.

                [DllImport("pcre2-16.dll", CharSet = CharSet.Unicode)]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern IntPtr pcre2_compile_16(string pattern, int patternLength, uint patternOptions, ref int errorNumber, ref int errorOffset, IntPtr context);

                [DllImport("pcre2-16.dll", CharSet = CharSet.Unicode)]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern int pcre2_match_16(IntPtr code, string subject, int subjectLength, int subjectOffset, uint subjectOptions, IntPtr matchData, IntPtr matchContext);

                [DllImport("pcre2-16.dll", CharSet = CharSet.Unicode)]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern int pcre2_exec_16(IntPtr code, IntPtr extra, string subject, int subjectLength, int subjectOffset, uint subjectOptions, IntPtr ovector, IntPtr ovectorSize);

                [DllImport("pcre2-16.dll")]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern IntPtr pcre2_match_data_create_16(int ovecSize, IntPtr generalContext);

                [DllImport("pcre2-16.dll")]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern IntPtr pcre2_match_data_create_from_pattern_16(IntPtr code, IntPtr generalContext);

                [DllImport("pcre2-16.dll")]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern int pcre2_get_startchar_16(IntPtr matchData);

                [DllImport("pcre2-16.dll")]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern int pcre2_get_ovector_count_16(IntPtr matchData);

                [DllImport("pcre2-16.dll")]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern IntPtr pcre2_get_ovector_pointer_16(IntPtr matchData);

                [DllImport("pcre2-16.dll", CharSet = CharSet.Unicode)]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern int pcre2_substring_number_from_name_16(IntPtr code, string name);

                [DllImport("pcre2-16.dll", CharSet = CharSet.Unicode)]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern void pcre2_match_data_free_16(IntPtr data);

                [DllImport("pcre2-16.dll", CharSet = CharSet.Unicode)]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern void pcre2_code_free_16(IntPtr code);

                [DllImport("pcre2-16.dll", CharSet = CharSet.Unicode)]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern int pcre2_get_error_message_16(int code, IntPtr buffer, int bufferLength);

                [DllImport("pcre2-16.dll", CharSet = CharSet.Unicode)]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern int pcre2_set_compile_extra_options_16(IntPtr context, uint extraOptions);

                [DllImport("pcre2-16.dll", CharSet = CharSet.Unicode)]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern IntPtr pcre2_compile_context_create_16(IntPtr generalContext);

                [DllImport("pcre2-16.dll", CharSet = CharSet.Unicode)]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern int pcre2_set_newline_16(IntPtr generalContext, uint newlineOptions);
            }

            internal enum PCRE2_OPTIONS : uint
            {
                CASELESS = 0x00000008,
                MULTILINE = 0x00000400,
                DOTALL = 0x00000020,
                EXTENDED = 0x00000080,
                UCP = 0x00020000,
                UTF = 0x00080000,
                NO_AUTO_CAPTURE = 0x00002000,
                ALT_BSUX = 0x00000002,
            }

            // from https://www.pcre.org/current/doc/html/pcre2api.html#SEC16
            // PCRE2 supports five different conventions for indicating line breaks in strings:
            // a single CR(carriage return) character, a single LF(linefeed) character,
            // the two-character sequence CRLF, any of the three preceding, or any Unicode newline sequence.
            // The Unicode newline sequences are the three just mentioned, plus the single characters VT(vertical tab, U+000B),
            // FF(form feed, U+000C), NEL(next line, U+0085), LS(line separator, U+2028), and PS(paragraph separator, U+2029).

            internal enum PCRE2_NEWLINE : uint
            {
                LF = 2,
                CRLF = 3,
                ANY = 4, // unicode
                ANYCRLF = 5, // \r, \n, and \r\n
            }

            internal enum PCRE2_EXTRA_OPTIONS : uint
            {
                ALLOW_SURROGATE_ESCAPES = 0x00000001,
                ALT_BSUX = 0x00000020,
            }

            internal enum PCRE2_MATCH_OPTIONS : uint
            {
                NOTEMPTY = 0x00000004,
                NOTEMPTY_ATSTART = 0x00000008,
                ANCHORED = 0x80000000,
            }

            internal enum PCRE2_RETURNCODES : int
            {
                ERROR_NOMATCH = -1,
                ERROR_PARIAL = -2,
            }

            private static readonly Mutex PCRE2Mutex = new Mutex();  // protect concurrent access to the node process

            internal static FormulaValue Match(string subject, string pattern, string flags, bool matchAll = false)
            {
                int errorNumber = 0;
                int errorOffset = 0;
                IntPtr matchContext = (IntPtr)0;
                IntPtr generalContext = (IntPtr)0;

                PCRE2_OPTIONS pcreOptions = PCRE2_OPTIONS.UCP | PCRE2_OPTIONS.UTF;
                RegexOptions options = RegexOptions.None;

                Match inlineOptions = Regex.Match(pattern, @"^\(\?([imnsx]+)\)");
                if (inlineOptions.Success)
                {
                    flags = flags + inlineOptions.Groups[1];
                    pattern = pattern.Substring(inlineOptions.Length);
                }

                if (!flags.Contains('N'))
                {
                    pcreOptions |= PCRE2_OPTIONS.NO_AUTO_CAPTURE;
                    options |= RegexOptions.ExplicitCapture;
                }

                if (flags.Contains('i'))
                {
                    pcreOptions |= PCRE2_OPTIONS.CASELESS;
                    options |= RegexOptions.IgnoreCase;
                }

                if (flags.Contains('m'))
                {
                    pcreOptions |= PCRE2_OPTIONS.MULTILINE;
                    options |= RegexOptions.Multiline;
                }

                if (flags.Contains('s'))
                {
                    pcreOptions |= PCRE2_OPTIONS.DOTALL;
                    options |= RegexOptions.Singleline;
                }

                if (flags.Contains('x'))
                {
                    var inCharacterClass = false;
                    var alteredPattern = new StringBuilder();

                    foreach (char p in pattern)
                    {
                        if (p == '[')
                        {
                            inCharacterClass = true;
                        }
                        else if (p == ']')
                        {
                            inCharacterClass = false;
                        }

                        // replace whitespace characters that PCRE2 doesn't support with a simple ' '; all newlines, space, and tab are fine
                        if (!inCharacterClass && MatchWhiteSpace.IsSpaceNewLine(p) && !MatchWhiteSpace.IsNewLine(p) && p != ' ' && p != '\t')
                        {
                            alteredPattern.Append(' ');
                        }
                        else
                        {
                            alteredPattern.Append(p);
                        }
                    }

                    // add a \n so that we can add a $ at the end, in case there is an unterminated pound comment
                    pattern = alteredPattern + "#\n";

                    pcreOptions |= PCRE2_OPTIONS.EXTENDED;
                    options |= RegexOptions.IgnorePatternWhitespace;
                }

                if (flags.Contains('^') && (pattern.Length == 0 || pattern[0] != '^'))
                {
                    pattern = "^" + pattern;
                }

                if (flags.Contains('$') && (pattern.Length == 0 || pattern[pattern.Length - 1] != '$'))
                {
                    pattern = pattern + "$";
                }

                // pcre2 does not allow surrogate pairs in the form /uxxxx/uyyyy as each /u individually is not a valid Unicode code point
                // so we need to translate out of those pairs here, into a single /u{...} code point.
                StringBuilder patternSurrogates = new StringBuilder();

                for (int i = 0; i < pattern.Length; i++)
                {
                    if (pattern[i] == '\\' && i + 2 < pattern.Length && pattern[i + 1] == 'u')
                    {
                        Match m;

                        if ((m = new Regex("^\\\\u(?<high>[0-9a-fA-F]{4})\\\\u(?<low>[0-9a-fA-F]{4})").Match(pattern, i)).Success &&
                           int.TryParse(m.Groups["high"].Value, NumberStyles.HexNumber, null, out var high) && char.IsHighSurrogate((char)high) &&
                           int.TryParse(m.Groups["low"].Value, NumberStyles.HexNumber, null, out var low) && char.IsLowSurrogate((char)low))
                        {
                            patternSurrogates.Append("\\x{" + Convert.ToString(((high - 0xd800) * 0x400) + (low - 0xdc00) + 0x10000, 16) + "}");
                            i += m.Length - 1;
                        }
                        else if (i + 3 < pattern.Length && pattern[i + 2] == '{' &&
                                 (m = new Regex("^\\\\u\\{(?<hex>[0-9a-fA-F]{1,})\\}").Match(pattern, i)).Success &&
                                 int.TryParse(m.Groups["hex"].Value, NumberStyles.HexNumber, null, out var hex) && hex >= 0 && hex <= 0x10ffff)
                        {
                            patternSurrogates.Append("\\x{" + Convert.ToString(hex, 16) + "}");
                            i += m.Length - 1;
                        }
                        else if (i + 5 < pattern.Length)
                        {
                            patternSurrogates.Append("\\x{" + pattern[i + 2] + pattern[i + 3] + pattern[i + 4] + pattern[i + 5] + "}");
                            i += 5;
                        }
                    }
                    else
                    {
                        patternSurrogates.Append(pattern[i]);
                    }
                }

                var pcrePattern = patternSurrogates.ToString();

                // uses .NET compatible pattern
                var (regexAltered, regexOptions) = AlterRegex_DotNet(pattern, flags);
                Regex rex = new Regex(regexAltered, regexOptions, new TimeSpan(0, 0, 1));

                // pcre2 should be thread safe, but we have seen odd behaviors, just making sure
                PCRE2Mutex.WaitOne();

                var context = NativeMethods.pcre2_compile_context_create_16(generalContext);
                NativeMethods.pcre2_set_newline_16(context, (uint)PCRE2_NEWLINE.ANY);

                var encoder = new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true);

                var code = NativeMethods.pcre2_compile_16(pcrePattern, pcrePattern.Length, (uint)pcreOptions, ref errorNumber, ref errorOffset, context);
                if (code == IntPtr.Zero)
                {
                    byte[] buffer = new byte[4096];

                    GCHandle pinnedArray = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    IntPtr pointer = pinnedArray.AddrOfPinnedObject();

                    NativeMethods.pcre2_get_error_message_16(errorNumber, pointer, buffer.Length);
                    var message = System.Text.Encoding.Unicode.GetString(buffer);
                    var fullMessage = $"PCRE2 error compiling {pattern}, errorNumber={errorNumber} ({message}), errorOffset={errorOffset}";

                    pinnedArray.Free();

                    PCRE2Mutex.ReleaseMutex();
                    throw new Exception(fullMessage);
                }

                var md = NativeMethods.pcre2_match_data_create_from_pattern_16(code, generalContext);
                
                var subjectBytes = encoder.GetBytes(subject);
                var subjectLen = subject.Length;

                List<RecordValue> allMatches = new ();

                (int, int) ProcessMatch()
                {
                    Dictionary<string, NamedValue> fields = new ();

                    var sc = NativeMethods.pcre2_get_startchar_16(md);
                    fields.Add(STARTMATCH, new NamedValue(STARTMATCH, NumberValue.New((double)sc + 1)));

                    IntPtr op = NativeMethods.pcre2_get_ovector_pointer_16(md);
                    var start0 = Marshal.ReadInt32(op, 0);
                    var end0 = Marshal.ReadInt32(op, Marshal.SizeOf(typeof(long)));
                    fields.Add(FULLMATCH, new NamedValue(FULLMATCH, StringValue.New(subject.Substring(start0, end0 - start0))));

                    List<FormulaValue> subMatches = new List<FormulaValue>();
                    var oc = NativeMethods.pcre2_get_ovector_count_16(md);
                    for (var i = 1; i < oc; i++)
                    {
                        var start = Marshal.ReadInt32(op, i * 2 * Marshal.SizeOf(typeof(long)));
                        var end = Marshal.ReadInt32(op, ((i * 2) + 1) * Marshal.SizeOf(typeof(long)));
                        if (start >= 0 && end >= 0)
                        {
                            subMatches.Add(StringValue.New(subject.Substring(start, end - start)));
                        }
                        else
                        {
                            subMatches.Add(BlankValue.NewBlank(FormulaType.String));
                        }
                    }

                    if (!fields.ContainsKey(SUBMATCHES) && (options & RegexOptions.ExplicitCapture) == 0)
                    {
                        var recordType = RecordType.Empty().Add(TableValue.ValueName, FormulaType.String);
                        fields.Add(SUBMATCHES, new NamedValue(SUBMATCHES, TableValue.NewTable(recordType, subMatches.Select(s => FormulaValue.NewRecordFromFields(new NamedValue(TableValue.ValueName, s))))));
                    }
                    else
                    {
                        foreach (var name in rex.GetGroupNames())
                        {
                            if (!int.TryParse(name, out _))
                            {
                                var ni = NativeMethods.pcre2_substring_number_from_name_16(code, name);
                                fields.Add(name, new NamedValue(name, subMatches[ni - 1]));
                            }
                        }
                    }

                    allMatches.Add(RecordValue.NewRecordFromFields(fields.Values));

                    return (start0, end0);
                }

                // translated more or less verbatim from https://pcre.org/current/doc/html/pcre2demo.html for proper usage of the PCRE2 API

                var rc = NativeMethods.pcre2_match_16(code, subject, subject.Length, 0, 0, md, matchContext);

                if (rc != (int)PCRE2_RETURNCODES.ERROR_NOMATCH)
                {
                    (var start0, var end0) = ProcessMatch();

                    while (matchAll)
                    {
                        int startMatch = end0;
                        PCRE2_MATCH_OPTIONS matchOptions = 0;

                        if (end0 == start0)
                        {
                            if (end0 >= subjectLen)
                            {
                                break;
                            }

                            matchOptions = PCRE2_MATCH_OPTIONS.NOTEMPTY_ATSTART | PCRE2_MATCH_OPTIONS.ANCHORED;
                        }

                        rc = NativeMethods.pcre2_match_16(code, subject, subject.Length, startMatch, (uint)matchOptions, md, matchContext);

                        if (rc == (int)PCRE2_RETURNCODES.ERROR_NOMATCH)
                        {
                            if (matchOptions == 0)
                            {
                                break;
                            }

                            end0 = startMatch + 1;
                            if (end0 + 1 < subjectLen && subjectBytes[end0] == '\r' && subjectBytes[end0 + 1] == '\n')
                            {
                                end0++;
                            }

                            continue;
                        }

                        (start0, end0) = ProcessMatch();
                    }
                }

                NativeMethods.pcre2_match_data_free_16(md);
                NativeMethods.pcre2_code_free_16(code);

                PCRE2Mutex.ReleaseMutex();

                if (allMatches.Count == 0)
                {
                    return matchAll ? FormulaValue.NewTable(new KnownRecordType(GetRecordTypeFromRegularExpression(rex))) 
                                    : new BlankValue(IRContext.NotInSource(new KnownRecordType(GetRecordTypeFromRegularExpression(rex))));
                }
                else
                {
                    return matchAll ? FormulaValue.NewTable(allMatches.First().Type, allMatches)
                                    : allMatches.First();
                }
            }
        }

        public static void EnableRegExFunctions(PowerFxConfig config, TimeSpan regExTimeout = default, int regexCacheSize = -1)
        {
            RegexTypeCache regexTypeCache = new (regexCacheSize);

            foreach (KeyValuePair<TexlFunction, IAsyncTexlFunction> func in RegexFunctions(regExTimeout, regexTypeCache))
            {
                if (config.SymbolTable.Functions.AnyWithName(func.Key.Name))
                {
                    throw new InvalidOperationException("Cannot add RegEx functions more than once.");
                }

                config.SymbolTable.AddFunction(func.Key);
                config.AdditionalFunctions.Add(func.Key, func.Value);
            }
        }

        internal static Dictionary<TexlFunction, IAsyncTexlFunction> RegexFunctions(TimeSpan regexTimeout, RegexTypeCache regexCache)
        {
            if (regexTimeout == TimeSpan.Zero)
            {
                regexTimeout = new TimeSpan(0, 0, 1);
            }

            if (regexTimeout.TotalMilliseconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(regexTimeout), "Timeout duration for regular expression execution must be positive.");
            }

            return new Dictionary<TexlFunction, IAsyncTexlFunction>()
            {
                { new IsMatchFunction(regexCache), new PCRE2_IsMatchImplementation(regexTimeout) },
                { new MatchFunction(regexCache), new PCRE2_MatchImplementation(regexTimeout) },
                { new MatchAllFunction(regexCache), new PCRE2_MatchAllImplementation(regexTimeout) }
            };
        }

        internal class PCRE2_IsMatchImplementation : PCRE2_RegexCommonImplementation
        {
            private readonly TimeSpan _regexTimeout;

            protected override string DefaultRegexOptions => DefaultIsMatchOptions;

            public PCRE2_IsMatchImplementation(TimeSpan regexTimeout)
            {
                _regexTimeout = regexTimeout;
            }

            internal override FormulaValue InvokeRegexFunction(string input, string regex, string options)
            {
                var match = Match(input, regex, options);

                return new BooleanValue(IRContext.NotInSource(FormulaType.Boolean), !match.IsBlank());
            }
        }

        internal class PCRE2_MatchImplementation : PCRE2_RegexCommonImplementation
        {
            private readonly TimeSpan _regexTimeout;

            protected override string DefaultRegexOptions => DefaultMatchOptions;

            public PCRE2_MatchImplementation(TimeSpan regexTimeout)
            {
                _regexTimeout = regexTimeout;
            }

            internal override FormulaValue InvokeRegexFunction(string input, string regex, string options)
            {
                return Match(input, regex, options);
            }
        }

        internal class PCRE2_MatchAllImplementation : PCRE2_RegexCommonImplementation
        {
            private readonly TimeSpan _regexTimeout;

            protected override string DefaultRegexOptions => DefaultMatchAllOptions;

            public PCRE2_MatchAllImplementation(TimeSpan regexTimeout)
            {
                _regexTimeout = regexTimeout;
            }

            internal override FormulaValue InvokeRegexFunction(string input, string regex, string options)
            {
                return Match(input, regex, options, matchAll: true);
            }
        }
    }
}

#endif
