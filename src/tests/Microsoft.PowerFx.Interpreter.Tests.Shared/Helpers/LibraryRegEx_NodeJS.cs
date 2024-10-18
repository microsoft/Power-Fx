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
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json.Linq;

namespace Microsoft.PowerFx.Functions
{
    public class RegEx_NodeJS
    {
        private static Process node;

        private static readonly StringBuilder OutputSB = new StringBuilder();
        private static readonly StringBuilder ErrorSB = new StringBuilder();
        private static TaskCompletionSource<bool> readTask = null;

        private static void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            OutputSB.Append(outLine.Data);
            if (outLine.Data.Contains("%%end%%"))
            {
                readTask.TrySetResult(true);
            }
        }

        private static void ErrorHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            ErrorSB.Append(outLine.Data);
            readTask.TrySetResult(true);
        }

        private class JSMatch
        {
            public int Index { get; set; }

            public string[] Numbered { get; set; }

            public Dictionary<string, string> Named { get; set; }
        }

        internal abstract class NodeJS_RegexCommonImplementation : Library.RegexCommonImplementation
        {
            internal static FormulaValue Match(string subject, string pattern, string flags, bool matchAll = false)
            {
                Task<FormulaValue> task = Task.Run<FormulaValue>(async () => await MatchAsync(subject, pattern, flags, matchAll));
                return task.Result;
            }

            internal static async Task<FormulaValue> MatchAsync(string subject, string pattern, string flags, bool matchAll = false)
            {
                var js = new StringBuilder();

                js.Append($"MatchTest('{subject.Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n").Replace("'", "\\'")}',");
                js.Append($"'{pattern.Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n").Replace("'", "\\'")}',");
                js.Append($"'{flags}',");
                js.Append($"{(matchAll ? "true" : "false")});");

#if false
                // for debugging unicode passing of strings to Node, output ignored by deserializer but visible in the debugger
                js.AppendLine(@"
                    for (var i = 0; i < subject.length; i++)
                    {
                        console.log(subject[i] + '> ' + subject.charCodeAt(i).toString(16));
                    }
                ");
#endif

                if (node == null)
                {
                    string js2 = @"
                        function MatchTest( subject, pattern, flags, matchAll )
                        {
                            const [alteredPattern, alteredFlags] = AlterRegex_JavaScript( pattern, flags );
                            const regex = RegExp(alteredPattern, alteredFlags.concat(matchAll ? 'g' : ''));
                            const matches = matchAll ? [...subject.matchAll(regex)] : [subject.match(regex)];
                            console.log('%%begin%%');
                            if (matches.length != 0 && matches[0] != null)
                            {
                                var arr = new Array();
                                for (const match of matches)
                                {
                                    var o = new Object();
                                    o.Index = match.index;
                                    o.Named = match.groups;
                                    o.Numbered = match;
                                    arr.push(o);
                                }
                                console.log(JSON.stringify(arr));
                            }
                            console.log('%%end%%');
                        }
                        ";

                    node = new Process();
                    node.StartInfo.FileName = "node.exe";
                    node.StartInfo.Arguments = "-i";
                    node.StartInfo.RedirectStandardInput = true;
                    node.StartInfo.RedirectStandardOutput = true;
                    node.StartInfo.RedirectStandardError = true;
                    node.StartInfo.CreateNoWindow = true;
                    node.StartInfo.UseShellExecute = false;
                    node.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;

                    // Not supported by .NET framework 4.6.2, we need to use the manual GetBytes method below
                    // node.StartInfo.StandardInputEncoding = System.Text.Encoding.UTF8;

                    node.OutputDataReceived += OutputHandler;
                    node.ErrorDataReceived += ErrorHandler;

                    node.Start();

                    node.BeginOutputReadLine();
                    node.BeginErrorReadLine();

                    await node.StandardInput.WriteLineAsync(RegEx_JavaScript.AlterRegex_JavaScript);
                    await node.StandardInput.WriteLineAsync(js2);
                }

                OutputSB.Clear();
                ErrorSB.Clear();
                readTask = new TaskCompletionSource<bool>();

                var jsString = js.ToString();
                var bytes = Encoding.UTF8.GetBytes(jsString);
                await node.StandardInput.BaseStream.WriteAsync(bytes, 0, bytes.Length);
                await node.StandardInput.WriteLineAsync();

                await node.StandardInput.FlushAsync();

                await readTask.Task;

                string output = OutputSB.ToString();
                string error = ErrorSB.ToString();

                if (error.Length > 0)
                {
                    throw new InvalidOperationException(error);
                }

                int begin = output.IndexOf("%%begin%%");
                int end = output.IndexOf("%%end%%");

                var type = new KnownRecordType(GetRecordTypeFromRegularExpression(pattern, flags.Contains('N') ? RegexOptions.None : RegexOptions.ExplicitCapture));

                if (end == begin + 9)
                {
                    return matchAll ? FormulaValue.NewTable(type) : new BlankValue(IRContext.NotInSource(type));
                }

                string json = output.Substring(begin + 9, end - begin - 9);
                var result = JsonSerializer.Deserialize<JSMatch[]>(json);

                List<RecordValue> allMatches = new ();

                foreach (JSMatch match in result)
                {
                    Dictionary<string, NamedValue> fields = new Dictionary<string, NamedValue>()
                    {
                        { STARTMATCH, new NamedValue(STARTMATCH, NumberValue.New(Convert.ToDouble(match.Index) + 1)) },
                        { FULLMATCH, new NamedValue(FULLMATCH, match.Numbered[0] == null ? BlankValue.NewBlank(FormulaType.String) : StringValue.New(match.Numbered[0])) },
                    };

                    if (match.Named != null)
                    {
                        foreach (var name in type.FieldNames)
                        {
                            if (name != STARTMATCH && name != FULLMATCH && name != SUBMATCHES)
                            {
                                fields.Add(name, new NamedValue(name, match.Named.ContainsKey(name) ? StringValue.New(match.Named[name]) : BlankValue.NewBlank(FormulaType.String)));
                            }
                        }
                    }

                    if (flags.Contains('N'))
                    {
                        List<RecordValue> subMatches = new List<RecordValue>();

                        for (int i = 1; i < match.Numbered.Count(); i++)
                        {
                            var n = match.Numbered[i];
                            subMatches.Add(FormulaValue.NewRecordFromFields(new NamedValue(TableValue.ValueName, n == null ? BlankValue.NewBlank(FormulaType.String) : StringValue.New(n))));
                        }

                        var recordType = RecordType.Empty().Add(TableValue.ValueName, FormulaType.String);
                        fields.Add(SUBMATCHES, new NamedValue(SUBMATCHES, TableValue.NewTable(recordType, subMatches)));
                    }

                    allMatches.Add(RecordValue.NewRecordFromFields(fields.Values));
                }

                return matchAll ? FormulaValue.NewTable(allMatches.First().Type, allMatches) : allMatches.First();
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
