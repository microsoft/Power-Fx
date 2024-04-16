// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Web;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    /// <summary>
    /// A helper class to help with creating and writing notifications.
    /// </summary>
    internal static class NotificationHelper
    {
        /// <summary>
        /// Create PublishTokens notification and writes it to the builder. 
        /// Note: This is a legacy notification, and should be replaced by semantic tokens in all hosts.
        /// </summary>
        /// <param name="builder">Language Server Output Builder.</param>
        /// <param name="documentUri">Document Uri.</param>
        /// <param name="result">Check Result.</param>
        public static void WriteTokensNotification(this LanguageServerOutputBuilder builder, string documentUri, CheckResult result)
        {
            var notification = CreateTokensNotification(documentUri, result);
            if (notification != null)
            {
                builder.AddNotification(CustomProtocolNames.PublishTokens, notification);
            }
        }

        /// <summary>
        /// Create PublishTokens notification. 
        /// Note: This is a legacy notification, and should be replaced by semantic tokens in all hosts.
        /// </summary>
        /// <param name="documentUri">Document Uri.</param>
        /// <param name="result">Check Result.</param>
        /// <returns>PublishTokensParams.</returns>
        public static PublishTokensParams CreateTokensNotification(string documentUri, CheckResult result)
        {
            var uri = new Uri(documentUri);
            var nameValueCollection = HttpUtility.ParseQueryString(uri.Query);
            if (!uint.TryParse(nameValueCollection.Get("getTokensFlags"), out var flags))
            {
                return null;
            }

            var tokens = result.GetTokens((GetTokensFlags)flags);
            if (tokens == null || tokens.Count == 0)
            {
                return null;
            }

            return new PublishTokensParams()
            {
                Uri = documentUri,
                Tokens = tokens
            };
        }

        /// <summary>
        /// Create PublishExpressionType notification and writes it to the builder.
        /// </summary>
        /// <param name="builder">Language Server Output Builder.</param>
        /// <param name="documentUri">Document Uri.</param>
        /// <param name="result">Check Result.</param>
        public static void WriteExpressionTypeNotification(this LanguageServerOutputBuilder builder, string documentUri, CheckResult result)
        {
            var notification = CreateExpressionTypeNotification(documentUri, result);
            if (notification != null)
            {
                builder.AddNotification(CustomProtocolNames.PublishExpressionType, notification);
            }
        }

        /// <summary>
        /// Create PublishExpressionType notification.
        /// </summary>
        /// <param name="documentUri">Document Uri.</param>
        /// <param name="result">Check Result.</param>
        /// <returns>Expression Type Notification.</returns>
        public static PublishExpressionTypeParams CreateExpressionTypeNotification(string documentUri, CheckResult result)
        {
            var uri = new Uri(documentUri);
            var nameValueCollection = HttpUtility.ParseQueryString(uri.Query);
            if (!bool.TryParse(nameValueCollection.Get("getExpressionType"), out var enabled) || !enabled)
            {
                return null;
            }

            return new PublishExpressionTypeParams()
            {
                Uri = documentUri,
                Type = result.ReturnType
            };
        }
    }
}
