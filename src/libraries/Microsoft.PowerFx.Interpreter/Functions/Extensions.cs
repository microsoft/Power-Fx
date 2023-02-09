// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

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
    }
}
