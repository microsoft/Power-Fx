// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Intellisense;

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    /// <summary>
    /// Retrieves additional NL2Fx information.
    /// </summary>
    public interface IPowerFxScopeNL2Fx : IPowerFxScope
    {
        NL2FxParameters GetNL2FxParameters();
    }
}
