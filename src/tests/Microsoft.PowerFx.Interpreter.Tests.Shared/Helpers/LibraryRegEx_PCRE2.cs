// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

// This file implements our Regular Expression functions using PCRE2 instead of .NET.
// We run tests with this to find semantic differences between our regular expression language and what Excel supports.
// To run this code, make sure that pcre2-32d.dll in your path, built from https://github.com/PCRE2Project/pcre2
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
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
                // use 32 bit version as PCRE2 as it doesn't support surrogate pairs, we manually convert in/out of surrogate pairs to UTF-32.

                [DllImport("pcre2-32.dll", CharSet = CharSet.Unicode)]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern IntPtr pcre2_compile_32(byte[] pattern, int patternLength, uint patternOptions, ref int errorNumber, ref int errorOffset, IntPtr context);

                [DllImport("pcre2-32.dll", CharSet = CharSet.Unicode)]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern int pcre2_match_32(IntPtr code, byte[] subject, int subjectLength, int subjectOffset, uint subjectOptions, IntPtr matchData, IntPtr matchContext);

                [DllImport("pcre2-32.dll", CharSet = CharSet.Unicode)]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern int pcre2_exec_32(IntPtr code, IntPtr extra, byte[] subject, int subjectLength, int subjectOffset, uint subjectOptions, IntPtr ovector, IntPtr ovectorSize);

                [DllImport("pcre2-32.dll")]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern IntPtr pcre2_match_data_create_32(int ovecSize, IntPtr generalContext);

                [DllImport("pcre2-32.dll")]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern IntPtr pcre2_match_data_create_from_pattern_32(IntPtr code, IntPtr generalContext);

                [DllImport("pcre2-32.dll")]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern int pcre2_get_startchar_32(IntPtr matchData);

                [DllImport("pcre2-32.dll")]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern int pcre2_get_ovector_count_32(IntPtr matchData);

                [DllImport("pcre2-32.dll")]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern IntPtr pcre2_get_ovector_pointer_32(IntPtr matchData);

                [DllImport("pcre2-32.dll", CharSet = CharSet.Unicode)]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern int pcre2_substring_number_from_name_32(IntPtr code, byte[] name);

                [DllImport("pcre2-32.dll", CharSet = CharSet.Unicode)]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern void pcre2_match_data_free_32(IntPtr data);

                [DllImport("pcre2-32.dll", CharSet = CharSet.Unicode)]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern void pcre2_code_free_32(IntPtr code);

                [DllImport("pcre2-32.dll", CharSet = CharSet.Unicode)]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern int pcre2_get_error_message_32(int code, IntPtr buffer, int bufferLength);

                [DllImport("pcre2-32.dll", CharSet = CharSet.Unicode)]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern int pcre2_set_compile_extra_options_32(IntPtr context, uint extraOptions);

                [DllImport("pcre2-32.dll", CharSet = CharSet.Unicode)]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern IntPtr pcre2_compile_context_create_32(IntPtr generalContext);
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
            }

            internal enum PCRE2_EXTRA_OPTIONS : uint
            {
                ALLOW_SURROGATE_ESCAPES = 0x00000001,
            }

            internal enum PCRE2_MATCH_OPTIONS : uint
            {
                NOTEMPTY = 0x00000004,
                NOTEMPTY_ATSTART = 0x00000008,
            }

            private static readonly Mutex PCRE2Mutex = new Mutex();  // protect concurrent access to the node process

            private static string Extract(byte[] bytes, int start, int end)
            {
                StringBuilder result = new StringBuilder();

                for (int i = start; i < end; i++)
                {
                    int number = BitConverter.ToInt32(bytes, i * 4);
                    string s = char.ConvertFromUtf32(number);
                    result.Append(s);
                }

                result.Replace('\uf8ff', '\u180e');

                return result.ToString();
            }

            internal static FormulaValue Match(string subject, string pattern, string flags, bool matchAll = false)
            {
                int errorNumber = 0;
                int errorOffset = 0;
                IntPtr matchContext = (IntPtr)0;
                IntPtr generalContext = (IntPtr)0;

                PCRE2_OPTIONS pcreOptions = PCRE2_OPTIONS.UTF | PCRE2_OPTIONS.UCP;
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
                    // replace the three characters that PCRE2 recognizes as white space that Power Fx does not
                    // add a \n so that we can add a $ at the end, in case there is an unterminated pound comment
                    pattern = pattern.Replace("\u000b", "\\u000b").Replace("\u2028", "\\u2028").Replace("\u2029", "\\u2029") + '\n';
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

                // convert out of surrogate pairs and into UTF-32 for the pattern manually
                // convesion of the subject is handled with Encoding.UTF32.GetBytes below

                StringBuilder patternSurrogates = new StringBuilder();

                for (int i = 0; i < pattern.Length; i++)
                {
                    if (i + 11 < pattern.Length && pattern[i] == '\\' && pattern[i + 1] == 'u' && pattern[i + 6] == '\\' && pattern[i + 7] == 'u')
                    {
                        var s1 = Convert.ToInt32(Convert.ToInt32(pattern.Substring(i + 2, 4), 16));
                        var s2 = Convert.ToInt32(Convert.ToInt32(pattern.Substring(i + 8, 4), 16));
                        if (s1 >= 0xd800 && s1 <= 0xdbff && s2 >= 0xdc00 && s2 <= 0xdfff)
                        {
                            patternSurrogates.Append("\\x{" + Convert.ToString(((s1 - 0xd800) * 0x400) + (s2 - 0xdc00) + 0x10000, 16) + "}");
                            i += 11;
                        }
                    }
                    else if (i + 5 < pattern.Length && pattern[i] == '\\' && pattern[i + 1] == 'u')
                    {
                        patternSurrogates.Append("\\x{" + pattern[i + 2] + pattern[i + 3] + pattern[i + 4] + pattern[i + 5] + "}");
                        i += 5;
                    }
                    else
                    { 
                        patternSurrogates.Append(pattern[i]);
                    }
                }

                var patternPCRE2 = patternSurrogates.ToString();

                PCRE2Mutex.WaitOne();

                var context = NativeMethods.pcre2_compile_context_create_32(generalContext);

#if false
                // not needed as we convert out of surrogate pairs above
                NativeMethods.pcre2_set_compile_extra_options_32(context, (uint)PCRE2_EXTRA_OPTIONS.ALLOW_SURROGATE_ESCAPES);
#endif

                var code = NativeMethods.pcre2_compile_32(Encoding.UTF32.GetBytes(patternPCRE2), -1, (uint)pcreOptions, ref errorNumber, ref errorOffset, context);
                if (code == IntPtr.Zero)
                {
                    byte[] buffer = new byte[4096];

                    GCHandle pinnedArray = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    IntPtr pointer = pinnedArray.AddrOfPinnedObject();

                    NativeMethods.pcre2_get_error_message_32(errorNumber, pointer, buffer.Length);
                    var message = System.Text.Encoding.Unicode.GetString(buffer);
                    var fullMessage = $"PCRE2 error compiling {patternPCRE2}, errorNumber={errorNumber} ({message}), errorOffset={errorOffset}";

                    pinnedArray.Free();

                    PCRE2Mutex.ReleaseMutex();
                    throw new Exception(fullMessage);
                }

                var md = NativeMethods.pcre2_match_data_create_from_pattern_32(code, generalContext);

                var startMatch = 0;
                List<RecordValue> allMatches = new ();

                // PCRE2 uses an older definition of Unicode where 180e is a space character, moving it to something else (used defined cahracter) here for category comparisons tests
                subject = subject.Replace('\u180e', '\uf8ff');

                var subjectBytes = Encoding.UTF32.GetBytes(subject);
                PCRE2_MATCH_OPTIONS matchOptions = 0;
                while (startMatch >= 0 && NativeMethods.pcre2_match_32(code, subjectBytes, -1, startMatch, (uint)matchOptions, md, matchContext) > 0)
                {
                    Dictionary<string, NamedValue> fields = new ();

                    var sc = NativeMethods.pcre2_get_startchar_32(md);
                    fields.Add(STARTMATCH, new NamedValue(STARTMATCH, NumberValue.New((double)sc + 1)));

                    IntPtr op = NativeMethods.pcre2_get_ovector_pointer_32(md);
                    var start0 = Marshal.ReadInt32(op, 0);
                    var end0 = Marshal.ReadInt32(op, Marshal.SizeOf(typeof(long)));
                    fields.Add(FULLMATCH, new NamedValue(FULLMATCH, StringValue.New(Extract(subjectBytes, start0, end0))));

                    // for next iteration
                    if (matchAll)
                    {
                        startMatch = end0;
                        if (end0 == start0)
                        {
#if false
                            startMatch++;
#else
                            if (matchOptions == 0)
                            {
                                matchOptions = PCRE2_MATCH_OPTIONS.NOTEMPTY_ATSTART;
                            }
                            else
                            {
                                throw new Exception("PCRE2 repeated empty result");
                            }
#endif
                        }
                        else
                        {
                            matchOptions = 0;
                        }
                    }
                    else
                    {
                        startMatch = -1;
                    }

                    List<FormulaValue> subMatches = new List<FormulaValue>();
                    var oc = NativeMethods.pcre2_get_ovector_count_32(md);
                    for (var i = 1; i < oc; i++)
                    {
                        var start = Marshal.ReadInt32(op, i * 2 * Marshal.SizeOf(typeof(long)));
                        var end = Marshal.ReadInt32(op, ((i * 2) + 1) * Marshal.SizeOf(typeof(long)));
                        if (start >= 0 && end >= 0)
                        {
                            subMatches.Add(StringValue.New(Extract(subjectBytes, start, end)));
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
                        // In x mode, comment line endings are [\r\n], but .NET only supports \n.  For our purposes here, we can just replace the \r.
                        pattern = pattern.Replace('\r', '\n');
                        var regex = new Regex(pattern, options);
                        foreach (var name in regex.GetGroupNames())
                        {
                            if (!int.TryParse(name, out _))
                            {
                                var ni = NativeMethods.pcre2_substring_number_from_name_32(code, Encoding.UTF32.GetBytes(name));
                                fields.Add(name, new NamedValue(name, subMatches[ni - 1]));
                            }
                        }
                    }

                    allMatches.Add(RecordValue.NewRecordFromFields(fields.Values));
                }

                NativeMethods.pcre2_match_data_free_32(md);
                NativeMethods.pcre2_code_free_32(code);

                PCRE2Mutex.ReleaseMutex();

                if (allMatches.Count == 0)
                {
                    return matchAll ? FormulaValue.NewTable(new KnownRecordType(GetRecordTypeFromRegularExpression(pattern, options))) 
                                    : new BlankValue(IRContext.NotInSource(new KnownRecordType(GetRecordTypeFromRegularExpression(pattern, options))));
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
                { new IsMatchFunction(), new PCRE2_IsMatchImplementation(regexTimeout) },
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
