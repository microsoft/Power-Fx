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
            StringBuilder sb = new StringBuilder();
            GetDetailedExceptionMessageInternal(sb, string.Empty, ex, 0, maxDepth);
            return sb.ToString();
        }

        private static void GetDetailedExceptionMessageInternal(StringBuilder sb, string prefix, Exception ex, int depth, int maxDepth)
        {
            depth++;

            if (depth > maxDepth)
            {
                return;
            }            

            if (depth > 1)
            {
                AddSeparator(sb, depth);
            }            

            if (ex == null)
            {
                sb.Append("Exception is null");
                return;
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
                    GetDetailedExceptionMessageInternal(sb, $"Inner#{i++}", ie, depth, maxDepth);
                }

                return;
            }

            PropertyInfo pi = ex.GetType().GetProperty("InnerException");
            if (pi?.GetValue(ex) is Exception iex)
            {                
                GetDetailedExceptionMessageInternal(sb, "Inner", iex, depth, maxDepth);
            }           
        }

        private static void AddSeparator(StringBuilder sb, int depth)
        {
            sb.AppendLine();
            sb.Append(new string(' ', (depth - 1) * 3));
            sb.AppendLine("----------");
        }
    }
}
