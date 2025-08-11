// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Annotations;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    // Do static analysis to look for potential threading issues. 
    public class ThreadingTests : PowerFxTest
    {
        [Fact]
        public void CheckFxCore()
        {
            var asm = typeof(Microsoft.PowerFx.Syntax.TexlNode).Assembly;

            var bugsFieldType = new HashSet<Type>
            {            
                // make these fields readonly
                typeof(Core.Localization.TexlStrings.StringGetter),
                typeof(Core.Localization.ErrorResourceKey),
            };

            var bugNames = new HashSet<string>
            {
                // Threading.InterlockedIncrement.
                "VersionHash._hashStarter",

                "TexlFunctionSet._empty", // points to an immutable object. 

                // readonly arrays / dictionary - is there an IReadOnly type to changes these too instead? 
                "ArgumentSuggestions._languageCodeSuggestions",
                "BuiltinFunctionsCore._featureGateFunctions",
                "BuiltinFunctionsCore._library",
                "BuiltinFunctionsCore._testOnlyLibrary",
                "DateAddFunction.SubDayStringList",
                "DelegationCapability._binaryOpToDelegationCapabilityMap",
                "DelegationCapability._operatorToDelegationCapabilityMap",
                "DelegationCapability._unaryOpToDelegationCapabilityMap",
                "DType._kindToSuperkindMapping",
                "DTypeSpecParser._types",
                "ErrorResource.ErrorResourceTagToReswSuffix",
                "IntellisenseProvider.SuggestionHandlers",
                "JsonFunction._unsupportedTopLevelTypes",
                "JsonFunction._unsupportedTypes",
                "LazyList`1.Empty",
                "ODataFunctionMappings.BinaryOpToOperatorMap",
                "ODataFunctionMappings.UnaryOpToOperatorMap",
                "PrettyPrintVisitor.BinaryPrecedence",
                       
                // Potential bugs, Need more review...
                "ArgumentSuggestions.CustomFunctionSuggestionProviders",
                "TrackingProvider.Instance",

                "DataTypeInfo._validDataFormatsPerDKind",
                "DataTypeInfo.AllowedValuesOnly",
                "DataTypeInfo.NoValidFormat", // returns arrays 

                "BuiltinFunctionsCore.OtherKnownFunctions",
                "Contracts._assertFailExCtor",
                "CurrentLocaleInfo.<CurrentLCID>k__BackingField",
                "CurrentLocaleInfo.<CurrentLocaleName>k__BackingField",
                "CurrentLocaleInfo.<CurrentUILanguageName>k__BackingField",
                "DelegationCapability.maxSingleCapabilityValue",
                "EmptyEnumerator`1._instance",
                "FeatureFlags._inTests",
                "FeatureFlags._stringInterpolation",
                "StringResources.<ExternalStringResources>k__BackingField",
                "StringResources.<ShouldThrowIfMissing>k__BackingField",
                "StringResources.ResourceManagers",
            };

            AnalyzeThreadSafety.CheckStatics(asm, bugsFieldType, bugNames);
        }
    }
}
