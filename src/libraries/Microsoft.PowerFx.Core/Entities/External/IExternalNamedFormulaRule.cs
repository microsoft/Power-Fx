// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.App
{
    /// <summary>
    /// Wraps a named formula's Formula, Rule, Token, and ResultType.
    /// </summary>
    internal interface IExternalNamedFormulaRule
    {
        Formula Formula { get; }

        IExternalRule Rule { get; }

        IdentToken Token { get; }

        DType ResultType { get; }
    }
}
