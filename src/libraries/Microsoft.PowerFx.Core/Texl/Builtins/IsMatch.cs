// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // IsMatch(text:s, format:s)
    // Checks if the input text is of the correct format.
    internal sealed class IsMatchFunction : BuiltinFunction
    {
        public override bool UseParentScopeForArgumentSuggestions => true;

        public override bool IsSelfContained => true;

        public override bool HasPreciseErrors => true;

        public IsMatchFunction()
            : base("IsMatch", TexlStrings.AboutIsMatch, FunctionCategories.Text, DType.Boolean, 0, 2, 3, DType.String, DType.String, DType.String)
        {
        }

        public override IEnumerable<string> GetRequiredEnumNames()
        {
            return new List<string>() { LanguageConstants.MatchEnumString, LanguageConstants.MatchOptionsEnumString };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.IsMatchArg1, TexlStrings.IsMatchArg2 };
            yield return new[] { TexlStrings.IsMatchArg1, TexlStrings.IsMatchArg2, TexlStrings.IsMatchArg3 };
        }

        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            if (argTypes[1].Kind != DKind.String && argTypes[1].Kind != DKind.OptionSetValue)
            {
                errors.EnsureError(args[1], TexlStrings.ErrVariableRegEx);
            }
        }

        public override bool HasSuggestionsForParam(int index)
        {
            Contracts.Assert(index >= 0);

            return index <= 2;
        }
    }
}

#pragma warning restore SA1649 // File name should match first type name
