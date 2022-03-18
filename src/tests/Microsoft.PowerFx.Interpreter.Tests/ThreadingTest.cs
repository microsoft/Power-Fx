// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Utils;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    // Do static analysis to look for potential threading issues. 
    public class ThreadingTest
    {
        [Fact]
        public void CheckIntepreter()
        {
            var asm = typeof(RecalcEngine).Assembly;            
            CheckStatics(asm);
        }

        [Fact]
        public void CheckFxCore()
        {
            var asm = typeof(Core.Syntax.Nodes.TexlNode).Assembly;
            CheckStatics(asm);
        }

        // Verify there are no "unsafe" static fields that could be threading issues.
        private void CheckStatics(Assembly asm)
        {
            var errors = new List<string>();

            var total = 0;
            foreach (var type in asm.GetTypes())
            {
                // Reflecting over fields will also find compiler-generated "backing fields" from static properties. 
                foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var name = $"{type.Name}.{field.Name}";
                    total++;

                    if (field.Attributes.HasFlag(FieldAttributes.Literal))
                    {
                        continue;  // 'const' keyword, 
                    }

                    if (type.Name.Contains("<>") ||
                        type.Name.Contains("<PrivateImplementationDetails>"))
                    {
                        continue; // exclude compiler generated closures. 
                    }

                    // Whitelist - these are reviewed and ok
                    var whitelist = new HashSet<string>
                    {                     
                        "CallNode._uniqueInvocationIdNext"
                    };
                    if (whitelist.Contains(name))
                    {
                        continue;
                    }

                    // TODO - Drive this list to zero. 
                    var bugs = new HashSet<Type>
                    {
                        typeof(Dictionary<Core.IR.Nodes.UnaryOpKind, Functions.Library.FunctionPtr>),
                        typeof(Functions.Library.FunctionPtr),

                         // make these fields readonly
                        typeof(Core.Localization.TexlStrings.StringGetter),
                        typeof(Core.Localization.ErrorResourceKey),
                    };
                    if (bugs.Contains(field.FieldType))
                    {
                        continue;
                    }

                    // TODO - Drive this list to zero. 
                    var bugNames = new HashSet<string>
                    {
                        // readonly arrays / dictionary - is there an IReadOnly type to changes these too instead? 
                        "Library._funcsByName",
                        "LazyList`1.Empty",
                        "DType._kindToSuperkindMapping",
                        "DTypeSpecParser._types",
                        "BuiltinFunctionsCore._library",
                        "ArgumentSuggestions._languageCodeSuggestions",
                        "IntellisenseProvider.SuggestionHandlers",
                        "DateAddFunction.SubDayStringList",
                        "PrettyPrintVisitor.BinaryPrecedence",
                        "ErrorResource.ErrorResourceTagToReswSuffix",
                        "StringResources.Strings",
                        "StringResources.ErrorResources",
                        "DelegationCapability._binaryOpToDelegationCapabilityMap",
                        "DelegationCapability._unaryOpToDelegationCapabilityMap",
                        "DelegationCapability._operatorToDelegationCapabilityMap",
                        "ODataFunctionMappings.BinaryOpToOperatorMap",
                        "ODataFunctionMappings.UnaryOpToOperatorMap",
                        "DataTypeInfo.NoValidFormat",
                        "DataTypeInfo.AllowedValuesOnly",
                        "DataTypeInfo._validDataFormatsPerDKind",
                        
                        // Potential bugs, Need more review...
                        "ArgumentSuggestions.CustomFunctionSuggestionProviders",
                        "TrackingProvider.Instance",
                        "TexlLexer._prebuiltLexers",
                        "Library._random",

                        "FeatureFlags.StringInterpolation",
                        "EmptyEnumerator`1._instance",
                        "StringBuilderCache`1.maxBuilderSize",
                        "StringBuilderCache`1.cachedInstance",
                        "Contracts._assertFailExCtor",
                        "CurrentLocaleInfo.<CurrentLocaleName>k__BackingField",
                        "CurrentLocaleInfo.<CurrentUILanguageName>k__BackingField",
                        "CurrentLocaleInfo.<CurrentLCID>k__BackingField",
                        "StringResources.<ExternalStringResources>k__BackingField",
                        "StringResources.<ShouldThrowIfMissing>k__BackingField",
                        "TexlLexer._lex",
                        "DelegationCapability.maxSingleCapabilityValue",
                    };
                    if (bugNames.Contains(name))
                    {
                        continue;
                    }

                    // Is it readonly?  Const?
                    if (!field.Attributes.HasFlag(FieldAttributes.InitOnly))
                    {
                        // Mutable static field! That's bad.  
                        errors.Add($"{name} is not readonly");
                        continue;
                    }

                    // Is it a
                    if (!IsTypeImmutable(field.FieldType))
                    {
                        errors.Add($"{name} readonly, but still a mutable type {field.FieldType}");
                    }

                    // Safe! The static field is readonly and set to an immutable object. 
                }
            }

            // Sanity check that we actuall ran the test. 
            Assert.True(total > 10, "failed to find fields");

            // Batch up errors so we can see all at once. 
            Assert.Empty(errors);            
        }

        // TODO - Mark these with [Immutable] attribute.
        // https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.immutableobjectattribute?view=net-6.0? 
        private static readonly HashSet<Type> _knownImmutableTypes = new HashSet<Type>
        {
            // Primitives
            typeof(object),
            typeof(string),
            typeof(Random),
            typeof(DateTime),
            typeof(System.Text.RegularExpressions.Regex),
            typeof(System.Numerics.BigInteger),

            // Readonly collections 
            typeof(IReadOnlyDictionary<,>),

            // Custom types 
            typeof(DisabledDisplayNameProvider),
            typeof(DName),
            typeof(DPath),
            typeof(Core.Types.DType),
            typeof(Core.Functions.FunctionArgValidators.SortOrderValidator),
            typeof(Core.Functions.FunctionArgValidators.DelegatableDataSourceInfoValidator),
            typeof(Core.Functions.FunctionArgValidators.DataSourceArgNodeValidator),
            typeof(Core.Functions.FunctionArgValidators.EntityArgNodeValidator),
            typeof(Core.Binding.ScopeUseSet),

            // Remove these?
            typeof(Core.Texl.Intellisense.AddSuggestionHelper),
            typeof(Core.Texl.Intellisense.AddSuggestionDryRunHelper)
        };

        // If the instance is readonly, is the type itself immutable ?
        private static bool IsTypeImmutable(Type t)
        {
            if (t.IsPrimitive)
            {
                return true;
            }

            var attr = t.GetCustomAttribute<System.ComponentModel.ImmutableObjectAttribute>();
            if (attr != null)
            {
                return attr.Immutable;
            }
            
            if (t.IsGenericType)
            {
                t = t.GetGenericTypeDefinition();
            }

            if (_knownImmutableTypes.Contains(t))
            {
                return true;
            }

            return false;
        }
    }
}
