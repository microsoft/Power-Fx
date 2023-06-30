﻿// Copyright (c) Microsoft Corporation.
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
        internal static readonly IResourceStringManager LocalStringResources = new PowerFxStringResources(new ThreadSafeResouceManager());

        public static ErrorResource GetErrorResource(ErrorResourceKey resourceKey, string locale = null)
        {
            Contracts.CheckValue(resourceKey.Key, nameof(resourceKey));
            Contracts.CheckValueOrNull(locale, nameof(locale));

            // As foreign languages can lag behind en-US while being localized, if we can't find it then always look in the en-US locale
            if (!resourceKey.ResourceManager.TryGetErrorResource(resourceKey, out var resourceValue, locale))
            {
                Debug.WriteLine(string.Format(CultureInfo.InvariantCulture, "ERROR error resource {0} not found", resourceKey));
                throw new System.IO.FileNotFoundException(resourceKey.Key);
            }

            return resourceValue;
        }

        public static string Get(ErrorResourceKey resourceKey, string locale = null)
        {
            return resourceKey.ResourceManager.Get(resourceKey.Key, locale);
        }

        public static string Get(string resourceKey, string locale = null)
        {
            return LocalStringResources.Get(resourceKey, locale);
        }

        public static bool TryGet(string resourceKey, out string resourceValue, string locale = null)
        {
            return LocalStringResources.TryGet(resourceKey, out resourceValue, locale);
        }

        public static bool TryGetErrorResource(ErrorResourceKey resourceKey, out ErrorResource resourceValue, string locale = null)
        {
            return resourceKey.ResourceManager.TryGetErrorResource(resourceKey, out resourceValue, locale);
        }
    }

    [ThreadSafeImmutable]
    internal class ThreadSafeResouceManager
    {
        // Get methods on this are threadsafe, as long as we never call ReleaseAll()
        // This wrapper ensures that we don't accidentally do that
        private readonly ResourceManager _resourceManager = new ResourceManager("Microsoft.PowerFx.Core.strings.PowerFxResources", typeof(StringResources).Assembly);

        public string GetLocaleResource(string resourceKey, string locale)
        {
            if (string.IsNullOrEmpty(locale))
            {
                // $$$ Don't use CurrentUICulture
                return _resourceManager.GetString(resourceKey, CultureInfo.CurrentUICulture);
            }

            return _resourceManager.GetString(resourceKey, CultureInfo.CreateSpecificCulture(locale));
        }
    }
    
    internal class PowerFxStringResources : IResourceStringManager
    {
        private readonly ThreadSafeResouceManager _resourceManager;

        [ThreadSafeProtectedByLockAttribute("_errorResources")]
        private static readonly Dictionary<string, Dictionary<string, ErrorResource>> _errorResources = new Dictionary<string, Dictionary<string, ErrorResource>>(StringComparer.OrdinalIgnoreCase);

        internal PowerFxStringResources(ThreadSafeResouceManager resourceManager)
        {
            _resourceManager = resourceManager;
        }

        bool IResourceStringManager.TryGet(string resourceKey, out string resourceValue, string locale)
        {
            Contracts.CheckValue(resourceKey, nameof(resourceKey));
            Contracts.CheckValueOrNull(locale, nameof(locale));

            resourceValue = _resourceManager.GetLocaleResource(resourceKey, locale);

            return resourceValue != null;
        }

        public string Get(string resourceKey, string locale = null)
        {
            Contracts.CheckValue(resourceKey, nameof(resourceKey));
            Contracts.CheckValueOrNull(locale, nameof(locale));

            if (!((IResourceStringManager)this).TryGet(resourceKey, out var resourceValue, locale))
            {
                // Prior to ErrorResources, error messages were fetched like other string resources.
                // The resource associated with the key corresponds to the ShortMessage of the new
                // ErrorResource objects. For backwards compatibility with tests/telemetry that fetched
                // the error message manually (as opposed to going through the DocError class), we check
                // if there is an error resource associated with this key if we did not find it normally.
                if (((IResourceStringManager)this).TryGetErrorResource(new ErrorResourceKey(resourceKey, this), out var potentialErrorResource, locale))
                {
                    return potentialErrorResource.GetSingleValue(ErrorResource.ShortMessageTag);
                }

                Debug.WriteLine(string.Format(CultureInfo.InvariantCulture, "ERROR resource string {0} not found", resourceKey));                               
                throw new System.IO.FileNotFoundException(resourceKey);                
            }

            return resourceValue;
        }

        bool IResourceStringManager.TryGetErrorResource(ErrorResourceKey resourceKey, out ErrorResource resourceValue, string locale)
        {
            Contracts.CheckValue(resourceKey.Key, nameof(resourceKey));
            Contracts.CheckValueOrNull(locale, nameof(locale));

            if (string.IsNullOrEmpty(locale))
            {
                // $$$ Don't use CurrentUICulture
                locale = CultureInfo.CurrentUICulture.Name;

                if (string.IsNullOrEmpty(locale))
                {
                    locale = "en-US";
                }

                Contracts.CheckNonEmpty(locale, "locale");
            }

            // Error resources are a bit odd and need to be reassembled from separate keys.
            // Check to see if we've already retrieved one for this locale/key combo before 
            // rebuilding the full error resource.
            lock (_errorResources)
            {
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
            }

            return false;
        }

        private bool TryRebuildErrorResource(string key, string locale, out ErrorResource errorResource)
        {
            var members = new Dictionary<string, Dictionary<int, string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var tag in ErrorResource.ErrorResourceTagToReswSuffix)
            {
                if (!ErrorResource.IsTagMultivalue(tag.Key))
                {
                    var member = _resourceManager.GetLocaleResource(ErrorResource.ReswErrorResourcePrefix + key + tag.Value, locale);
                    if (member != null)
                    {
                        // Single valued tag, we use index=0 here when inserting
                        members.Add(tag.Key, new Dictionary<int, string>() { { 0, member } });
                    }
                }
                else
                {
                    // Max 10 multivalue tags, although that's absurd. 
                    for (var i = 1; i < 11; ++i)
                    {
                        var resourceName = ErrorResource.ReswErrorResourcePrefix + key + tag.Value + "_" + i;
                        var member = _resourceManager.GetLocaleResource(resourceName, locale);
                        if (member == null)
                        {
                            break;
                        }

                        if (!members.TryGetValue(tag.Key, out var multiValueKeys))
                        {
                            multiValueKeys = new Dictionary<int, string>();
                            members.Add(tag.Key, multiValueKeys);
                        }

                        multiValueKeys.Add(i, member);

                        // Also handle the URL for link resources
                        if (tag.Key == ErrorResource.LinkTag)
                        {
                            // This must exist, and the AssertValue call will fail CI builds if the resource is incorrectly defined. 
                            var link = _resourceManager.GetLocaleResource(resourceName + "_" + ErrorResource.LinkTagUrlSuffix, locale);
                            Contracts.AssertValue(link);

                            if (!members.TryGetValue(ErrorResource.LinkTagUrlTag, out var linkKeys))
                            {
                                linkKeys = new Dictionary<int, string>();
                                members.Add(ErrorResource.LinkTagUrlTag, linkKeys);
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
