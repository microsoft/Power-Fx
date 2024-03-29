// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    public class LanguageServerOutput
    {
        public string Output { get; set; }

        public bool IsRequest { get; private set; } = true;

        public bool IsSuccessResponse { get; private set; } = false;

        private LanguageServerOutput()
        {
        }

        internal static LanguageServerOutput CreateSuccessOutput<T>(string id, T result)
        {
            return new LanguageServerOutput { Output = JsonRpcHelper.CreateSuccessResult(id, result), IsSuccessResponse = true, IsRequest = true };
        }

        internal static LanguageServerOutput CreateErrorOutput(string id, JsonRpcHelper.ErrorCode errorCode, string message = null)
        {
            return new LanguageServerOutput { Output = JsonRpcHelper.CreateErrorResult(id, errorCode, message), IsSuccessResponse = false, IsRequest = true };
        }

        internal static LanguageServerOutput CreateNotificationOutput<T>(string method, T notificationParams)
        {
            return new LanguageServerOutput { Output = JsonRpcHelper.CreateNotification(method, notificationParams), IsRequest = false, IsSuccessResponse = false };
        }
    }

    public class LanguageServerResponseBuilder : IEnumerable<LanguageServerOutput>
    {
        private readonly List<LanguageServerOutput> _response;

        public string Response
        {
            get
            {
                if (_response.Count == 1)
                {
                    return _response.First().Output;
                }

                return JsonSerializer.Serialize(_response.Select(responseItem => responseItem.Output));
            }
        }

        public LanguageServerResponseBuilder()
        {
            _response = new List<LanguageServerOutput>();
        }

        public void AddSuccessResponse<T>(string id, T result)
        {
            _response.Add(LanguageServerOutput.CreateSuccessOutput(id, result));
        }

        public void AddErrorResponse(string id, JsonRpcHelper.ErrorCode errorCode, string message = null)
        {
            _response.Add(LanguageServerOutput.CreateErrorOutput(id, errorCode, message));
        }

        public void AddNotification<T>(string method, T notificationParams)
        {
            _response.Add(LanguageServerOutput.CreateNotificationOutput(method, notificationParams));
        }

        public IEnumerator<LanguageServerOutput> GetEnumerator()
        {
            return _response.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
