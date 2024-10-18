// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

// This file implements our Regular Expression functions using PCRE2 instead of .NET.
// We run tests with this to find semantic differences between our regular expression language and what Excel supports.
// To run this code, make sure that pcre2-16d.dll in your path, built from https://github.com/PCRE2Project/pcre2
// with cmake , shared library, 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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
                [DllImport("pcre2-16d.dll", CharSet = CharSet.Unicode)]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern IntPtr pcre2_compile_16(string pattern, int patternLength, uint patternOptions, ref int errorNumber, ref int errorOffset, IntPtr context);

                [DllImport("pcre2-16d.dll", CharSet = CharSet.Unicode)]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern int pcre2_match_16(IntPtr code, string subject, int subjectLength, int subjectOffset, uint subjectOptions, IntPtr matchData, IntPtr matchContext);

                [DllImport("pcre2-16d.dll")]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern IntPtr pcre2_match_data_create_16(int ovecSize, IntPtr generalContext);

                [DllImport("pcre2-16d.dll")]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern IntPtr pcre2_match_data_create_from_pattern_16(IntPtr code, IntPtr generalContext);

                [DllImport("pcre2-16d.dll")]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern int pcre2_get_startchar_16(IntPtr matchData);

                [DllImport("pcre2-16d.dll")]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern int pcre2_get_ovector_count_16(IntPtr matchData);

                [DllImport("pcre2-16d.dll")]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern IntPtr pcre2_get_ovector_pointer_16(IntPtr matchData);

                [DllImport("pcre2-16d.dll", CharSet = CharSet.Unicode)]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern int pcre2_substring_number_from_name_16(IntPtr code, string name);

                [DllImport("pcre2-16d.dll", CharSet = CharSet.Unicode)]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern void pcre2_match_data_free_16(IntPtr data);

                [DllImport("pcre2-16d.dll", CharSet = CharSet.Unicode)]
                [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
                internal static extern void pcre2_code_free_16(IntPtr code);
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

            internal static FormulaValue Match(string subject, string pattern, string flags, bool matchAll = false)
            {
                int errorNumber = 0;
                int errorOffset = 0;
                IntPtr context = (IntPtr)0;
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

                var patternPCRE2 = Regex.Replace(pattern, @"\\u(?<hex>\w\w\w\w)", @"\x{${hex}}");
                var code = NativeMethods.pcre2_compile_16(patternPCRE2, -1, (uint)pcreOptions, ref errorNumber, ref errorOffset, context);
                var md = NativeMethods.pcre2_match_data_create_from_pattern_16(code, generalContext);

                var startMatch = 0;
                List<RecordValue> allMatches = new ();

                while (startMatch >= 0 && NativeMethods.pcre2_match_16(code, subject, -1, startMatch, 0, md, matchContext) > 0)
                {
                    Dictionary<string, NamedValue> fields = new ();

                    var sc = NativeMethods.pcre2_get_startchar_16(md);
                    fields.Add(STARTMATCH, new NamedValue(STARTMATCH, NumberValue.New((double)sc + 1)));

                    IntPtr op = NativeMethods.pcre2_get_ovector_pointer_16(md);
                    var start0 = Marshal.ReadInt32(op, 0);
                    var end0 = Marshal.ReadInt32(op, Marshal.SizeOf(typeof(long)));
                    fields.Add(FULLMATCH, new NamedValue(FULLMATCH, StringValue.New(subject.Substring(start0, end0 - start0))));
                    startMatch = matchAll ? end0 : -1;  // for next iteration

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
                        // In x mode, comment line endings are [\r\n], but .NET only supports \n.  For our purposes here, we can just replace the \r.
                        pattern = pattern.Replace('\r', '\n');
                        var regex = new Regex(pattern, options);
                        foreach (var name in regex.GetGroupNames())
                        {
                            if (!int.TryParse(name, out _))
                            {
                                var ni = NativeMethods.pcre2_substring_number_from_name_16(code, name);
                                fields.Add(name, new NamedValue(name, subMatches[ni - 1]));
                            }
                        }
                    }

                    allMatches.Add(RecordValue.NewRecordFromFields(fields.Values));
                }

                NativeMethods.pcre2_match_data_free_16(md);
                NativeMethods.pcre2_code_free_16(code);

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
