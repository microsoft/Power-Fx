// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.LanguageServerProtocol.Handlers
{
    /// <summary>
    /// Represents the result of pre-handling step of NL2Fx.
    /// </summary>
    /// <param name="parameters"> Nl2Fx Parameters computed during pre handle step. </param>
    public record Nl2FxPreHandleResult(NL2FxParameters parameters);

    /// <summary>
    /// Represents the result of NL2Fx model.
    /// </summary>
    /// <param name="actualResult">Actual result from the model.</param>
    public record Nl2FxResult(CustomNL2FxResult actualResult);

    /// <summary>
    /// Represents the context for handling NL2Fx.
    /// Lifetime of this object is limited to the handling of a single NL2Fx request.
    /// For each request, a new instance of this object is created.
    /// This is used to pass the results of different steps across the different stages of handling Nl2Fx.
    /// This is immutable and is progressively updated as we move through the different stages of handling Nl2Fx.
    /// This is thread safe as it is immutable and a different copy is yielded as each stage adds more information to it.
    /// </summary>
    /// <param name="nl2FxRequestParams">Parameters for Nl2Fx request.</param>
    /// <param name="preHandleResult">Result of Nl2Fx Prehandle stage.</param>
    /// <param name="nl2FxResult">Result of talking to Nl2Fx Model.</param>
    public record Nl2FxHandleContext(CustomNL2FxParams nl2FxRequestParams, Nl2FxPreHandleResult preHandleResult, Nl2FxResult nl2FxResult);

    /// <summary>
    ///  Represents the result of pre-handling step of NL2Fx for a handler that addresses backwards compatibility.
    /// </summary>
    /// <param name="nlHandler"> NlHandler instance to be used for NL2Fx. </param>
    /// <param name="basePreHandleResult"> Base pre handle result. </param>
    internal record BackwardsCompatibleNl2FxPreHandleResult(NLHandler nlHandler, Nl2FxPreHandleResult basePreHandleResult)
        : Nl2FxPreHandleResult(basePreHandleResult?.parameters);
}
