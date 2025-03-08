﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

#if MATCHCOMPARE

// This file compares the results from .NET, PCRE2, and NODEJS.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Functions.RegEx_NodeJS;
using static Microsoft.PowerFx.Functions.RegEx_PCRE2;

namespace Microsoft.PowerFx.Functions
{
    public class RegEx_Compare
    {
        public static void EnableRegExFunctions(PowerFxConfig config, TimeSpan regExTimeout = default, int regexCacheSize = -1, bool includeNode = true, bool includePCRE2 = true)
        {
            RegexTypeCache regexTypeCache = new (regexCacheSize);

            foreach (KeyValuePair<TexlFunction, IAsyncTexlFunction> func in RegexFunctions(regExTimeout, regexTypeCache, includeNode, includePCRE2))
            {
                if (config.ComposedConfigSymbols.Functions.AnyWithName(func.Key.Name))
                {
                    throw new InvalidOperationException("Cannot add RegEx functions more than once.");
                }

                config.InternalConfigSymbols.AddFunction(func.Key);
                config.AdditionalFunctions.Add(func.Key, func.Value);
            }
        }

        internal static Dictionary<TexlFunction, IAsyncTexlFunction> RegexFunctions(TimeSpan regexTimeout, RegexTypeCache regexCache, bool includeNode, bool includePCRE2)
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
                { new IsMatchFunction(regexCache), new Compare_IsMatchImplementation(regexTimeout, includeNode, includePCRE2) },
                { new MatchFunction(regexCache), new Compare_MatchImplementation(regexTimeout, includeNode, includePCRE2) },
                { new MatchAllFunction(regexCache), new Compare_MatchAllImplementation(regexTimeout, includeNode, includePCRE2) }
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

            private string CharCodes(string text)
            {
                StringBuilder sb = new StringBuilder();

                foreach (char c in text)
                {
                    sb.Append(Convert.ToInt32(c).ToString("X4"));
                    sb.Append(" ");
                }

                if (sb.Length > 0)
                {
                    return sb.ToString().Substring(0, sb.Length - 1);
                }
                else
                {
                    return string.Empty;
                }    
            }

            private FormulaValue InvokeRegexFunctionOne(string input, string regex, string options, Library.RegexCommonImplementation dotnet, Library.RegexCommonImplementation node, Library.RegexCommonImplementation pcre2, string kind)
            {
                var dotnetMatch = dotnet.InvokeRegexFunction(input, regex, options);
                var dotnetExpr = dotnetMatch.ToExpression();

                string nodeExpr = null;
                string pcre2Expr = null;

                if (node != null)
                {
                    var nodeMatch = node.InvokeRegexFunction(input, regex, options);
                    nodeExpr = nodeMatch.ToExpression();
                }

                if (pcre2 != null)
                {
                    var pcre2Match = pcre2.InvokeRegexFunction(input, regex, options);
                    pcre2Expr = pcre2Match.ToExpression();
                }

                string prefix = null;

                if (nodeExpr != null && nodeExpr != dotnetExpr)
                {
                    prefix = $"{kind}: node != net";        
                }

                if (pcre2Expr != null && pcre2Expr != dotnetExpr)
                {
                    prefix = $"{kind}: pcre2 != net";
                }

                if (prefix != null)
                {
                    var report =
                        $"  re='{regex}' options='{options}'\n" +
                        $"  input='{input}' ({CharCodes(input)})\n" +
                        $"  net={dotnetExpr}\n" +
                        (nodeExpr != null ? $"  node={nodeExpr}\n" : string.Empty) +
                        (pcre2Expr != null ? $"  pcre2={pcre2Expr}\n" : string.Empty);

                    throw new Exception($"{prefix}\n{report}");
                }

                return dotnetMatch;
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

            internal Compare_IsMatchImplementation(TimeSpan regexTimeout, bool includeNode, bool includePCRE2) 
            {
                dotnet = new Library.IsMatchImplementation(regexTimeout);
                dotnet_alt = new Library.MatchImplementation(regexTimeout);

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

            internal Compare_MatchImplementation(TimeSpan regexTimeout, bool includeNode, bool includePCRE2)
            {
                if (includeNode)
                {
                    node = new NodeJS_MatchImplementation(regexTimeout);
                }

                if (includePCRE2)
                {
                    pcre2 = new PCRE2_MatchImplementation(regexTimeout);
                }

                dotnet = new Library.MatchImplementation(regexTimeout);
            }
        }

        internal class Compare_MatchAllImplementation : Compare_CommonImplementation
        {
            protected override string DefaultRegexOptions => DefaultMatchAllOptions;

            internal Compare_MatchAllImplementation(TimeSpan regexTimeout, bool includeNode, bool includePCRE2)
            {
                if (includeNode)
                {
                    node = new NodeJS_MatchAllImplementation(regexTimeout);
                }

                if (includePCRE2)
                {
                    pcre2 = new PCRE2_MatchAllImplementation(regexTimeout);
                }

                dotnet = new Library.MatchAllImplementation(regexTimeout);
            }
        }
    }
}

#endif
