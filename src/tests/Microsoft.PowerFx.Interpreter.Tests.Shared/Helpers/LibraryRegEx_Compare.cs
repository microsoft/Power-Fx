// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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
                { new IsMatchFunction(), new Compare_IsMatchImplementation(regexTimeout) },
                { new MatchFunction(regexCache), new Compare_MatchImplementation(regexTimeout) },
                { new MatchAllFunction(regexCache), new Compare_MatchAllImplementation(regexTimeout) }
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
                var nodeMatch = node.InvokeRegexFunction(input, regex, options);
                var nodeExpr = nodeMatch.ToExpression();

                var pcre2Match = pcre2.InvokeRegexFunction(input, regex, options);
                var pcre2Expr = pcre2Match.ToExpression();

                var dotnetMatch = dotnet.InvokeRegexFunction(input, regex, options);
                var dotnetExpr = dotnetMatch.ToExpression();

                if (nodeExpr != dotnetExpr)
                {
                    throw new Exception($"{kind}: node != net on re='{regex}' options='{options}'\n  input='{input}' ({CharCodes(input)})\n  net='{dotnetExpr}'\n  node='{nodeExpr}'\n  pcre2='{pcre2Expr}'\n");
                }

                if (pcre2Expr != dotnetExpr)
                {
                    throw new Exception($"{kind}: pcre2 != net on re='{regex}' options='{options}'\n  input='{input}' ({CharCodes(input)})\n  net='{dotnetExpr}'\n  node='{nodeExpr}'\n  pcre2='{pcre2Expr}'\n");
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

            internal Compare_IsMatchImplementation(TimeSpan regexTimeout) 
            {
                node = new NodeJS_IsMatchImplementation(regexTimeout);
                pcre2 = new PCRE2_IsMatchImplementation(regexTimeout);
                dotnet = new Library.IsMatchImplementation(regexTimeout);

                node_alt = new NodeJS_MatchImplementation(regexTimeout);
                pcre2_alt = new PCRE2_MatchImplementation(regexTimeout);
                dotnet_alt = new Library.MatchImplementation(regexTimeout);
            }
        }

        internal class Compare_MatchImplementation : Compare_CommonImplementation
        {
            protected override string DefaultRegexOptions => DefaultMatchOptions;

            internal Compare_MatchImplementation(TimeSpan regexTimeout)
            {
                node = new NodeJS_MatchImplementation(regexTimeout);
                pcre2 = new PCRE2_MatchImplementation(regexTimeout);
                dotnet = new Library.MatchImplementation(regexTimeout);
            }
        }

        internal class Compare_MatchAllImplementation : Compare_CommonImplementation
        {
            protected override string DefaultRegexOptions => DefaultMatchAllOptions;

            internal Compare_MatchAllImplementation(TimeSpan regexTimeout)
            {
                node = new NodeJS_MatchAllImplementation(regexTimeout);
                pcre2 = new PCRE2_MatchAllImplementation(regexTimeout);
                dotnet = new Library.MatchAllImplementation(regexTimeout);
            }
        }
    }
}
