// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Text;

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
    }
}
