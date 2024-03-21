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
        /// <param name="features">Enabled feature set, primarily for Use PFx v1 compatibility rules if enabled (less
        /// permissive Accepts relationships).</param>
        /// <returns></returns>
        internal static bool CheckAggregateNames(this DType argType, DType dataSourceType, TexlNode arg, IErrorContainer errors, Features features, bool supportsParamCoercion = false)
        {
            bool isValid = true;
            var usePowerFxV1CompatibilityRules = features.PowerFxV1CompatibilityRules;
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

                // !!! This is a tactical fix for the case where we have a boolean option set and a boolean value.
                // PA allows this but we don't. We should remove this once we have a better way to handle this.
                if (dsNameType.IsOptionSet &&
                    DType.Boolean.Accepts(type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) &&
                    type.CoercesTo(dsNameType, out _, aggregateCoercion: false, isTopLevelCoercion: false, features))
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, arg, TexlStrings.ErrTypeError_Arg_Expected_Found, name, dsNameType.GetKindString(), type.GetKindString());
                    isValid = false;
                }

                if ((!dsNameType.Accepts(type, out var schemaDifference, out var schemaDifferenceType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) &&
                     !DType.Number.Accepts(type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) &&
                     !DType.Decimal.Accepts(type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules)) &&
                    (!supportsParamCoercion || !type.CoercesTo(dsNameType, out var coercionIsSafe, aggregateCoercion: false, isTopLevelCoercion: false, features) || !coercionIsSafe))
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

        /// <summary>
        /// Gets the literal value from a parse. This currently works only for string, number, guid, and boolean literals.
        /// </summary>
        /// <param name="checkResult">Parsed expression result.</param>
        /// <param name="value">Literal value, if available.</param>
        /// <returns></returns>
        public static bool TryGetAsLiteral(this CheckResult checkResult, out object value)
        {
            switch (checkResult.Parse.Root)
            {
                case StrLitNode strLitNode:
                    value = strLitNode.Value;
                    return true;
                case NumLitNode numLitNode:
                    value = numLitNode.Value.Value;
                    return true;
                case DecLitNode decLitNode:
                    value = decLitNode.Value.Value;
                    return true;
                case BoolLitNode booLitNode:
                    value = booLitNode.Value;
                    return true;
                case CallNode callNode:
                    if (callNode.IsCall("GUID"))
                    {
                        if (callNode.Args.Count == 1 && callNode.Args.ChildNodes[0] is StrLitNode strLitNode)
                        {
                            if (Guid.TryParse(strLitNode.Value, out Guid result))
                            {
                                value = result;
                                return true;
                            }
                        }
                    }

                    break;
            }

            value = null;
            return false;
        }

        public static bool IsCall(this CallNode node, string functionName)
        {
            return node.Head.Namespace.Length == 0 && node.Head.Name.Value == functionName;
        }
    }
}
