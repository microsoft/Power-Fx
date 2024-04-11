// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.LanguageServerProtocol.Handlers
{
    /// <summary>
    /// Represents the result of pre-handling step of Fx2Nl.
    /// </summary>
    /// <param name="checkResult">Check result of pre handle step.</param>
    /// <param name="parameters"> Fx2Nl Parameters computed during pre handle step. </param>
    public record Fx2NlPreHandleResult(CheckResult checkResult, Fx2NLParameters parameters);

    /// <summary>
    /// Represents the context for handling Fx2Nl.
    /// Lifetime of this object is limited to the handling of a single Fx2Nl request.
    /// For each request, a new instance of this object is created.
    /// This is used to pass the results of different steps across the different stages of handling Fx2Nl.
    /// This is immutable and is progressively updated as we move through the different stages of handling Fx2Nl.
    /// This is thread safe as it is immutable and a different copy is yielded as each stage adds more information to it.
    /// </summary>
    /// <param name="fx2NlRequestParams">Parameters for Fx2Nl request.</param>
    /// <param name="preHandleResult">Result of Fx2Nl Prehandle stage.</param>
    public record Fx2NlHandleContext(CustomFx2NLParams fx2NlRequestParams, Fx2NlPreHandleResult preHandleResult);
}
