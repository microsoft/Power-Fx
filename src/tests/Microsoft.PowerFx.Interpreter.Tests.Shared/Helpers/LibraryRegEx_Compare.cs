// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

#if MATCHCOMPARE

// This file compares the results from .NET, PCRE2, and NODEJS.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Functions.RegEx_NodeJS;
using static Microsoft.PowerFx.Functions.RegEx_PCRE2;

namespace Microsoft.PowerFx.Functions
{
    public class RegEx_Compare
    {
        public static void EnableRegExFunctions(PowerFxConfig config, TimeSpan regExTimeout = default, int regexCacheSize = -1, bool includeDotNet = true, bool includeNode = true, bool includePCRE2 = true)
        {
            RegexTypeCache regexTypeCache = new (regexCacheSize);

            foreach (KeyValuePair<TexlFunction, IAsyncTexlFunction> func in RegexFunctions(regExTimeout, regexTypeCache, includeDotNet, includeNode, includePCRE2))
            {
                if (config.ComposedConfigSymbols.Functions.AnyWithName(func.Key.Name))
                {
                    throw new InvalidOperationException("Cannot add RegEx functions more than once.");
                }

                config.InternalConfigSymbols.AddFunction(func.Key);
                config.AdditionalFunctions.Add(func.Key, func.Value);
            }
        }

