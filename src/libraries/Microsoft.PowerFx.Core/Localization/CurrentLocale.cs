// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;

namespace Microsoft.PowerFx.Core.Localization
{
    internal static class CurrentLocaleInfo
    {
        public static string CurrentLocaleName { get; set; } = CultureInfo.CurrentCulture.Name;

        public static string CurrentUILanguageName { get; set; } = CultureInfo.CurrentCulture.Name;

        public static int CurrentLCID { get; set; } = CultureInfo.CurrentCulture.LCID;
    }
}
