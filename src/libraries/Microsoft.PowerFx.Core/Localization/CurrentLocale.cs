// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Localization
{
    internal static class CurrentLocaleInfo
    {
        public static string CurrentLocaleName { get; set; } = PowerFxConfig.GetCurrentCulture().Name;

        public static string CurrentUILanguageName { get; set; } = PowerFxConfig.GetCurrentCulture().Name;

        public static int CurrentLCID { get; set; } = PowerFxConfig.GetCurrentCulture().LCID;
    }
}