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
            protected Library.RegexCommonImplementation node;
            protected Library.RegexCommonImplementation pcre2;
            protected Library.RegexCommonImplementation dotnet;

            internal override FormulaValue InvokeRegexFunction(string input, string regex, string options)
            {
                var nodeMatch = node.InvokeRegexFunction(input, regex, options);
                var nodeExpr = nodeMatch.ToExpression();

                var pcre2Match = pcre2.InvokeRegexFunction(input, regex, options);
                var pcre2Expr = pcre2Match.ToExpression();

                var dotnetMatch = dotnet.InvokeRegexFunction(input, regex, options);
                var dotnetExpr = dotnetMatch.ToExpression();

                if (nodeExpr != dotnetExpr)
                {
                    throw new Exception($"node != net on input='{input}', re='{regex}', options='{options}', node='{nodeExpr}', net='{dotnetExpr}'");
                }

                if (pcre2Expr != dotnetExpr)
                {
                    throw new Exception($"pcre2 != net on input='{input}', re='{regex}', options='{options}', pcre2='{pcre2Expr}', net='{dotnetExpr}'");
                }

                return dotnetMatch;
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
