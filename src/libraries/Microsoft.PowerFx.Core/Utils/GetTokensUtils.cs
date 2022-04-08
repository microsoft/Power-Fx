// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Public;

namespace Microsoft.PowerFx.Core.Utils
{
    internal static class GetTokensUtils
    {
        internal static IReadOnlyDictionary<string, TokenResultType> GetTokens(TexlBinding binding, GetTokensFlags flags)
        {
            var tokens = new Dictionary<string, TokenResultType>();

            if (binding == null)
            {
                return tokens;
            }

            if (flags.HasFlag(GetTokensFlags.UsedInExpression))
            {
                foreach (var item in binding.GetCalls())
                {
                    if (item.Function != null)
                    {
                        tokens[item.Function.QualifiedName] = TokenResultType.Function;
                    }
                }

                foreach (var item in binding.GetFirstNames())
                {
                    switch (item.Kind)
                    {
                        case BindKind.Control:
                        case BindKind.OptionSet:
                        case BindKind.PowerFxResolvedObject:
                            tokens[item.Name] = TokenResultType.HostSymbol;
                            break;
                        case BindKind.LambdaField:
                            tokens[item.Name] = TokenResultType.Variable;
                            break;
                        default:
                            break;
                    }
                }
            }

            if (flags.HasFlag(GetTokensFlags.AllFunctions))
            {
                foreach (var item in binding.NameResolver.Functions)
                {
                    tokens[item.QualifiedName] = TokenResultType.Function;
                }
            }

            return tokens;
        }
    }
}
