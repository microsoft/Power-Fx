// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
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

        /// <summary>
        /// By default, we run the result back through the serializer and deserialize again to se if we get the same result.
        /// For some tests, for example reserved words, the deserialization will not work properly.
        /// </summary>
        internal bool SkipDeserializeComparison { get; set; }

        private static bool TryGetFeaturesProperty(string featureName, out PropertyInfo propertyInfo)
        {
            propertyInfo = typeof(Features).GetProperty(featureName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return propertyInfo?.CanWrite == true;
        }

        internal static InternalSetup Parse(string setupHandlerName, bool numberIsFloat = false)
        {
            var iSetup = new InternalSetup
            {
                // Default features
                Features = new Features
                {
                    TableSyntaxDoesntWrapRecords = true,
                    ConsistentOneColumnTableResult = true
                }
            };

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
                else if (string.Equals(part, "SkipDeserializeComparison", StringComparison.OrdinalIgnoreCase))
                {
                    iSetup.SkipDeserializeComparison = true;
                    parts.Remove(part);
                }
                else if (Enum.TryParse<TexlParser.Flags>(partName, out var flag))
                {
                    if (isDisable)
                    {
                        if (!iSetup.Flags.HasFlag(flag))
                        {
                            throw new InvalidOperationException($"Flag {partName} is already disabled");
                        }

                        iSetup.Flags &= ~flag;
                    }
                    else
                    {
                        if (iSetup.Flags.HasFlag(flag))
                        {
                            throw new InvalidOperationException($"Flag {partName} is already enabled");
                        }

                        iSetup.Flags |= flag;
                    }

                    parts.Remove(part);
                }
                else if (TryGetFeaturesProperty(partName, out var prop))
                {
                    if (isDisable)
                    {
                        if (!((bool)prop.GetValue(iSetup.Features)))
                        {
                            throw new InvalidOperationException($"Feature {partName} is already disabled");
                        }

                        prop.SetValue(iSetup.Features, false);
                    }
                    else
                    {
                        if ((bool)prop.GetValue(iSetup.Features))
                        {
                            throw new InvalidOperationException($"Feature {partName} is already enabled");
                        }

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

            if (parts.Count > 1)
            {
                throw new ArgumentException("Too many setup handler names!");
            }

            iSetup.HandlerName = parts.FirstOrDefault();
            return iSetup;
        }
    }
}
