// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Expression re-writer interface. This will be leveraged by InitalFixup method to rewrite the expression in LSP.
    /// </summary>
    public interface IExpressionRewriter
    {
        public string Process(CheckResult check);
    }
}
