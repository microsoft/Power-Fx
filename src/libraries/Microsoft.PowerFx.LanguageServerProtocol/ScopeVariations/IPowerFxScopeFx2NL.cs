// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Intellisense;

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    /// <summary>
    /// Additional methods to get Fx2NL context.
    /// Hosts should prefer to use EditorContextScope directly. 
    /// </summary>
    public interface IPowerFxScopeFx2NL : IPowerFxScope
    {
        Fx2NLParameters GetFx2NLParameters();
    }
}
