// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    public class LanguageServerOutput
    {
        public string Output { get; init; }

        private LanguageServerOutput()
        {
        }

        public static LanguageServerOutput CreateErrorResult(string id, JsonRpcHelper.ErrorCode code, string message = null)
        {
            return new LanguageServerOutput
            {
                Output = JsonRpcHelper.CreateErrorResult(id, code, message)
            };
        }

        public static LanguageServerOutput CreateSuccessResult<T>(string id, T result)
        {
            return new LanguageServerOutput
            {
                Output = JsonRpcHelper.CreateSuccessResult(id, result)
            };
        }

        public static LanguageServerOutput CreateNotification<T>(string method, T notificationParams)
        {
            return new LanguageServerOutput
            {
                Output = JsonRpcHelper.CreateNotification(method, notificationParams)
            };
        }
    }
}
