// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;

namespace Microsoft.PowerFx.Functions
{
    internal static class Extensions
    {
        internal static bool IsValid(this DateTime dateTime, EvalVisitor runner)
        {
            var tzi = runner.GetService<TimeZoneInfo>() ?? TimeZoneInfo.Local;

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

        internal static TexlFunctionSet ToTexlFunctionSet(this IEnumerable<TexlFunction> functions)
        {
            TexlFunctionSet tfs = new TexlFunctionSet();

            foreach (TexlFunction function in functions)
            {
                tfs.Add(function);
            }

            return tfs;
        }
    }
}
