// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Resgister a handle for providing code-fix results. 
    /// </summary>
    public interface ICodeFixHandler
    {
        CodeActionResult[] SuggestFixes(
            Engine engine,
            CheckResult checkResult);
    }
}
