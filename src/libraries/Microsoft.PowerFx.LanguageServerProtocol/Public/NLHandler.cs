// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using static Microsoft.PowerFx.LanguageServerProtocol.LanguageServer;

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    /// <summary>
    /// Callback handler invoked by <see cref="LanguageServer"/> to handle NL requests. 
    /// </summary>
    public class NLHandler
    {
        public virtual Task<CustomNL2FxResult> NL2Fx(NL2FxParameters request, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Resolved from <see cref="CustomNL2FxParams"/>.
    /// </summary>
    public class NL2FxParameters
    {
        public string Sentence { get; set; }

        // Current symbols to pass into NL prompt 
        public CheckContextSummary SymbolSummary { get; set; }
    }
}