        internal static Dictionary<TexlFunction, IAsyncTexlFunction> RegexFunctions(TimeSpan regexTimeout, RegexTypeCache regexCache, bool includeDotNet, bool includeNode, bool includePCRE2)
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
                { new IsMatchFunction(regexCache), new Compare_IsMatchImplementation(regexTimeout, includeDotNet, includeNode, includePCRE2) },
                { new MatchFunction(regexCache), new Compare_MatchImplementation(regexTimeout, includeDotNet, includeNode, includePCRE2) },
                { new MatchAllFunction(regexCache), new Compare_MatchAllImplementation(regexTimeout, includeDotNet, includeNode, includePCRE2) }
            };
        }

        internal abstract class Compare_CommonImplementation : Library.RegexCommonImplementation
        {
            protected Library.RegexCommonImplementation dotnet;
            protected Library.RegexCommonImplementation node;
            protected Library.RegexCommonImplementation pcre2;

            protected Library.RegexCommonImplementation dotnet_alt;
            protected Library.RegexCommonImplementation node_alt;
            protected Library.RegexCommonImplementation pcre2_alt;

            private string CharCodes(string str)
            {
                StringBuilder outStr = new StringBuilder();
                int highSurrogate = 0;

                foreach (var c in str)
                {
                    if (highSurrogate > 0)
                    {
                        if (c >= 0xdc00 && c <= 0xdfff)
                        {
                            int codepoint = ((highSurrogate - 0xd800) << 10) + ((int)c) - 0xdc00 + 0x10000;
                            outStr.Append("\\u{" + codepoint.ToString("X6") + "}");
                        }
                        else
                        {
                            outStr.Append("\\u" + highSurrogate.ToString("X4"));
                            outStr.Append("\\u" + ((int)c).ToString("X4"));
                        }

                        highSurrogate = 0;
                    }
                    else
                    {
                        switch (c)
                        {
                            case '\"':
                                outStr.Append("\\\"");
                                break;
                            case '\r':
                                outStr.Append("\\r");
                                break;
                            case '\n':
                                outStr.Append("\\n");
                                break;
                            case '\t':
                                outStr.Append("\\t");
                                break;
                            case '\b':
                                outStr.Append("\\b");
                                break;
                            case '\f':
                                outStr.Append("\\f");
                                break;
                            case '\\':
                                outStr.Append("\\\\");
                                break;
                            default:
                                if (c >= 0xd800 && c <= 0xdbff)
                                {
                                    highSurrogate = c;
                                }
                                else if (c < 0x20 || c >= 0x7f)
                                {
                                    outStr.Append("\\u" + ((int)c).ToString("X4"));
                                }
                                else
                                {
                                    outStr.Append(c);
                                }

                                break;
                        }
                    }
                }

                return outStr.ToString();
            }

            private FormulaValue InvokeRegexFunctionOne(string input, string regex, string options, Library.RegexCommonImplementation dotnet, Library.RegexCommonImplementation node, Library.RegexCommonImplementation pcre2, string kind)
            {
                FormulaValue dotnetMatch = null;
                FormulaValue pcre2Match = null;
                FormulaValue nodeMatch = null;

                string nodeExpr = null;
                string pcre2Expr = null;
                string dotnetExpr = null;

                if (dotnet != null)
                {
                    dotnetMatch = dotnet.InvokeRegexFunction(input, regex, options);
                    dotnetExpr = dotnetMatch.ToExpression();
                }

                if (node != null)
                {
                    nodeMatch = node.InvokeRegexFunction(input, regex, options);
                    nodeExpr = nodeMatch.ToExpression();
                }

                if (pcre2 != null)
                {
                    int retry = 3;

                    do
                    {
                        pcre2Match = pcre2.InvokeRegexFunction(input, regex, options);
                        pcre2Expr = pcre2Match.ToExpression();
                    }
                    while (--retry > 0 && (pcre2Expr != dotnetExpr || (node != null && pcre2Expr != nodeExpr)));
                }

                string prefix = null;

                if (nodeExpr != null && dotnetExpr != null && nodeExpr != dotnetExpr)
                {
                    prefix = $"{kind}: node != net";        
                }

                if (pcre2Expr != null && dotnetExpr != null && pcre2Expr != dotnetExpr)
                {
                    prefix = $"{kind}: pcre2 != net";
                }

                if (pcre2Expr != null && nodeExpr != null && pcre2Expr != nodeExpr)
                {
                    prefix = $"{kind}: pcre2 != node";
                }

                if (prefix != null)
                {
                    var report =
                        $"  re='{regex}' options='{options}'\n" +
                        $"  input='{CharCodes(input)}'\n" +
                        $"  net={dotnetExpr}\n" +
                        (nodeExpr != null ? $"  node={nodeExpr}\n" : string.Empty) +
                        (pcre2Expr != null ? $"  pcre2={pcre2Expr}\n" : string.Empty);

                    throw new Exception($"{prefix}\n{report}");
                }

                return dotnetMatch ?? nodeMatch ?? pcre2Match;
            }

            internal override FormulaValue InvokeRegexFunction(string input, string regex, string options)
            {
                var result = InvokeRegexFunctionOne(input, regex, options, dotnet, node, pcre2, "main");

                if (dotnet_alt != null)
                {
                    InvokeRegexFunctionOne(input, regex, options, dotnet_alt, node_alt, pcre2_alt, "alt");
                }

                return result;
            }
        }

        internal class Compare_IsMatchImplementation : Compare_CommonImplementation
        {
            protected override string DefaultRegexOptions => DefaultIsMatchOptions;

            internal Compare_IsMatchImplementation(TimeSpan regexTimeout, bool includeDotNet, bool includeNode, bool includePCRE2) 
            {
                if (includeDotNet)
                {
                    dotnet = new Library.IsMatchImplementation(regexTimeout);
                    dotnet_alt = new Library.MatchImplementation(regexTimeout);
                }

                if (includeNode)
                {
                    node = new NodeJS_IsMatchImplementation(regexTimeout);
                    node_alt = new NodeJS_MatchImplementation(regexTimeout);
                }

                if (includePCRE2)
                {
                    pcre2 = new PCRE2_IsMatchImplementation(regexTimeout);
                    pcre2_alt = new PCRE2_MatchImplementation(regexTimeout);
                }
            }
        }

        internal class Compare_MatchImplementation : Compare_CommonImplementation
        {
            protected override string DefaultRegexOptions => DefaultMatchOptions;

            internal Compare_MatchImplementation(TimeSpan regexTimeout, bool includeDotNet, bool includeNode, bool includePCRE2)
            {
                if (includeDotNet)
                {
                    dotnet = new Library.MatchImplementation(regexTimeout);
                }

                if (includeNode)
                {
                    node = new NodeJS_MatchImplementation(regexTimeout);
                }

                if (includePCRE2)
                {
                    pcre2 = new PCRE2_MatchImplementation(regexTimeout);
                }
            }
        }

        internal class Compare_MatchAllImplementation : Compare_CommonImplementation
        {
            protected override string DefaultRegexOptions => DefaultMatchAllOptions;

            internal Compare_MatchAllImplementation(TimeSpan regexTimeout, bool includeDotNet, bool includeNode, bool includePCRE2)
            {
                if (includeDotNet)
                {
                    dotnet = new Library.MatchAllImplementation(regexTimeout);
                }

                if (includeNode)
                {
                    node = new NodeJS_MatchAllImplementation(regexTimeout);
                }

                if (includePCRE2)
                {
                    pcre2 = new PCRE2_MatchAllImplementation(regexTimeout);
                }
            }
        }
    }
}

#endif
