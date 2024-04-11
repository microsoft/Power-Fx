// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.LanguageServerProtocol.Handlers;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    /// <summary>
    /// PowerFx Language server implementation
    ///
    /// LanguageServer can receive request/notification payload from client, it can also send request/notification to client.
    ///
    /// LanguageServer is hosted inside WebSocket or HTTP/HTTPS service
    ///   * For WebSocket, OnDataReceived() is for incoming traffic, SendToClient() is for outgoing traffic
    ///   * For HTTP/HTTPS, OnDataReceived() is for HTTP/HTTPS request, SendToClient() is queued up in next HTTP/HTTPS response.
    /// </summary>
    public class LanguageServer
    {
        /// <summary>
        /// This const represents the dummy formula that is used to create an infrastructure needed to get the symbols for Nl2Fx operation.
        /// </summary>
        public static readonly string Nl2FxDummyFormula = "\"f7979178-07f0-424d-8f8b-00fee6fd19b8\"";

        public delegate void SendToClient(string data);

        private readonly SendToClient _sendToClient;
        private readonly IPowerFxScopeFactory _scopeFactory;

        public delegate void NotifyDidChange(DidChangeTextDocumentParams didChangeParams);

        public event NotifyDidChange OnDidChange;

        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        };

        public delegate void OnLogUnhandledExceptionHandler(Exception e);

        /// <summary>
        /// Callback for host to get notified of unhandled exceptions that are happening asynchronously.
        /// This should be used for logging purposes. 
        /// This exists only for backward compat reasons now
        /// </summary>
        public event OnLogUnhandledExceptionHandler LogUnhandledExceptionHandler;

        /// <summary>
        /// If set, provides the handler for $/nlSuggestion message.
        /// Note: This is not a thread safe. Consider using the NlHandlerFactory.
        /// </summary>
        [Obsolete("Use NLHandlerFactory")]
        public NLHandler NL2FxImplementation { get; set; }

        /// <summary>
        /// A factory to get the NLHandler from the given scope.
        /// </summary>
        public INLHandlerFactory NLHandlerFactory { get; init; }

        private readonly ILanguageServerLogger _loggerInstance;

        private readonly ILanguageServerOperationHandlerFactory _handlerFactory;

        private readonly IHostTaskExecutor _hostTaskExecutor;

        [Obsolete("Use the constructor with ILanguageServerOperationHandlerFactory")]
        public LanguageServer(SendToClient sendToClient, IPowerFxScopeFactory scopeFactory, Action<string> logger = null)
        {
            Contracts.AssertValue(sendToClient);
            Contracts.AssertValue(scopeFactory);

            _sendToClient = sendToClient;
            _scopeFactory = scopeFactory;
            _loggerInstance = logger == null ? null : new BackwardsCompatibleLogger(logger);
        }

        public LanguageServer(
            IPowerFxScopeFactory scopeFactory,
            ILanguageServerOperationHandlerFactory operationHandlerFactory = null, 
            IHostTaskExecutor hostTaskExecutor = null,
            ILanguageServerLogger languageServerLogger = null)
        {
            _scopeFactory = scopeFactory;
            _handlerFactory = operationHandlerFactory;
            _hostTaskExecutor = hostTaskExecutor;
            _loggerInstance = languageServerLogger;
        }
   
        // Only exists for backward compat
        private INLHandlerFactory GetNLHandlerFactory()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return NLHandlerFactory ?? new BackwardsCompatibleNLHandlerFactory(NL2FxImplementation);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private ILanguageServerOperationHandlerFactory GetHandlerFactory()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return _handlerFactory ?? new DefaultLanguageServerOperationHandlerFactory(GetNLHandlerFactory(), OnDidChange);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// Asynchronously process the incoming request/notification payload from client.
        /// </summary>
        /// <param name="input">Parsed Language Server Input.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns> Response to be sent to the client.</returns>
        public async Task<string> OnDataReceivedAsync(LanguageServerInput input, CancellationToken cancellationToken = default)
        {
            var outputBuilder = await OnDataReceivedAsyncInternal(input, cancellationToken).ConfigureAwait(false);
            return outputBuilder.Response;
        }

        /// <summary>
        /// Asynchronously process the incoming request/notification payload from client.
        /// </summary>
        /// <param name="jsonRpcPayload">Raw Input from the client.</param>
        /// <param name="cancellationToken"> Cancellation Token.</param>
        /// <returns> Response to be sent to the client.</returns>
        public async Task<string> OnDataReceivedAsync(string jsonRpcPayload, CancellationToken cancellationToken = default)
        {
            var outputBuilder = await OnDataReceivedAsyncInternal(jsonRpcPayload, cancellationToken).ConfigureAwait(false);
            return outputBuilder.Response;
        }

        private async Task<LanguageServerOutputBuilder> OnDataReceivedAsyncInternal(string jsonRpcPayload, CancellationToken cancellationToken)
        {
            var input = LanguageServerInput.Parse(jsonRpcPayload);
            return await OnDataReceivedAsyncInternal(input, cancellationToken).ConfigureAwait(false);
        }

        private async Task<LanguageServerOutputBuilder> OnDataReceivedAsyncInternal(LanguageServerInput input, CancellationToken cancellationToken)
        {
            var outputBuilder = new LanguageServerOutputBuilder();
            try
            {
                if (input == null)
                {
                    outputBuilder.AddParseError(null, "Could not parse the incoming params");
                    return outputBuilder;
                }

                if (input.Method == null)
                {
                    _loggerInstance?.LogError($"[PFX] OnDataReceived CreateErrorResult InvalidRequest (method not found)");
                    outputBuilder.AddInvalidRequestError(input.Id, $"Invalid method");
                    return outputBuilder;
                }

                var handler = GetHandlerFactory().GetHandler(input.Method, new HandlerCreationContext(LogUnhandledExceptionHandler));

                if (handler == null)
                {
                    _loggerInstance?.LogError($"[PFX] OnDataReceived CreateErrorResult InvalidRequest (params not found)");
                    outputBuilder.AddMethodNotFoundError(input.Id, $"No handler for method {input.Method} was found");
                    return outputBuilder;
                }

                var inputParams = input.RawParams;

                if (string.IsNullOrWhiteSpace(inputParams))
                {
                    outputBuilder.AddInvalidRequestError(input.Id, $"Invalid params for method {input.Method}");
                    return outputBuilder;
                }

                if (handler.IsRequest && string.IsNullOrWhiteSpace(input.Id))
                {
                    // This is bit tricky. Language Server Protocol requires id for request
                    // But what if id is not provided for request?
                    // This error response won't be acknowledged at the client level
                    outputBuilder.AddInvalidRequestError(input.Id, $"Invalid request id for method {input.Method}");
                    return outputBuilder;
                }

                var context = new LanguageServerOperationContext(_scopeFactory)
                {
                    LspMethod = input.Method,
                    Logger = _loggerInstance,
                    RequestId = input.Id,
                    RawOperationInput = inputParams,
                    OutputBuilder = outputBuilder,
                    HostTaskExecutor = _hostTaskExecutor
                };

                await handler.HandleAsync(context, cancellationToken).ConfigureAwait(false);
                return outputBuilder;
            }
            catch (Exception ex)
            {
                _loggerInstance?.LogException(ex);

                // this exists only for backward compat
                LogUnhandledExceptionHandler?.Invoke(ex);
                outputBuilder.AddErrorResponse(input.Id, JsonRpcHelper.ErrorCode.InternalError, ex.GetDetailedExceptionMessage());
                return outputBuilder;
            }
        }

        /// <summary>
        /// Received request/notification payload from client.
        /// Note: Do not use this overload. Move to using OnDataReceivedAsync overloads.
        /// Note: This only exists for backward compatibility and runs synchronously which could affect performance.
        /// </summary>
        [Obsolete("Use OnDataReceivedAsync Overloads", false)]
        public void OnDataReceived(string jsonRpcPayload)
        {
            Contracts.AssertValue(jsonRpcPayload);

            // Only log when logger is provided and not null
            // Creating log string is expensive here, especially for the completion requests
            // Only create log strings when logger is not null
            _loggerInstance?.LogInformation($"[PFX] OnDataReceived Received: {jsonRpcPayload ?? "<null>"}");
            var outputBuilder = OnDataReceivedAsyncInternal(jsonRpcPayload, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

            try
            {
                foreach (var outputItem in outputBuilder)
                {
                    _sendToClient?.Invoke(outputItem.Output);
                }
            }
            catch (Exception ex)
            {
                _loggerInstance?.LogException(ex);
                LogUnhandledExceptionHandler?.Invoke(ex);
            }
        }

        #region Need to keep these functions to not breaking binaries
        protected int GetPosition(string expression, int line, int character)
        {
            return PositionRangeHelper.GetPosition(expression, line, character);
        }

        public static Range GetRange(string expression, Span span)
        {
            return span.ConvertSpanToRange(expression);
        }

        protected static int GetCharPosition(string expression, int position)
        {
            return PositionRangeHelper.GetCharPosition(expression, position);
        }
        #endregion
    }
}
