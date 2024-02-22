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
        public virtual bool SupportsNL2Fx { get; } = false;

        public virtual bool SupportsFx2NL { get; } = false;

        public virtual Task<CustomNL2FxResult> NL2FxAsync(NL2FxParameters request, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public virtual Task<CustomFx2NLResult> Fx2NLAsync(CheckResult check, Fx2NLParameters hints, CancellationToken cancel)
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

        // Engine can provide ambient details for NL, such as config and feature flags.
        public Engine Engine { get; set; }
    }

    /// <summary>
    /// Additional context passed from Fx2NL. 
    /// This should be information that we can't get from a <see cref="CheckResult"/>.
    /// </summary>
    public class Fx2NLParameters
    {
        /// <summary>
        ///  Optional. Additional AppContext about where this expresion is used. 
        /// </summary>
        public UsageHints UsageHints { get; set; }

        // we may add additional app context...
    }

    /// <summary>
    /// Additional context that can help explain an expression.
    /// This is purely optional and used for heuristics. 
    /// </summary>
    public class UsageHints
    {
        /// <summary>
        /// Name of control in the document. Eg, "Label1".
        /// </summary>
        public string ControlName { get; set; }

        /// <summary>
        /// Kind of control, eg "Button", "Gallery", etc.
        /// </summary>
        public string ControlKind { get; set; }

        /// <summary>
        /// Name of property that this expression is assigned to. 
        /// </summary>
        public string PropertyName { get; set; }

        // Many usages can be mapped to control/property. 
        // Other possible usage locations:
        //  FieldName. 

        public string PropertyDescription { get; set; }
    }
}
