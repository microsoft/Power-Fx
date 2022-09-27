// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx.Functions
{
    internal static class Extensions
    {
        internal static bool IsValid(this DateTime dateTime, EvalVisitor runner)
        {
            var tzi = runner.GetService<TimeZoneInfo>();

            // If TZI isn't specified, we cannot validate if the datetime is valid or ambiguous
            // If DateTime is UTC, the time is always valid
            if (tzi == null || dateTime.Kind == DateTimeKind.Utc)
            {
                return true;
            }

            // Check if the time exists in this time zone
            // Check if the time is ambiguous in this time zone
            // https://www.timeanddate.com/time/change/usa/seattle?year=2023
            // 12 Mar 2023 02:10:00 is invalid
            //  5 Nov 2023 01:10:00 is ambiguous
            if (tzi.IsInvalidTime(dateTime) || tzi.IsAmbiguousTime(dateTime))
            {
                return false;
            }            

            return true;
        }
    }
}
