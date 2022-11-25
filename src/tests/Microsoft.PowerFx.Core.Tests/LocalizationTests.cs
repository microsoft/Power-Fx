// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Localization;
using Xunit;
using System.Reflection;
using System.Linq;

namespace Microsoft.PowerFx.Core.Tests
{
    public class LocalizationTests
    {
        public static List<string> locales = new List<string>()
        {
            "bg-BG",
            "ca-ES",
            "cs-CZ",
            "da-DK",
            "de-DE",
            "el-GR",
            "en-US",
            "es-ES",
            "et-EE",
            "eu-ES",
            "fi-FI",
            "fr-FR",
            "gl-ES",
            "hi-IN",
            "hr-HR",
            "hu-HU",
            "id-ID",
            "it-IT",
            "ja-JP",
            "kk-KZ",
            "ko-KR",
            "lt-LT",
            "lv-LV",
            "ms-MY",
            "nb-NO",
            "nl-NL",
            "pl-PL",
            "pt-BR",
            "pt-PT",
            "ro-RO",
            "ru-RU",
            "sk-SK",
            "sl-SI",
            "sr-Cyrl-RS",
            "sr-Latn-RS",
            "sv-SE",
            "th-TH",
            "tr-TR",
            "uk-UA",
            "vi-VN",
            "zh-CN",
            "zh-TW",
        };

        [Fact]
        public void CheckMissingResourceStrings()
        {
            var fields = typeof(TexlStrings).GetFields(BindingFlags.Public | BindingFlags.Static);
            var errors = new StringBuilder();

            foreach (var field in fields)
            {
                try
                {
                    _ = StringResources.Get(field.Name);

                    foreach (var locale in locales)
                    {
                        try
                        {
                            _ = StringResources.Get(field.Name, locale);
                        }
                        catch (Exception ex)
                        {
                            errors.AppendLine($"{field.Name}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.AppendLine($"{field.Name}: {ex.Message}");
                }                
            }            

            if (errors.Length > 0)
            {
                Assert.False(true, errors.ToString());
            }
        }        
    }
}
