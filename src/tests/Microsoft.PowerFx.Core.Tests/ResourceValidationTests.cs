// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    public class ResourceValidationTests
    {
        [Fact]
        public void ResourceLoadsOnlyRequiredLocales()
        {
            // Get a string from En-Us to ensure it's loaded
            Assert.NotNull(StringResources.Get("AboutIf", "en-US"));

            var loaded = string.Empty;
            var loadedCount = 0;

            void ResourceAssemblyLoadHandler(object sender, AssemblyLoadEventArgs args)
            {
                loaded = args.LoadedAssembly.FullName;
                loadedCount++;
            }

            try 
            {
                AppDomain.CurrentDomain.AssemblyLoad += ResourceAssemblyLoadHandler;

                // Fallback locale (en-US) is already loaded above
                var generalError = StringResources.Get("ErrGeneralError");
                Assert.Empty(loaded);

                // Other locales force a new assembly load
                generalError = StringResources.Get("ErrGeneralError", "de-DE");
                Assert.Contains("de-DE", loaded);

                // No other assemblies were loaded
                Assert.Equal(1, loadedCount);
            }
            finally 
            {
                AppDomain.CurrentDomain.AssemblyLoad -= ResourceAssemblyLoadHandler; 
            }
        }

        [Fact]
        public void TestErrorResourceImport()
        {
            var error = StringResources.GetErrorResource(TexlStrings.ErrIncompatibleTypesForEquality_Left_Right);

            // Verify that associated messages have been pulled in.
            Assert.True(error.GetSingleValue(ErrorResource.ShortMessageTag).Any());
            Assert.True(error.GetSingleValue(ErrorResource.LongMessageTag).Any());
            Assert.Equal(2, error.GetValues(ErrorResource.HowToFixTag).Count);
            Assert.Equal(2, error.HelpLinks.Count);
        }

        [Fact]
        public void AllBuiltinFunctionsHaveParameterDescriptions()
        {
            var texlFunctionsLibrary = BuiltinFunctionsCore.BuiltinFunctionsLibrary;
            var functions = texlFunctionsLibrary
                .Where(x => !x.FunctionCategoriesMask.HasFlag(FunctionCategories.REST));

            foreach (var function in functions)
            {
                if (function.MaxArity == 0)
                {
                    continue;
                }

                foreach (var paramName in function.GetParamNames())
                {
                    Assert.True(
                        function.TryGetParamDescription(paramName, out var descr),
                        "Missing parameter description. Please add the following to Resources: " + "About" + function.LocaleInvariantName + "_" + paramName);
                }
            }
        }
    }
}
