// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    public class ResourceValidationTests : PowerFxTest
    {       
        [Fact]
        public void ResourceLoadsOnlyRequiredLocales()
        {
            // Get a string from En-Us and to De-DE ensure they're loaded
            Assert.NotNull(StringResources.Get("AboutIf", "en-US"));
            Assert.NotNull(StringResources.Get("ErrGeneralError", "de-DE"));

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            Assert.True(assemblies.Where(x => x.FullName.Contains("Culture=en-US")).Any());
            Assert.True(assemblies.Where(x => x.FullName.Contains("Culture=de-DE")).Any());
        }

        [Fact]
        public void TestResourceImportUsesCurrentUICulture()
        {
            var initialCulture = CultureInfo.CurrentUICulture;
            var enUsERContent = StringResources.GetErrorResource(TexlStrings.ErrBadToken);
            var enUsBasicContent = StringResources.Get("AboutAbs");

            CultureInfo.CurrentUICulture = CultureInfo.CreateSpecificCulture("fr-FR");

            var frERContent = StringResources.GetErrorResource(TexlStrings.ErrBadToken);
            var frBasicContent = StringResources.Get("AboutAbs");

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            Assert.True(assemblies.Where(x => x.FullName.Contains("Culture=fr-FR")).Any());

            // Strings are not the same as enUS
            // Not validating content directly, since it might change
            Assert.NotEqual(enUsBasicContent, frBasicContent);
            Assert.NotEqual(enUsERContent.GetSingleValue(ErrorResource.ShortMessageTag), frERContent.GetSingleValue(ErrorResource.ShortMessageTag));
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
