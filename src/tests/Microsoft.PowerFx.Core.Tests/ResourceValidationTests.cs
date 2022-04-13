// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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
            var loaded = string.Empty;
            var loadedCount = 0;

            void ResourceAssemblyLoadHandler(object sender, AssemblyLoadEventArgs args)
            {
                loaded = args.LoadedAssembly.FullName;
                loadedCount++;
            }

            var currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyLoad += ResourceAssemblyLoadHandler;

            // Fallback locale is loaded when no others are specified
            var generalError = StringResources.Get("ErrGeneralError");
            Assert.Contains("en-US", loaded);

            // Other locales force a new assembly load
            generalError = StringResources.Get("ErrGeneralError", "de-DE");
            Assert.Contains("de-DE", loaded);

            // No other assemblies were loaded
            Assert.Equal(2, loadedCount);
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
