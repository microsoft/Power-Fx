// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Net.Http;
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
        public static DelegatingHandler FromDocument(
            OpenApiDocument document,
            string environmentId,
            string connectionId,
            AuthTokenProvider tokenProvider,
            string userAgent,
            HttpMessageHandler httpMessageHandler)
        {
            return new PowerPlatformConnectorClient2(
                document,
                environmentId,
                connectionId,
                tokenProvider,
                userAgent,
                httpMessageHandler);
        }

        /// <summary>
        /// Builds a client from a base URL provided as a string.
        /// </summary>
        public static DelegatingHandler FromBaseUrl(
            string baseUrl,
            string environmentId,
            string connectionId,
            AuthTokenProvider tokenProvider,
            string userAgent,
            HttpMessageHandler httpMessageHandler = null)
        {
            return new PowerPlatformConnectorClient2(
                baseUrl,
                environmentId,
                connectionId,
                tokenProvider,
                userAgent,
                httpMessageHandler);
        }

        /// <summary>
        /// Builds a client from a base URL provided as a <see cref="System.Uri"/>.
        /// </summary>
        public static DelegatingHandler FromUri(
            System.Uri baseUrl,
            string environmentId,
            string connectionId,
            AuthTokenProvider tokenProvider,
            string userAgent,
            HttpMessageHandler httpMessageHandler)
        {
            return new PowerPlatformConnectorClient2(
                baseUrl,
                environmentId,
                connectionId,
                tokenProvider,
                userAgent,
                httpMessageHandler);
        }
    }
}
