// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.PowerFx.Core.Parser;

namespace Microsoft.PowerFx.Core.Tests
{
    internal class InternalSetup
    {
        internal string HandlerName { get; set; }

        internal TexlParser.Flags Flags { get; set; }

        internal Features Features { get; set; }

        internal TimeZoneInfo TimeZoneInfo { get; set; }

        /// <summary>
        /// By default, we run expressions with a memory governor to enforce a limited amount of memory. 
        /// When true, disable memory checks and allow expression to use as much memory as it needs. 
        /// </summary>
        internal bool DisableMemoryChecks { get; set; }

        internal static InternalSetup Parse(string setupHandlerName, bool numberIsFloat = false)
        {
            var iSetup = new InternalSetup();

            if (string.IsNullOrWhiteSpace(setupHandlerName))
            {
                return iSetup;
            }

            var parts = setupHandlerName.Split(",").Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();

            foreach (var part in parts.ToArray())
            {
                // negative flags are caught when adding the tests
                if (part.StartsWith("!"))
                {
                    parts.Remove(part);
                }
                else if (string.Equals(part, "DisableMemChecks", StringComparison.OrdinalIgnoreCase))
                {
                    iSetup.DisableMemoryChecks = true;
                    parts.Remove(part);
                }
                else if (Enum.TryParse<TexlParser.Flags>(part, out var flag))
                {
                    iSetup.Flags |= flag;
                    parts.Remove(part);
                }
                else if (Enum.TryParse<Features>(part, out var f))
                {
                    iSetup.Features |= f;
                    parts.Remove(part);
                }
                else if (part.StartsWith("TimeZoneInfo", StringComparison.OrdinalIgnoreCase))
                {
                    var m = new Regex(@"TimeZoneInfo\(""(?<tz>[^)]+)""\)", RegexOptions.IgnoreCase).Match(part);

                    if (m.Success)
                    {
                        var tz = m.Groups["tz"].Value;

                        // This call will throw if the Id in invalid
                        iSetup.TimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(tz);
                        parts.Remove(part);
                    }
                    else
                    {
                        throw new ArgumentException("Invalid TimeZoneInfo setup!");
                    }
                }
            }

            if (numberIsFloat)
            {
                iSetup.Flags |= TexlParser.Flags.NumberIsFloat;
            }

            if (parts.Count > 5)
            {
                throw new ArgumentException("Too many setup handler names!");
            }

            iSetup.HandlerName = parts.FirstOrDefault();
            return iSetup;
        }
    }
}
