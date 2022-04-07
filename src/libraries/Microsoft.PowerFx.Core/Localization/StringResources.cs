// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Localization
{
    internal static class StringResources
    {
        internal enum ResourceFormat
        {
            Resw,
            Pares
        }

        /// <summary>
        ///  This field is set once on startup by Canvas' Document Server, and allows access to Canvas-specific string keys
        ///  It is a legacy use, left over from when PowerFx was deeply embedded in Canvas, and ideally should be removed if possible.
        /// </summary>
        internal static IExternalStringResources ExternalStringResources { get; set; }

        // This is used to workaround a build-time issue when this class is loaded by reflection without all the resources initialized correctly. 
        // If the dependency on ExternalStringResources is removed, this can be as well
        public static bool ShouldThrowIfMissing { get; set; } = true;

        private const string FallbackLocale = "en-US";

        public static ErrorResource GetErrorResource(ErrorResourceKey resourceKey, string locale = null)
        {
            Contracts.CheckValue(resourceKey.Key, "action");
            Contracts.CheckValueOrNull(locale, "locale");

            // As foreign languages can lag behind en-US while being localized, if we can't find it then always look in the en-US locale
            if (!TryGetErrorResource(resourceKey, out var resourceValue, locale) && !TryGetErrorResource(resourceKey, out resourceValue, FallbackLocale))
            {
                Debug.WriteLine(string.Format("ERROR error resource {0} not found", resourceKey));
                if (ShouldThrowIfMissing)
                {
                    throw new System.IO.FileNotFoundException(resourceKey.Key);
                }
            }

            return resourceValue;
        }

        public static string Get(ErrorResourceKey resourceKey, string locale = null)
        {
            return Get(resourceKey.Key, locale);
        }

        public static string Get(string resourceKey, string locale = null)
        {
            Contracts.CheckValue(resourceKey, "action");
            Contracts.CheckValueOrNull(locale, "locale");

            // As foreign languages can lag behind en-US while being localized, if we can't find it then always look in the en-US locale
            if (!TryGet(resourceKey, out var resourceValue, locale) && !TryGet(resourceKey, out resourceValue, FallbackLocale))
            {
                // Prior to ErrorResources, error messages were fetched like other string resources.
                // The resource associated with the key corresponds to the ShortMessage of the new
                // ErrorResource objects. For backwards compatibility with tests/telemetry that fetched
                // the error message manually (as opposed to going through the DocError class), we check
                // if there is an error resource associated with this key if we did not find it normally.
                if (TryGetErrorResource(new ErrorResourceKey(resourceKey), out var potentialErrorResource, locale) || TryGetErrorResource(new ErrorResourceKey(resourceKey), out potentialErrorResource, FallbackLocale))
                {
                    return potentialErrorResource.GetSingleValue(ErrorResource.ShortMessageTag);
                }

                Debug.WriteLine(string.Format("ERROR resource string {0} not found", resourceKey));
                if (ShouldThrowIfMissing)
                {
                    throw new System.IO.FileNotFoundException(resourceKey);
                }
            }

            return resourceValue;
        }

        // One resource dictionary per locale
        private static readonly Dictionary<string, Dictionary<string, string>> Strings = new Dictionary<string, Dictionary<string, string>>();
        private static readonly Dictionary<string, Dictionary<string, ErrorResource>> ErrorResources = new Dictionary<string, Dictionary<string, ErrorResource>>();
        private static readonly object DictionaryLock = new object();

        private class TypeFromThisAssembly
        {
        }

        private static readonly string ResourceNamePrefix = "Microsoft.PowerFx.Core.Strings.";
        private static readonly string ResourceFileName = "PowerFxResources.resw";

        public static bool TryGetErrorResource(ErrorResourceKey resourceKey, out ErrorResource resourceValue, string locale = null)
        {
            Contracts.CheckValue(resourceKey.Key, "action");
            Contracts.CheckValueOrNull(locale, "locale");

            if (locale == null)
            {
                locale = CurrentLocaleInfo.CurrentUILanguageName;

                // If the locale is not set here, return false immedately and go to the "en-us" fallback
                if (string.IsNullOrEmpty(locale))
                {
                    resourceValue = default;
                    return false;
                }
            }

            if (!ErrorResources.TryGetValue(locale, out var errorResources))
            {
                lock (DictionaryLock)
                {
                    LoadFromResource(locale, ResourceNamePrefix, typeof(TypeFromThisAssembly), ResourceFileName, ResourceFormat.Resw, out var strings, out errorResources);
                    Strings[locale] = strings;
                    ErrorResources[locale] = errorResources;
                }
            }

            return errorResources.TryGetValue(resourceKey.Key, out resourceValue) || (ExternalStringResources?.TryGetErrorResource(resourceKey, out resourceValue, locale) ?? false);
        }

        public static bool TryGet(string resourceKey, out string resourceValue, string locale = null)
        {
            Contracts.CheckValue(resourceKey, "action");
            Contracts.CheckValueOrNull(locale, "locale");

            if (locale == null)
            {
                locale = CurrentLocaleInfo.CurrentUILanguageName;

                // If the locale is not set here, return false immedately and go to the "en-us" fallback
                if (string.IsNullOrEmpty(locale))
                {
                    resourceValue = default;
                    return false;
                }
            }

            if (!Strings.TryGetValue(locale, out var strings))
            {
                lock (DictionaryLock)
                {
                    LoadFromResource(locale, ResourceNamePrefix, typeof(TypeFromThisAssembly), ResourceFileName, ResourceFormat.Resw, out strings, out var errorResources);
                    Strings[locale] = strings;
                    ErrorResources[locale] = errorResources;
                }
            }

            return strings.TryGetValue(resourceKey, out resourceValue) || (ExternalStringResources?.TryGet(resourceKey, out resourceValue, locale) ?? false);
        }

        internal static void LoadFromResource(string locale, string assemblyPrefix, Type typeFromAssembly, string resourceFileName, ResourceFormat resourceFormat, out Dictionary<string, string> strings, out Dictionary<string, ErrorResource> errorResources)
        {
            var assembly = typeFromAssembly.Assembly;

            // This is being done because the filename of the manifest is case sensitive e.g. given zh-CN it was returning English
            if (locale.Equals("zh-CN"))
            {
                locale = "zh-cn";
            }
            else if (locale.Equals("zh-TW"))
            {
                locale = "zh-tw";
            }
            else if (locale.Equals("ko-KR"))
            {
                locale = "ko-kr";
            }

            using (var res = assembly.GetManifestResourceStream(assemblyPrefix + locale.Replace("-", "_") + "." + resourceFileName))
            {
                if (res == null)
                {
                    if (locale == FallbackLocale)
                    {
                        throw new InvalidProgramException(string.Format("[StringResources] Resources not found for locale '{0}' and failed to find fallback", locale));
                    }

                    // Load the default ones (recursive, but not infinite due to check above)
                    LoadFromResource(FallbackLocale, assemblyPrefix, typeFromAssembly, resourceFileName, resourceFormat, out strings, out errorResources);
                }
                else
                {
                    var loadedStrings = XDocument.Load(res).Descendants(XName.Get("data"));
                    strings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    errorResources = new Dictionary<string, ErrorResource>(StringComparer.OrdinalIgnoreCase);

                    if (resourceFormat == ResourceFormat.Pares)
                    {
                        foreach (var item in loadedStrings)
                        {
                            if (item.TryGetNonEmptyAttributeValue("type", out var type) && type == ErrorResource.XmlType)
                            {
                                errorResources[item.Attribute("name").Value] = ErrorResource.Parse(item);
                            }
                            else
                            {
                                strings[item.Attribute("name").Value] = item.Element("value").Value;
                            }
                        }
                    }
                    else if (resourceFormat == ResourceFormat.Resw)
                    {
                        var separatedResourceKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var item in loadedStrings)
                        {
                            var itemName = item.Attribute("name").Value;
                            if (itemName.StartsWith(ErrorResource.ReswErrorResourcePrefix, StringComparison.OrdinalIgnoreCase))
                            {
                                separatedResourceKeys[itemName] = item.Element("value").Value;
                            }
                            else
                            {
                                strings[itemName] = item.Element("value").Value;
                            }
                        }

                        errorResources = PostProcessErrorResources(separatedResourceKeys);
                    }
                    else
                    {
                        Contracts.Assert(false, "Unknown Resource Format");
                    }
                }
            }
        }

        private static bool TryGetMultiValueSuffix(string resourceKey, string baseSuffix, out string suffix, out int index)
        {
            var pattern = new Regex(baseSuffix + "_([0-9]*)", RegexOptions.IgnoreCase);
            var match = pattern.Match(resourceKey);
            if (match.Success)
            {
                suffix = match.Value;
                index = int.Parse(match.Groups[1].Value);
                return true;
            }

            suffix = null;
            index = 0;
            return false;
        }

        private static void UpdateErrorResource(string resourceName, string resourceValue, string tag, int index, Dictionary<string, Dictionary<string, Dictionary<int, string>>> errorResources)
        {
            Contracts.AssertValue(errorResources);
            Contracts.AssertNonEmpty(resourceName);
            Contracts.AssertNonEmpty(resourceValue);

            if (errorResources.TryGetValue(resourceName, out var tagToValuesDict))
            {
                if (tagToValuesDict.TryGetValue(tag, out var tagNumberToValuesDict))
                {
                    tagNumberToValuesDict.Add(index, resourceValue);
                }
                else
                {
                    tagNumberToValuesDict = new Dictionary<int, string>
                    {
                        { index, resourceValue }
                    };
                    tagToValuesDict.Add(tag, tagNumberToValuesDict);
                }
            }
            else
            {
                tagToValuesDict = new Dictionary<string, Dictionary<int, string>>(StringComparer.OrdinalIgnoreCase);
                var tagNumberToValuesDict = new Dictionary<int, string>
                {
                    { index, resourceValue }
                };
                tagToValuesDict.Add(tag, tagNumberToValuesDict);
                errorResources.Add(resourceName, tagToValuesDict);
            }
        }

        private static Dictionary<string, ErrorResource> PostProcessErrorResources(Dictionary<string, string> separateResourceKeys)
        {
            // ErrorResource name -> ErrorResourceTag -> tag number -> value
            var errorResources = new Dictionary<string, Dictionary<string, Dictionary<int, string>>>(StringComparer.OrdinalIgnoreCase);

            foreach (var resource in separateResourceKeys)
            {
                if (!resource.Key.StartsWith(ErrorResource.ReswErrorResourcePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Skip URLs, we'll handle that paired with the link tag
                if (resource.Key.EndsWith(ErrorResource.LinkTagUrlTag, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (var tag in ErrorResource.ErrorResourceTagToReswSuffix)
                {
                    if (!ErrorResource.IsTagMultivalue(tag.Key))
                    {
                        if (!resource.Key.EndsWith(tag.Value, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        // Single valued tag, we use index=0 here when inserting
                        var resourceName = resource.Key.Substring(ErrorResource.ReswErrorResourcePrefix.Length, resource.Key.Length - (ErrorResource.ReswErrorResourcePrefix.Length + tag.Value.Length));
                        UpdateErrorResource(resourceName, resource.Value, tag.Key, 0, errorResources);
                        break;
                    }
                    else
                    {
                        if (!TryGetMultiValueSuffix(resource.Key, tag.Value, out var suffix, out var index))
                        {
                            continue;
                        }

                        var resourceName = resource.Key.Substring(ErrorResource.ReswErrorResourcePrefix.Length, resource.Key.Length - (ErrorResource.ReswErrorResourcePrefix.Length + suffix.Length));
                        UpdateErrorResource(resourceName, resource.Value, tag.Key, index, errorResources);

                        // Also handle the URL for link resources
                        if (tag.Key == ErrorResource.LinkTag)
                        {
                            // This must exist, and the .verify call will fail CI builds if the resource is incorrectly defined. 
                            separateResourceKeys.TryGetValue(resource.Key + "_url", out var urlValue).Verify();
                            UpdateErrorResource(resourceName, urlValue, ErrorResource.LinkTagUrlTag, index, errorResources);
                        }

                        break;
                    }
                }
            }

            return errorResources.ToDictionary(kvp => kvp.Key, kvp => ErrorResource.Reassemble(kvp.Value));
        }
    }
}
