// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Syntax;

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

        /// <summary>
        /// Checks if all names within an aggregate DType exists.
        /// </summary>
        /// <param name="argType">Record DType.</param>
        /// <param name="dataSourceType">Table DType.</param>
        /// <param name="arg">Arg node.</param>
        /// <param name="errors">Error object reference.</param>
        /// <param name="supportsParamCoercion">Does the caller function support coercion.</param>
        /// <param name="usePowerFxV1CompatibilityRules">Use PFx v1 compatibility rules if enabled (less
        /// permissive Accepts relationships).</param>
        /// <returns></returns>
        internal static bool CheckAggregateNames(this DType argType, DType dataSourceType, TexlNode arg, IErrorContainer errors, bool supportsParamCoercion = false, bool usePowerFxV1CompatibilityRules = false)
        {
            bool isValid = true;

            foreach (var typedName in argType.GetNames(DPath.Root))
            {
                DName name = typedName.Name;
                DType type = typedName.Type;

                if (!dataSourceType.TryGetType(name, out DType dsNameType))
                {
                    dataSourceType.ReportNonExistingName(FieldNameKind.Display, errors, typedName.Name, arg);
                    isValid = false;
                    continue;
                }

                // For patching entities, we expand the type and drop entities and attachments for the purpose of comparison.
                if (dsNameType.Kind == DKind.DataEntity && type.Kind != DKind.DataEntity)
                {
                    if (dsNameType.TryGetExpandedEntityTypeWithoutDataSourceSpecificColumns(out var expandedType))
                    {
                        dsNameType = expandedType;
                    }
                }

                if (!dsNameType.Accepts(type, out var schemaDifference, out var schemaDifferenceType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) &&
                        (!supportsParamCoercion || !type.CoercesTo(dsNameType, out var coercionIsSafe, aggregateCoercion: false, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) || !coercionIsSafe))
                {
                    if (dsNameType.Kind == type.Kind)
                    {
                        errors.Errors(arg, type, schemaDifference, schemaDifferenceType);
                    }
                    else
                    {
                        errors.EnsureError(DocumentErrorSeverity.Severe, arg, TexlStrings.ErrTypeError_Arg_Expected_Found, name, dsNameType.GetKindString(), type.GetKindString());
                    }

                    isValid = false;
                }
            }

            return isValid;
        }
    }
}
