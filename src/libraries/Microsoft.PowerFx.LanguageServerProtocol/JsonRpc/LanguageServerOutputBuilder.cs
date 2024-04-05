// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    public class LanguageServerOutputBuilder : IEnumerable<LanguageServerOutput>
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new ()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly List<LanguageServerOutput> _outputs = new ();

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

        public void AddSuccessResponse<T>(string id, T result)
        {
            _outputs.Add(LanguageServerOutput.CreateSuccessResult(id, result));
        }

        public void AddErrorResponse(string id, JsonRpcHelper.ErrorCode code, string message = null)
        {
            _outputs.Add(LanguageServerOutput.CreateErrorResult(id, code, message));
        }

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
    }
}
