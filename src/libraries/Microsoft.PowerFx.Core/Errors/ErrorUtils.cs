// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Text;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Errors
{
    internal class ErrorUtils
    {
        /// <summary>
        /// Format error messages replacing the placeholder to argument values.
        /// </summary>
        /// <param name="message">Error message to format.</param>
        /// <param name="locale">CultureInfo information.</param>
        /// <param name="args">Original error message args.</param>
        /// <returns></returns>
        internal static string FormatMessage(string message, CultureInfo locale, params object[] args)
        {
            if (message == null)
            {
                return null;
            }

            var sb = new StringBuilder();
            if (args != null && args.Length > 0)
            {
                try
                {
                    sb.AppendFormat(locale ?? CultureInfo.CurrentCulture, message, args);
                }
                catch (FormatException)
                {
                    // Just in case we let a poorly escaped format string (eg a column name with {}s in it) get this far
                    // we will degrade the quality of the error report, but keep running at least
                    sb.Append(message);
                }
            }
            else
            {
                sb.Append(message);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get basic localized error content for an ErrorResourceKey.
        /// </summary>
        /// <param name="errKey">Error key.</param>
        /// <param name="locale">CultureInfo information.</param>
        /// <param name="errorResource">ErrorResource in case other tag content is needed.</param>
        /// <returns></returns>
        internal static (string shortMessage, string longMessage) GetLocalizedErrorContent(ErrorResourceKey errKey, CultureInfo locale, out ErrorResource errorResource)
        {
            // We expect errKey to be the key for an error resource object within string resources.
            // We fall back to using a basic content string within string resources, for errors
            // that haven't yet been converted to an ErrorResource in the Resources.pares file.
            string shortMessage;
            string longMessage;

            if (StringResources.TryGetErrorResource(errKey, out errorResource, locale?.Name))
            {
                shortMessage = errorResource.GetSingleValue(ErrorResource.ShortMessageTag);
                Contracts.AssertValue(shortMessage);
                longMessage = errorResource.GetSingleValue(ErrorResource.LongMessageTag);
            }
            else
            {
                shortMessage = StringResources.Get(errKey.Key, locale?.Name);
                longMessage = null;
                errorResource = null;
            }

            return (shortMessage, longMessage);
        }
    }
}
