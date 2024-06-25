// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.LanguageServerProtocol.Handlers;
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

        [Obsolete("Call overload with Fx2NLParameters")]
        public virtual Task<CustomFx2NLResult> Fx2NLAsync(CheckResult check, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }
        
        public virtual async Task<CustomFx2NLResult> Fx2NLAsync(CheckResult check, Fx2NLParameters hints, CancellationToken cancel)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return await Fx2NLAsync(check, cancel).ConfigureAwait(false);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// Additional hook to run pre-handle logic for NL2Fx.
        /// </summary>
        /// <param name="nl2fxParameters">Nl2fx Parameters computed from defualt pre handle.</param>
        /// <param name="nl2FxRequestParams">Nl2fx Request Params.</param>
        /// <param name="operationContext">Language Server Operation Context.</param>
        /// <exception cref="NotImplementedException">Not implemeted by default.</exception>
        public virtual void PreHandleNl2Fx(CustomNL2FxParams nl2FxRequestParams, NL2FxParameters nl2fxParameters, LanguageServerOperationContext operationContext)
        {
            // no op by default
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

        /// <summary>
        /// Set the locale that expect the output expression to be.
        /// For example: "vi-VN" or "fr-FR".
        /// </summary>
        public CultureInfo ExpressionCultureInfo { get; set; }
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
