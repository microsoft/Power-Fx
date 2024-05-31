// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.PowerFx.Core.Parser;

namespace Microsoft.PowerFx.Core.Tests
{
    internal class InternalSetup
    {
        internal List<string> HandlerNames { get; set; }

        internal TexlParser.Flags Flags { get; set; }

        internal Features Features { get; set; }

        internal TimeZoneInfo TimeZoneInfo { get; set; }

        /// <summary>
        /// By default, we run expressions with a memory governor to enforce a limited amount of memory. 
        /// When true, disable memory checks and allow expression to use as much memory as it needs. 
        /// </summary>
        internal bool DisableMemoryChecks { get; set; }

        private static bool TryGetFeaturesProperty(string featureName, out PropertyInfo propertyInfo)
        {
            propertyInfo = typeof(Features).GetProperty(featureName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return propertyInfo?.CanWrite == true;
        }

        internal static InternalSetup Parse(string setupHandlerName, bool numberIsFloat = false)
        {
            return Parse(setupHandlerName, new Features(), numberIsFloat);
        }

        internal static InternalSetup Parse(string setupHandlerName, Features features, bool numberIsFloat = false)
        {
            var iSetup = new InternalSetup { Features = features };

            if (numberIsFloat)
            {
                iSetup.Flags |= TexlParser.Flags.NumberIsFloat;
            }

            if (string.IsNullOrWhiteSpace(setupHandlerName))
            {
                return iSetup;
            }

            var parts = setupHandlerName.Split(",").Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();

            foreach (var part in parts.ToArray())
            {
                var isDisable = false;
                var partName = part;
                if (part.StartsWith("disable:", StringComparison.OrdinalIgnoreCase))
                {
                    isDisable = true;
                    partName = part.Substring("disable:".Length);
                }

                if (string.Equals(part, "DisableMemChecks", StringComparison.OrdinalIgnoreCase))
                {
                    iSetup.DisableMemoryChecks = true;
                    parts.Remove(part);
                }
                else if (Enum.TryParse<TexlParser.Flags>(partName, out var flag))
                {
                    if (isDisable)
                    {
                        iSetup.Flags &= ~flag;
                    }
                    else
                    {
                        iSetup.Flags |= flag;
                    }

                    parts.Remove(part);
                }
                else if (TryGetFeaturesProperty(partName, out var prop))
                {
                    if (isDisable)
                    {
                        prop.SetValue(iSetup.Features, false);
                    }
                    else
                    {
                        prop.SetValue(iSetup.Features, true);
                    }

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

            iSetup.HandlerNames = parts;
            return iSetup;
        }
    }
}
