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

        internal static InternalSetup Parse(string setupHandlerName)
        {
            var iSetup = new InternalSetup();

            if (string.IsNullOrWhiteSpace(setupHandlerName))
            {
                return iSetup;
            }

            var parts = setupHandlerName.Split(",").Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();

            foreach (var part in parts.ToArray())
            {
                if (Enum.TryParse<TexlParser.Flags>(part, out var flag))
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

            if (parts.Count > 1)
            {
                throw new ArgumentException("Too many setup handler names!");
            }

            iSetup.HandlerName = parts.FirstOrDefault();
            return iSetup;
        }
    }
}
