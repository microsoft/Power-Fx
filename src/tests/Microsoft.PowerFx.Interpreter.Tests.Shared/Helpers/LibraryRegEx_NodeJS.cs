// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

// This file implements our Regular Expression functions using ECMAScript hosted by Node.js.
// We run tests with this to find semantic differences between our regular expression language and what the JavaScript runtime (Canvas) supports.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    public class RegEx_NodeJS
    {
        internal abstract class NodeJS_RegexCommonImplementation : Library.RegexCommonImplementation
        {
            internal static FormulaValue Match(string subject, string pattern, RegexOptions options, bool matchAll = false)
            {
                StringBuilder flags = new StringBuilder();

                // RegexOptions.ExplicitCapture -> "n" is not done as ECMAScript does not support this flag

                if ((options & RegexOptions.IgnoreCase) != 0)
                {
                    flags.Append("i");
                }

                if ((options & RegexOptions.Multiline) != 0)
                {
                    flags.Append("m");
                }

                if ((options & RegexOptions.Singleline) != 0)
                {
                    flags.Append("s");
                }

                if ((options & RegexOptions.IgnorePatternWhitespace) != 0)
                {
                    flags.Append("x");
                }

                var js = new StringBuilder();

                js.AppendLine($"const subject='{subject.Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n").Replace("'", "\\'")}';");
                js.AppendLine($"const pattern='{pattern.Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n").Replace("'", "\\'")}';");
                js.AppendLine($"const flags='{flags}';");
                js.AppendLine($"const matchAll={(matchAll ? "true" : "false")};");

#if false
                // for debugging unicode passing of strings to Node, output ignored by deserializer but visible in the debugger
                js.AppendLine(@"
                    for (var i = 0; i < subject.length; i++)
                    {
                        console.log(subject[i] + '> ' + subject.charCodeAt(i).toString(16));
                    }
                ");
#endif

                js.AppendLine(@"
                    const [alteredPattern, alteredFlags] = AlterRegex_NodeJS( pattern, flags );
                    const regex = RegExp(alteredPattern, alteredFlags.concat(matchAll ? 'g' : ''));
                    const matches = matchAll ? [...subject.matchAll(regex)] : [subject.match(regex)];
                    for(const match of matches)
                    {
                        if (match != null)
                        {
                            console.log('%start%:' + match.index);
                            for(var group in match.groups)
                            {
                                var val = match.groups[group];
                                if (val === undefined)
                                {
                                    val = '';
                                }
                                console.log(group + ':""' + val.replace( / "" /, '""""') + '""');
                            }
                            for (var num = 0; num < match.length; num++)
                            {
                                var val = match[num];
                                if (val === undefined)
                                {
                                    val = '';
                                }
                                console.log(num + ':""' + val.replace( / "" /, '""""') + '""');
                            }
                            console.log('%end%:');
                        }
                    }
                    console.log('%done%:');
                ");

                var node = new Process();
                node.StartInfo.FileName = "node.exe";
                node.StartInfo.RedirectStandardInput = true;
                node.StartInfo.RedirectStandardOutput = true;
                node.StartInfo.RedirectStandardError = true;
                node.StartInfo.CreateNoWindow = true;
                node.StartInfo.UseShellExecute = false;
                node.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;

                // Not supported by .NET framework 4.6.2, we need to use the manual GetBytes method below
                // node.StartInfo.StandardInputEncoding = System.Text.Encoding.UTF8;

                node.Start();

                var jsString = js.ToString();
                var bytes = Encoding.UTF8.GetBytes(AlterRegex_NodeJS_Src + jsString);
                node.StandardInput.BaseStream.Write(bytes, 0, bytes.Length);
                node.StandardInput.WriteLine();
                node.StandardInput.Close();

                string output = node.StandardOutput.ReadToEnd();
                string error = node.StandardError.ReadToEnd();

                node.WaitForExit();
                node.Dispose();

                if (error.Length > 0)
                {
                    throw new InvalidOperationException(error);
                }

                var outputRE = new Regex(
                    @"
                    ^%start%:(?<start>[-\d]+)$ |
                    ^(?<end>%end%): |
                    ^(?<done>%done%): |
                    ^(?<num>\d+):""(?<numMatch>(""""|[^""])*)""$ |
                    ^(?<name>[^:]+):""(?<nameMatch>(""""|[^""])*)""$
                    ", RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

                List<string> subMatches = new ();
                Dictionary<string, NamedValue> fields = new ();
                List<RecordValue> allMatches = new ();

                foreach (Match token in outputRE.Matches(output))
                {
                    if (token.Groups["end"].Success)
                    {
                        if ((options & RegexOptions.ExplicitCapture) == 0)
                        {
                            fields.Add(SUBMATCHES, new NamedValue(SUBMATCHES, TableValue.NewSingleColumnTable(subMatches.Select(s => StringValue.New(s)).ToArray())));
                        }

                        allMatches.Add(RecordValue.NewRecordFromFields(fields.Values));
                    }
                    if (token.Groups["done"].Success)
                    {
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
                    else if (token.Groups["start"].Success)
                    {
                        fields = new ();
                        subMatches = new ();
                        fields.Add(STARTMATCH, new NamedValue(STARTMATCH, NumberValue.New(Convert.ToDouble(token.Groups["start"].Value) + 1)));
                    }
                    else if (token.Groups["name"].Success)
                    {
                        fields.Add(token.Groups["name"].Value, new NamedValue(token.Groups["name"].Value, StringValue.New(token.Groups["nameMatch"].Value.Replace(@"""""", @""""))));
                    }
                    else if (token.Groups["num"].Success)
                    {
                        var num = Convert.ToInt32(token.Groups["num"].Value);
                        if (num == 0)
                        {
                            fields.Add(FULLMATCH, new NamedValue(FULLMATCH, StringValue.New(token.Groups["numMatch"].Value.Replace(@"""""", @""""))));
                        }
                        else
                        {
                            subMatches.Add(token.Groups["numMatch"].Value.Replace(@"""""", @""""));
                        }
                    }
                }

                throw new Exception("%done% marker not found");
            }

            // This JavaScript function assumes that the regular expression has already been compiled and comforms to the Power Fx regular expression language.
            // For example, no affodance is made for nested character classes or inline options on a subexpression, as those would have already been blocked.
            private const string AlterRegex_NodeJS_Src = @"
                function AlterRegex_NodeJS(regex, flags)
                {
                    var regexIndex = 0;

                    const inlineFlagsRE = /^\(\?(?<flags>[imsx]+)\)/;
                    const inlineFlags = inlineFlagsRE.exec( regex );
                    if (inlineFlags != null)
                    {
                        flags = flags.concat(inlineFlags.groups['flags']);
                        regexIndex = inlineFlags[0].length;
                    }

                    const freeSpacing = flags.includes('x');
                    const multiline = flags.includes('m');
                    const dotAll = flags.includes('s');
                    const ignoreCase = flags.includes('i');

                    // rebuilding from booleans avoids possible duplicate letters
                    // x has been handled in this function and does not need to be passed on (and would cause an error)
                    const alteredFlags = 'u'.concat((ignoreCase ? 'i' : ''), (multiline ? 'm' : ''), (dotAll ? 's' : ''));  

                    var openCharacterClass = false;       // are we defining a character class?
                    var altered = '';

                    for ( ; regexIndex < regex.length; regexIndex++)
                    {
                        switch (regex.charAt(regexIndex) )
                        {
                            case '[':
                                openCharacterClass = true;
                                altered = altered.concat('[');
                                break;

                            case ']':
                                openCharacterClass = false;
                                altered = altered.concat(']');
                                break;

                            case '\\':
                                if (++regexIndex < regex.length)
                                {
                                    const letterChar = '\\p{Ll}\\p{Lu}\\p{Lt}\\p{Lo}\\p{Lm}\\p{Nd}\\p{Pc}';
                                    const wordChar = '(?:(?:\\p{L}\\p{M}*)|\\p{N}|_)';
                                    const spaceChar = '\\f\\n\\r\\t\\v\\x85\\p{Z}';
                                    const digitChar = '\\p{Nd}';

                                    switch (regex.charAt(regexIndex))
                                    { 
                                        case 'w':
                                            altered = altered.concat((openCharacterClass ? '' : '['), letterChar, (openCharacterClass ? '' : ']'));
                                            break;
                                        case 'W':
                                            altered = altered.concat('[^', letterChar, ']');
                                            break;

                                        case 'b':
                                            altered = altered.concat(`(?:(?<=${wordChar})(?!${wordChar})|(?<!${wordChar})(?=${wordChar}))`);
                                            break;
                                        case 'B':
                                            altered = altered.concat(`(?:(?<=${wordChar})(?=${wordChar})|(?<!${wordChar})(?!${wordChar}))`);
                                            break;

                                        case 's':
                                            altered = altered.concat((openCharacterClass ? '' : '['), spaceChar, (openCharacterClass ? '' : ']'));
                                            break;
                                        case 'S':
                                            altered = altered.concat('[^', spaceChar, ']');
                                            break;

                                        case 'd':
                                            altered = altered.concat(digitChar);
                                            break;
                                        case 'D':
                                            altered = altered.concat('[^', digitChar, ']');
                                            break;

                                        default:
                                            altered = altered.concat('\\', regex.charAt(regexIndex));
                                            break;
                                    }
                                }
                                else
                                {
                                    // backslash at end of regex
                                    altered = altered.concat( '\\' );
                                }

                                break;

                            case '.':
                                altered = altered.concat(!openCharacterClass && !dotAll ? '[^\\r\\n]' : '.');
                                break;

                            case '^':
                                altered = altered.concat(!openCharacterClass && multiline ? '(?<=^|\\r\\n|\\r|\\n)' : '^');
                                break;

                            case '$':
                                altered = altered.concat(openCharacterClass ? '$' : (multiline ? '(?=$|\\r\\n|\\r|\\n)' : '(?=$|\\r\\n$|\\r$|\\n$)'));
                                break;

                            case '(':
                                if (regex.length - regexIndex > 2 && regex.charAt(regexIndex+1) == '?' && regex.charAt(regexIndex+2) == '#')
                                {
                                    // inline comment
                                    for ( regexIndex++; regexIndex < regex.length && regex.charAt(regexIndex) != ')'; regexIndex++)
                                    {
                                        // eat characters until a close paren, it doesn't matter if it is escaped (consistent with .NET)
                                    }
                                }
                                else
                                {
                                    altered = altered.concat('('); 
                                }

                                break;

                            case ' ':
                            case '\f':
                            case '\n':
                            case '\r':
                            case '\t':
                            case '\v':
                                if (!freeSpacing)
                                {
                                    altered = altered.concat(regex.charAt(regexIndex));
                                }

                                break;

                            case '#':
                                if (freeSpacing)
                                {
                                    for ( regexIndex++; regexIndex < regex.length && regex.charAt(regexIndex) != '\r' && regex.charAt(regexIndex) != '\n'; regexIndex++)
                                    {
                                        // eat characters until the end of the line
                                        // leaving dangling whitespace characters will be eaten on next iteration
                                    }
                                }
                                else
                                {
                                    altered = altered.concat('#');
                                }

                                break;

                            default:
                                altered = altered.concat(regex.charAt(regexIndex));
                                break;
                        }
                    }

                    return [altered, alteredFlags];
                }
            ";
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
                { new IsMatchFunction(), new NodeJS_IsMatchImplementation(regexTimeout) },
                { new MatchFunction(regexCache), new NodeJS_MatchImplementation(regexTimeout) },
                { new MatchAllFunction(regexCache), new NodeJS_MatchAllImplementation(regexTimeout) }
            };
        }

        internal class NodeJS_IsMatchImplementation : NodeJS_RegexCommonImplementation
        {
            private readonly TimeSpan _regexTimeout;

            protected override string DefaultRegexOptions => DefaultIsMatchOptions;

            public NodeJS_IsMatchImplementation(TimeSpan regexTimeout)
            {
                _regexTimeout = regexTimeout;
            }

            internal override FormulaValue InvokeRegexFunction(string input, string regex, RegexOptions options)
            {
                var match = Match(input, regex, options);

                return new BooleanValue(IRContext.NotInSource(FormulaType.Boolean), !match.IsBlank());
            }
        }

        internal class NodeJS_MatchImplementation : NodeJS_RegexCommonImplementation
        {
            private readonly TimeSpan _regexTimeout;

            protected override string DefaultRegexOptions => DefaultMatchOptions;

            public NodeJS_MatchImplementation(TimeSpan regexTimeout)
            {
                _regexTimeout = regexTimeout;
            }

            internal override FormulaValue InvokeRegexFunction(string input, string regex, RegexOptions options)
            {
                return Match(input, regex, options);
            }
        }

        internal class NodeJS_MatchAllImplementation : NodeJS_RegexCommonImplementation
        {
            private readonly TimeSpan _regexTimeout;

            protected override string DefaultRegexOptions => DefaultMatchAllOptions;

            public NodeJS_MatchAllImplementation(TimeSpan regexTimeout)
            {
                _regexTimeout = regexTimeout;
            }

            internal override FormulaValue InvokeRegexFunction(string input, string regex, RegexOptions options)
            {
                return Match(input, regex, options, matchAll: true);
            }
        }
    }
}
