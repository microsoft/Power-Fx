// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;

namespace Microsoft.PowerFx.Core.Utils
{
    internal static class Extensions
    {
        public static string GetDetailedExceptionMessage(this Exception ex, int maxDepth = 10)
        {
            return GetDetailedExceptionMessageInternal(string.Empty, ex, 0, maxDepth);
        }

        private static string GetDetailedExceptionMessageInternal(string prefix, Exception ex, int depth, int maxDepth)
        {
            depth++;

            if (depth > maxDepth)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

            if (depth > 1)
            {
                AddSeparator(sb, depth);
            }            

            if (ex == null)
            {
                sb.Append("Exception is null");
                return sb.ToString();
            }

            sb.Append(new string(' ', (depth - 1) * 3));

            if (!string.IsNullOrEmpty(prefix))
            {
                sb.Append('[');
                sb.Append(prefix);
                sb.Append(']');
            }

            sb.Append("Exception ");
            sb.Append(ex.GetType().FullName);
            sb.Append(": Message='");
            sb.Append(ex.Message);
            sb.Append("', HResult=");
            sb.AppendFormat(CultureInfo.InvariantCulture, @"0x{0:X8}", ex.HResult);

            if (ex is WebException we)
            {
                sb.Append(", Status=");
                sb.Append((int)we.Status);
                sb.Append(" (");
                sb.Append(we.Status.ToString());
                sb.Append(')');                
            }

            sb.Append(", StackTrace='");
            sb.Append(ex.StackTrace);
            sb.Append('\'');

            if (ex is AggregateException ae)
            {
                int i = 0;
                foreach (Exception ie in ae.InnerExceptions)
                {                    
                    sb.Append(GetDetailedExceptionMessageInternal($"Inner#{i++}", ie, depth, maxDepth));
                }

                return sb.ToString();
            }

            PropertyInfo pi = ex.GetType().GetProperty("InnerException");
            if (pi?.GetValue(ex) is Exception iex)
            {                
                sb.Append(GetDetailedExceptionMessageInternal("Inner", iex, depth, maxDepth));
            }

            return sb.ToString();
        }

        private static void AddSeparator(StringBuilder sb, int depth)
        {
            sb.AppendLine();
            sb.Append(new string(' ', (depth - 1) * 3));
            sb.AppendLine("----------");
        }
    }
}
