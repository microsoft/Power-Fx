// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    /// <summary>
    /// A representation of the output of a language server.
    /// </summary>
    public class LanguageServerOutput
    {
        /// <summary>
        ///  A particular stringified output of the language server.
        /// </summary>
        public string Output { get; private set; }

        private LanguageServerOutput()
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="LanguageServerOutput"/> with the given error information.
        /// </summary>
        /// <param name="id"> The id of the request that resulted in the error. </param>
        /// <param name="code"> The error code. </param>
        /// <param name="message"> The error message. </param>
        /// <returns> A new instance of <see cref="LanguageServerOutput"/> with the given error information. </returns>
        public static LanguageServerOutput CreateErrorResult(string id, JsonRpcHelper.ErrorCode code, string message = null)
        {
            return new LanguageServerOutput
            {
                Output = JsonRpcHelper.CreateErrorResult(id, code, message),
            };
        }

        /// <summary>
        /// Creates a new instance of <see cref="LanguageServerOutput"/> with the given internal server error.
        /// </summary>
        /// <param name="id"> The id of the request that resulted in the error. </param>
        /// <param name="message"> The error message. </param>
        /// <returns> A new instance of <see cref="LanguageServerOutput"/> with the given error information. </returns>
        public static LanguageServerOutput CreateInternalServerErrorOutput(string id, string message = null)
        {
            return CreateErrorResult(id, JsonRpcHelper.ErrorCode.InternalError, message);
        }

        /// <summary>
        /// Creates a new instance of <see cref="LanguageServerOutput"/> with the given success result.
        /// </summary>
        /// <typeparam name="T"> The type of the result. </typeparam>
        /// <param name="id"> The id of the request that resulted in the success. </param>
        /// <param name="result"> The result. </param>
        /// <returns> A new instance of <see cref="LanguageServerOutput"/> with the given success result. </returns>
        public static LanguageServerOutput CreateSuccessResult<T>(string id, T result)
        {
            return new LanguageServerOutput
            {
                Output = JsonRpcHelper.CreateSuccessResult(id, result),
            };
        }

        /// <summary>
        /// Creates a new instance of <see cref="LanguageServerOutput"/> with the given notification.
        /// </summary>
        /// <typeparam name="T"> The type of the notification parameters. </typeparam>
        /// <param name="method"> The method of the notification. </param>
        /// <param name="notificationParams"> The notification parameters. </param>
        /// <returns> A new instance of <see cref="LanguageServerOutput"/> with the given notification. </returns>
        public static LanguageServerOutput CreateNotification<T>(string method, T notificationParams)
        {
            return new LanguageServerOutput
            {
                Output = JsonRpcHelper.CreateNotification(method, notificationParams),
            };
        }
    }
}
