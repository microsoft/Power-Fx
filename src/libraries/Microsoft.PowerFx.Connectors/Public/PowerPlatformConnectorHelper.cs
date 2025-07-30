// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using static Microsoft.PowerFx.Connectors.ConnectorSettings;

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Static builder/factory for retrieving DelegatingHandlers for Power Platform connector clients.
    /// </summary>
    public static class PowerPlatformConnectorHelper
    {
        /// <summary>
        /// Builds a client from an <see cref="OpenApiDocument"/>.
        /// </summary>
        /// <returns>A tuple containing the <see cref="DelegatingHandler"/> and the base <see cref="Uri"/>, this Uri must be set as a BaseAddress in HttpClient.</returns>
        public static (DelegatingHandler, Uri) FromDocument(
            OpenApiDocument document,
            string environmentId,
            string connectionId,
            BearerAuthTokenProvider tokenProvider,
            HttpMessageHandler httpMessageHandler,
            string userAgent = null,
            string sessionId = null)
        {
            var handler = new PowerPlatformConnectorClient2(
                document,
                environmentId,
                connectionId,
                tokenProvider,
                httpMessageHandler,
                userAgent,
                sessionId);

            return (handler, handler.BaseUri);
        }

        /// <summary>
        /// Builds a client from a base URL provided as a string.
        /// </summary>
        /// <returns>A tuple containing the <see cref="DelegatingHandler"/> and the base <see cref="Uri"/>, this Uri must be set as a BaseAddress in HttpClient.</returns>
        public static (DelegatingHandler, Uri) FromBaseUrl(
            string baseUrl,
            string environmentId,
            string connectionId,
            BearerAuthTokenProvider tokenProvider,
            HttpMessageHandler httpMessageHandler,
            string userAgent = null,
            string sessionId = null)
        {
            var handler = new PowerPlatformConnectorClient2(
                baseUrl,
                environmentId,
                connectionId,
                tokenProvider,
                httpMessageHandler,
                userAgent,
                sessionId);

            return (handler, handler.BaseUri);
        }

        /// <summary>
        /// Builds a client from a base URL provided as a <see cref="System.Uri"/>.
        /// </summary>
        /// <returns>A tuple containing the <see cref="DelegatingHandler"/> and the base <see cref="Uri"/>, this Uri must be set as a BaseAddress in HttpClient.</returns>
        public static (DelegatingHandler, Uri) FromUri(
            System.Uri baseUrl,
            string environmentId,
            string connectionId,
            BearerAuthTokenProvider tokenProvider,
            HttpMessageHandler httpMessageHandler,
            string userAgent = null)
        {
            var handler = new PowerPlatformConnectorClient2(
                baseUrl,
                environmentId,
                connectionId,
                tokenProvider,
                httpMessageHandler,
                userAgent);

            return (handler, handler.BaseUri);
        }
    }
}
