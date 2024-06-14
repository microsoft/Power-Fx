﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Microsoft.PowerFx.Connectors
{
    public class CdpServiceBase
    {
        protected CdpServiceBase()
        {
        }

        protected internal static async Task<T> GetObject<T>(HttpClient httpClient, string message, string uri, CancellationToken cancellationToken, ConnectorLogger logger = null, [CallerMemberName] string callingMethod = "")
            where T : class, new()
        {
            cancellationToken.ThrowIfCancellationRequested();

            string result = await GetObject(httpClient, message, uri, cancellationToken, logger, $"{callingMethod}<{typeof(T).Name}>").ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(result) ? null : JsonSerializer.Deserialize<T>(result);
        }

        protected internal static async Task<string> GetObject(HttpClient httpClient, string message, string uri, CancellationToken cancellationToken, ConnectorLogger logger = null, [CallerMemberName] string callingMethod = "")
        {
            cancellationToken.ThrowIfCancellationRequested();
            string log = $"{callingMethod}.{nameof(GetObject)} for {message}, Uri {uri}";

            try
            {
                logger?.LogInformation($"Entering in {log}");

                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

                string text = response?.Content == null ? string.Empty : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                int statusCode = (int)response.StatusCode;

                string reasonPhrase = string.IsNullOrEmpty(response.ReasonPhrase) ? string.Empty : $" ({response.ReasonPhrase})";
                logger?.LogInformation($"Exiting {log}, with Http Status {statusCode}{reasonPhrase}{(statusCode < 300 ? string.Empty : text)}");

                return statusCode < 300 ? text : throw new PowerFxConnectorException($"CDP call to {uri} failed with {statusCode} error: {reasonPhrase} - {text}") { StatusCode = statusCode };
            }
            catch (Exception ex)
            {
                logger?.LogException(ex, $"Exception in {log}, {ConnectorHelperFunctions.LogException(ex)}");
                throw;
            }
        }

        internal static string DoubleEncode(string param)
        {
            return HttpUtility.UrlEncode(HttpUtility.UrlEncode(param));
        }
    }
}
