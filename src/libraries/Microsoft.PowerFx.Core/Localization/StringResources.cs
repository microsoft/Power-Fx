// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Localization
{
    internal static class StringResources
    {
        /// <summary>
        ///  This field is set once on startup by Canvas' Document Server, and allows access to Canvas-specific string keys
        ///  It is a legacy use, left over from when PowerFx was deeply embedded in Canvas, and ideally should be removed if possible.
        /// </summary>
        internal static IExternalStringResources ExternalStringResources { get; set; }

        // This is used to workaround a build-time issue when this class is loaded by reflection without all the resources initialized correctly. 
        // If the dependency on ExternalStringResources is removed, this can be as well
        public static bool ShouldThrowIfMissing { get; set; } = true;

        private const string FallbackLocale = "en-US";

        private static readonly ResourceManager _resourceManager = new ResourceManager("Microsoft.PowerFx.Core.strings.PowerFxResources", typeof(StringResources).Assembly);
        private static readonly Dictionary<string, Dictionary<string, ErrorResource>> _errorResources = new Dictionary<string, Dictionary<string, ErrorResource>>(StringComparer.OrdinalIgnoreCase);

        public static ErrorResource GetErrorResource(ErrorResourceKey resourceKey, string locale = null)
        {
            Contracts.CheckValue(resourceKey.Key, nameof(resourceKey));
            Contracts.CheckValueOrNull(locale, nameof(locale));

            // As foreign languages can lag behind en-US while being localized, if we can't find it then always look in the en-US locale
            if (!TryGetErrorResource(resourceKey, out var resourceValue, locale))
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
            Contracts.CheckValue(resourceKey, nameof(resourceKey));
            Contracts.CheckValueOrNull(locale, nameof(locale));

            if (!TryGet(resourceKey, out var resourceValue, locale))
            {
                // Prior to ErrorResources, error messages were fetched like other string resources.
                // The resource associated with the key corresponds to the ShortMessage of the new
                // ErrorResource objects. For backwards compatibility with tests/telemetry that fetched
                // the error message manually (as opposed to going through the DocError class), we check
                // if there is an error resource associated with this key if we did not find it normally.
                if (TryGetErrorResource(new ErrorResourceKey(resourceKey), out var potentialErrorResource, locale))
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

        public static bool TryGet(string resourceKey, out string resourceValue, string locale = null)
        {
            Contracts.CheckValue(resourceKey, nameof(resourceKey));
            Contracts.CheckValueOrNull(locale, nameof(locale));

            resourceValue = GetLocaleResource(resourceKey, locale);

            return resourceValue != null ? true : (ExternalStringResources?.TryGet(resourceKey, out resourceValue, locale) ?? false);
        }

        private static string GetLocaleResource(string resourceKey, string locale)
        {
            if (string.IsNullOrEmpty(locale))
            {
                return _resourceManager.GetString(resourceKey);
            }

            return _resourceManager.GetString(resourceKey, CultureInfo.CreateSpecificCulture(locale));
        }

        public static bool TryGetErrorResource(ErrorResourceKey resourceKey, out ErrorResource resourceValue, string locale = null)
        {
            Contracts.CheckValue(resourceKey.Key, nameof(resourceKey));
            Contracts.CheckValueOrNull(locale, nameof(locale));

            if (string.IsNullOrEmpty(locale))
            {
                locale = FallbackLocale;
            }

            // Error resources are a bit odd and need to be reassembled from separate keys. Check to see if we've already retrieved one for this locale/key combo first. 
            if (_errorResources.TryGetValue(locale, out var localizedErrorResources))
            {
                if (localizedErrorResources.TryGetValue(resourceKey.Key, out resourceValue))
                {
                    return true;
                }
            }
            else
            {
                localizedErrorResources = new Dictionary<string, ErrorResource>(StringComparer.OrdinalIgnoreCase);
                _errorResources.Add(locale, localizedErrorResources);
            }

            if (TryRebuildErrorResource(resourceKey.Key, locale, out resourceValue))
            {
                localizedErrorResources.Add(resourceKey.Key, resourceValue);
                return true;
            }

            if (ExternalStringResources != null && ExternalStringResources.TryGetErrorResource(resourceKey, out resourceValue, locale))
            {
                localizedErrorResources.Add(resourceKey.Key, resourceValue);
                return true;
            }

            return false;
        }

        private static bool TryRebuildErrorResource(string key, string locale, out ErrorResource errorResource)
        {
            var members = new Dictionary<string, Dictionary<int, string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var tag in ErrorResource.ErrorResourceTagToReswSuffix)
            {
                if (!ErrorResource.IsTagMultivalue(tag.Key))
                {
                    var member = GetLocaleResource(ErrorResource.ReswErrorResourcePrefix + key + tag.Value, locale);
                    if (member != null)
                    {
                        // Single valued tag, we use index=0 here when inserting
                        members.Add(tag.Key, new Dictionary<int, string>() { { 0, member } });
                    }
                }
                else
                {                    
                    // Max 10 multivalue tags, although that's absurd. 
                    for (var i = 0; i < 10; ++i)
                    {
                        var resourceName = ErrorResource.ReswErrorResourcePrefix + key + tag.Value + "_" + i;
                        var member = GetLocaleResource(resourceName, locale);
                        if (member == null)
                        {
                            break;
                        }

                        if (!members.TryGetValue(tag.Key, out var multiValueKeys))
                        {
                            multiValueKeys = new Dictionary<int, string>();
                        }

                        multiValueKeys.Add(i, member);

                        // Also handle the URL for link resources
                        if (tag.Key == ErrorResource.LinkTag)
                        {
                            // This must exist, and the AssertValue call will fail CI builds if the resource is incorrectly defined. 
                            var link = GetLocaleResource(resourceName + "_" + ErrorResource.LinkTagUrlTag, locale);
                            Contracts.AssertValue(link);

                            if (!members.TryGetValue(ErrorResource.LinkTagUrlTag, out var linkKeys))
                            {
                                linkKeys = new Dictionary<int, string>();
                            }

                            linkKeys.Add(i, member);
                        }
                    }
                }
            }

            if (members.Any())
            {
                errorResource = ErrorResource.Reassemble(members);
                return true;
            }
            else
            {
                errorResource = null;
                return false;
            }
        }
    }
}
