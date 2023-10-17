﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.PowerFx.Core.Errors;
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
        public void FindExtraResources()
        {
            string r1 = StringResources.Get("SampleResource1", CultureInfo.InvariantCulture.Name);
            Assert.Null(r1);
            Assert.Throws<FileNotFoundException>(() => ErrorUtils.GetLocalizedErrorContent(new ErrorResourceKey("SampleResource1"), CultureInfo.InvariantCulture, out _));

            // Notice that the below line can only be called once in all tests of this class as it registers the string manager for the entire class (static)
            StringResources.ExternalStringResources = new PowerFxStringResources("Microsoft.PowerFx.Core.Tests.Properties.Resources", typeof(ResourceValidationTests).Assembly);

            string r2 = StringResources.Get("SampleResource1", CultureInfo.InvariantCulture.Name);
            Assert.NotNull(r2);
            Assert.Equal("This is only a sample resource", r2);

            (string shortMessage, string longMessage) = ErrorUtils.GetLocalizedErrorContent(new ErrorResourceKey("SampleResource1"), CultureInfo.InvariantCulture, out _);
            Assert.Equal("This is only a sample resource", shortMessage);

            ErrorResource er = StringResources.GetErrorResource(new ErrorResourceKey("SampleResource2"));
            Assert.NotNull(er);
            Assert.Equal("This is sample message #2 short", er.GetSingleValue(ErrorResource.ShortMessageTag));
            Assert.Equal("This is sample message #2 long version", er.GetSingleValue(ErrorResource.LongMessageTag));
            Assert.Equal("This is sample message #2 how to fix", er.GetSingleValue(ErrorResource.HowToFixTag));
            Assert.Equal("This is sample message #2 link", er.HelpLinks[0].DisplayText);

            // This is the correct way to get this resource here as it's really an ErrorResourceKey
            ErrorResource er2 = StringResources.GetErrorResource(TexlStrings.OpNotSupportedByColumnSuggestionMessage_OpNotSupportedByColumn);
            Assert.NotNull(er2);
            string str1 = er2.GetSingleValue(ErrorResource.ShortMessageTag);
            Assert.NotNull(str1);

            // Here we use some fallback logic as there is no "SuggestRemoteExecutionHint_OpNotSupportedByColumn" resource (this API will return the content of "ErrorResource_SuggestRemoteExecutionHint_OpNotSupportedByColumn_ShortMessage")
            string str2 = StringResources.Get(TexlStrings.OpNotSupportedByColumnSuggestionMessage_OpNotSupportedByColumn);
            Assert.NotNull(str2);

            Assert.Equal(str1, str2);
        }

        [Fact]
        public void TestResourceImportUsesCurrentUICulture()
        {
            // $$$ Don't use CurrentUICulture
            var initialCulture = CultureInfo.CurrentUICulture;
            var enUsERContent = StringResources.GetErrorResource(TexlStrings.ErrBadToken);
            var enUsBasicContent = StringResources.Get("AboutAbs");

            // $$$ Don't use CurrentUICulture
            CultureInfo.CurrentUICulture = CultureInfo.CreateSpecificCulture("fr-FR");

            var frERContent = StringResources.GetErrorResource(TexlStrings.ErrBadToken);
            var frBasicContent = StringResources.Get("AboutAbs");

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            Assert.True(assemblies.Where(x => x.FullName.Contains("Culture=fr-FR")).Any());

            // Strings are not the same as enUS
            // Not validating content directly, since it might change
            Assert.NotEqual(enUsBasicContent, frBasicContent);

            string usContent = enUsERContent.GetSingleValue(ErrorResource.ShortMessageTag);
            string frContent = frERContent.GetSingleValue(ErrorResource.ShortMessageTag);

            Assert.NotEqual(usContent, frContent);
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

        [Fact]
        public void InvariantCultureForResourceImportTest()
        {
            // $$$ Don't use CurrentUICulture
            CultureInfo.CurrentUICulture = CultureInfo.CreateSpecificCulture("en-US");
            var enUsERContent = StringResources.GetErrorResource(TexlStrings.ErrBadToken);
            var enUsBasicContent = StringResources.Get("AboutAbs");

            // $$$ Don't use CurrentUICulture
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            var invariantContent = StringResources.GetErrorResource(TexlStrings.ErrBadToken);
            var invariantBasicContent = StringResources.Get("AboutAbs");

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Assert.True(assemblies.Where(x => x.FullName.Contains("Culture=neutral")).Any());

            // Strings in invariantculture are the same as en-US culture
            // Not validating content directly, since it might change
            Assert.Equal(enUsBasicContent, invariantBasicContent);

            string usContent = enUsERContent.GetSingleValue(ErrorResource.ShortMessageTag);
            string invContent = invariantContent.GetSingleValue(ErrorResource.ShortMessageTag);

            Assert.Equal(usContent, invContent);
        }
    }
}
