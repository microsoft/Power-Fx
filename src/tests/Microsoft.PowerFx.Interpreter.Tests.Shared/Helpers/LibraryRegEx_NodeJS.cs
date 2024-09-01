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
            internal static FormulaValue Match(string subject, string pattern, string flags, bool matchAll = false)
            {
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
                    const [alteredPattern, alteredFlags] = AlterRegex_JavaScript( pattern, flags );
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
                                if (val !== undefined)
                                {
                                    val = '""' + val.replace( / "" /, '""""') + '""'
                                }
                                console.log(group + ':' + val);
                            }
                            for (var num = 0; num < match.length; num++)
                            {
                                var val = match[num];
                                if (val !== undefined)
                                {
                                    val = '""' + val.replace( / "" /, '""""') + '""'
                                }
                                console.log(num + ':' + val);
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
                var bytes = Encoding.UTF8.GetBytes(RegEx_JavaScript.AlterRegex_JavaScript + jsString);
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
                    ^(?<name>[^:]+):""(?<nameMatch>(""""|[^""])*)""$ |
                    ^(?<numBlank>\d+):undefined$ |
                    ^(?<nameBlank>[^:]+):undefined$
                    ", RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

                List<RecordValue> subMatches = new List<RecordValue>();
                Dictionary<string, NamedValue> fields = new ();
                List<RecordValue> allMatches = new ();

                foreach (Match token in outputRE.Matches(output))
                {
                    if (token.Groups["start"].Success)
                    {
                        fields = new ();
                        subMatches = new ();
                        fields.Add(STARTMATCH, new NamedValue(STARTMATCH, NumberValue.New(Convert.ToDouble(token.Groups["start"].Value) + 1)));
                    }
                    else if (token.Groups["end"].Success)
                    {
                        if (flags.Contains('N'))
                        {
                            var recordType = RecordType.Empty().Add(TableValue.ValueName, FormulaType.String);
                            fields.Add(SUBMATCHES, new NamedValue(SUBMATCHES, TableValue.NewTable(recordType, subMatches)));
                        }

                        allMatches.Add(RecordValue.NewRecordFromFields(fields.Values));
                    }
                    else if (token.Groups["done"].Success)
                    {
                        if (allMatches.Count == 0)
                        {
                            return matchAll ? FormulaValue.NewTable(new KnownRecordType(GetRecordTypeFromRegularExpression(pattern, flags.Contains('N') ? RegexOptions.None : RegexOptions.ExplicitCapture)))
                                            : new BlankValue(IRContext.NotInSource(new KnownRecordType(GetRecordTypeFromRegularExpression(pattern, flags.Contains('N') ? RegexOptions.None : RegexOptions.ExplicitCapture))));
                        }
                        else
                        {
                            return matchAll ? FormulaValue.NewTable(allMatches.First().Type, allMatches)
                                            : allMatches.First();
                        }
                    }
                    else if (token.Groups["name"].Success)
                    {
                        fields.Add(token.Groups["name"].Value, new NamedValue(token.Groups["name"].Value, StringValue.New(token.Groups["nameMatch"].Value.Replace(@"""""", @""""))));
                    }
                    else if (token.Groups["nameBlank"].Success)
                    {
                        fields.Add(token.Groups["nameBlank"].Value, new NamedValue(token.Groups["nameBlank"].Value, BlankValue.NewBlank(FormulaType.String)));
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
                            subMatches.Add(FormulaValue.NewRecordFromFields(new NamedValue(TableValue.ValueName, StringValue.New(token.Groups["numMatch"].Value.Replace(@"""""", @"""")))));
                        }
                    }
                    else if (token.Groups["numBlank"].Success)
                    {
                        var num = Convert.ToInt32(token.Groups["numBlank"].Value);
                        if (num == 0)
                        {
                            fields.Add(FULLMATCH, new NamedValue(FULLMATCH, BlankValue.NewBlank(FormulaType.String)));
                        }
                        else
                        {
                            subMatches.Add(FormulaValue.NewRecordFromFields(new NamedValue(TableValue.ValueName, BlankValue.NewBlank(FormulaType.String))));
                        }
                    }
                }

                throw new Exception("%done% marker not found");
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

            internal override FormulaValue InvokeRegexFunction(string input, string regex, string options)
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

            internal override FormulaValue InvokeRegexFunction(string input, string regex, string options)
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

            internal override FormulaValue InvokeRegexFunction(string input, string regex, string options)
            {
                return Match(input, regex, options, matchAll: true);
            }
        }
    }
}
