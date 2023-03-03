// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Functions
{
    internal static class Extensions
    {
        internal static bool IsValid(this DateTime dateTime, EvalVisitor runner)
        {
            return IsValid(dateTime, runner.TimeZoneInfo);
        }

        internal static bool IsValid(this DateTime dateTime, TimeZoneInfo tzi)
        {
            // If DateTime is UTC, the time is always valid
            if (dateTime.Kind == DateTimeKind.Utc)
            {
                return true;
            }

            // Check if the time exists in this time zone            
            // https://www.timeanddate.com/time/change/usa/seattle?year=2023
            // 12 Mar 2023 02:10:00 is invalid            
            if (tzi.IsInvalidTime(dateTime))
            {
                return false;
            }

            // ambiguous times (like 5 Nov 2023 01:10:00 is ambiguous in PST timezone) will be considered valid
            return true;
        }

        internal static bool CheckAggregateNames(this DType argType, DType dataSourceType, TexlNode arg, IErrorContainer errors, bool supportsParamCoercion = false)
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

                if (!type.Accepts(dsNameType, out var schemaDifference, out var schemaDifferenceType) &&
                    (!supportsParamCoercion || !type.CoercesTo(dsNameType, out var coercionIsSafe, aggregateCoercion: false) || !coercionIsSafe))
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
