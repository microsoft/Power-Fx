// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    /// <summary>
    /// An output builder to build Language Server Output.
    /// Lifecycle: A new instance of the builder should be created for each request.
    /// Can hold more than one output and also different types of outputs.
    /// Request/Notification Hanlders are free to add multiple outputs to the builder.
    /// </summary>
    public class LanguageServerOutputBuilder : IEnumerable<LanguageServerOutput>
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new ()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly List<LanguageServerOutput> _outputs = new ();

        public int Size => _outputs.Count;

        /// <summary>
        /// A serialized output created from all the outputs in the builder.
        /// </summary>
        public string Response
        {
            get
            {
                if (_outputs.Count == 0)
                {
                    return string.Empty;
                }

                if (_outputs.Count == 1)
                {
                    return _outputs[0].Output;
                }

                return JsonSerializer.Serialize(_outputs.Select(output => output.Output), _jsonSerializerOptions);
            }
        }

        /// <summary>
        /// Add a success response with the result.
        /// </summary>
        /// <typeparam name="T">Type of the result.</typeparam>
        /// <param name="id">Id of the request.</param>
        /// <param name="result">Result.</param>
        public void AddSuccessResponse<T>(string id, T result)
        {
            _outputs.Add(LanguageServerOutput.CreateSuccessResult(id, result));
        }

        /// <summary>
        /// Add an error response.
        /// </summary>
        /// <param name="id"> Id of the request.</param>
        /// <param name="code"> Error Code.</param>
        /// <param name="message"> Error Message.</param>
        public void AddErrorResponse(string id, JsonRpcHelper.ErrorCode code, string message = null)
        {
            _outputs.Add(LanguageServerOutput.CreateErrorResult(id, code, message));
        }

        /// <summary>
        /// Add a notification.
        /// </summary>
        /// <typeparam name="T"> Type of the notification params.</typeparam>
        /// <param name="method"> Method of the notification.</param>
        /// <param name="notificationParams"> Notification Params.</param>
        public void AddNotification<T>(string method, T notificationParams)
        {
            _outputs.Add(LanguageServerOutput.CreateNotification(method, notificationParams));
        }

        public IEnumerator<LanguageServerOutput> GetEnumerator()
        {
            return _outputs.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    ///  Helper class to build Language Server Output.
    /// </summary>
    internal static class LanguageServerOutputBuilderExtensions
    {
        private const string DefaultRequestCancelledMessage = "Lsp request with id {0} was canceled";

        /// <summary>
        /// Add an error response with ParseError error code.
        /// </summary>
        /// <param name="outputBuilder">Output Builder.</param>
        /// <param name="id">Request Id.</param>
        /// <param name="message">Option Error Message.</param>
        public static void AddParseError(this LanguageServerOutputBuilder outputBuilder, string id, string message = null)
        {
            outputBuilder.AddErrorResponse(id, JsonRpcHelper.ErrorCode.ParseError, message);
        }

        /// <summary>
        /// Add an error response with InternalError error code.
        /// </summary>
        /// <param name="outputBuilder">Output Builder.</param>
        /// <param name="id">Request Id.</param>
        /// <param name="message">Option Error Message.</param>
        public static void AddInternalError(this LanguageServerOutputBuilder outputBuilder, string id, string message = null)
        {
            outputBuilder.AddErrorResponse(id, JsonRpcHelper.ErrorCode.InternalError, message);
        }

        /// <summary>
        /// Add an error response with InvalidParams error code.
        /// </summary>
        /// <param name="outputBuilder">Output Builder.</param>
        /// <param name="id">Request Id.</param>
        /// <param name="message">Option Error Message.</param>
        public static void AddInvalidParamsError(this LanguageServerOutputBuilder outputBuilder, string id, string message = null)
        {
            outputBuilder.AddErrorResponse(id, JsonRpcHelper.ErrorCode.InvalidParams, message);
        }

        /// <summary>
        /// Add an error response with PropertyValueRequired error code.
        /// </summary>
        /// <param name="outputBuilder">Output Builder.</param>
        /// <param name="id">Request Id.</param>
        /// <param name="message">Option Error Message.</param>
        public static void AddProperyValueRequiredError(this LanguageServerOutputBuilder outputBuilder, string id, string message = null)
        {
            outputBuilder.AddErrorResponse(id, JsonRpcHelper.ErrorCode.PropertyValueRequired, message);
        }

        /// <summary>
        /// Add an error response with InvalidRequest error code.
        /// </summary>
        /// <param name="outputBuilder">Output Builder.</param>
        /// <param name="id">Request Id.</param>
        /// <param name="message">Option Error Message.</param>
        public static void AddInvalidRequestError(this LanguageServerOutputBuilder outputBuilder, string id, string message = null)
        {
            outputBuilder.AddErrorResponse(id, JsonRpcHelper.ErrorCode.InvalidRequest, message);
        }

        /// <summary>
        /// Add an error response with MethodNotFound error code.
        /// </summary>
        /// <param name="outputBuilder">Output Builder.</param>
        /// <param name="id">Request Id.</param>
        /// <param name="message">Option Error Message.</param>
        public static void AddMethodNotFoundError(this LanguageServerOutputBuilder outputBuilder, string id, string message = null)
        {
            outputBuilder.AddErrorResponse(id, JsonRpcHelper.ErrorCode.MethodNotFound, message);
        }

        /// <summary>
        /// Add an error response with RequestCancelled error code.
        /// </summary>
        /// <param name="outputBuilder">Output Builder.</param>
        /// <param name="id">Request Id.</param>
        /// <param name="message">Option Error Message.</param>
        public static void AddRequestCancelledError(this LanguageServerOutputBuilder outputBuilder, string id, string message = null)
        {
            var prefix = string.Format(CultureInfo.InvariantCulture, DefaultRequestCancelledMessage, id);
            message = string.IsNullOrEmpty(message) ? prefix : $"{prefix}. {message}";
            outputBuilder.AddErrorResponse(id, JsonRpcHelper.ErrorCode.RequestCancelled, message);
        }

        /// <summary>
        /// Add an error response with RequestCancelled error code if the exception is a cancellation exception or cancellation was requested on the token.
        /// </summary>
        /// <param name="outputBuilder">Output Builder.</param>
        /// <param name="id">Request Id.</param>
        /// <param name="exception">Exception.</param>
        /// <param name="cancellationToken" >Cancellation Token.</param>
        /// <returns>True if the error was added, false otherwise.</returns>
        public static bool TryAddRequestCancelledErrorIfApplicable(this LanguageServerOutputBuilder outputBuilder, string id, CancellationToken cancellationToken, Exception exception = null)
        {
            // Theoretically, we should not have anything in the output builder if cancellation was requested.
            // If there's something then we should not add the error and respect what's already there.
            // Adding the cancellation error might add extra noise to the output.
            if (outputBuilder.Size > 0)
            {
                return false;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                outputBuilder.AddRequestCancelledError(id, exception?.GetDetailedExceptionMessage());
                return true;
            }

            if (exception is OperationCanceledException || 

                // Downstream nl2fx or fx2nl dependencies might throw InvalidOperationException with message containing "canceled".
                // Instead of throwing OperationCanceledException
                (exception is InvalidOperationException && 
                exception.Message != null && exception.Message.ToLowerInvariant().Contains("canceled")))
            {
                outputBuilder.AddRequestCancelledError(id, exception?.GetDetailedExceptionMessage());
                return true;
            }

            return false;
        }
    }
}
