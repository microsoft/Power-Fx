// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx
{
    [ThreadSafeImmutable]
    public sealed class Features
    {
        /// <summary>
        /// Enable Table syntax to not add "Value:" extra layer.
        /// </summary>
        internal bool TableSyntaxDoesntWrapRecords { get; init; }

        /// <summary>
        /// Enable functions to consistently return one dimension tables with
        /// a "Value" column rather than some other name like "Result".
        /// </summary>
        internal bool ConsistentOneColumnTableResult { get; init; }

        /// <summary>
        /// Disables support for row-scope disambiguation syntax.
        /// Now,for example user would need to use Filter(A, ThisRecord.Value = 2) or Filter(A As Foo, Foo.Value = 2)
        /// instead of
        /// Filter(A, A[@Value] = 2).
        /// </summary>
        internal bool DisableRowScopeDisambiguationSyntax { get; init; }

        /// <summary>
        /// Enable Identifier support for describing column names.
        /// </summary>
        internal bool SupportColumnNamesAsIdentifiers { get; init; }

        /// <summary>
        /// Enforces strong-typing for builtin enums, rather than treating
        /// them as aliases for values of string/number/boolean types.
        /// </summary>
        internal bool StronglyTypedBuiltinEnums { get; init; }

        /// <summary>
        /// Updates the IsEmpty function to only allow table arguments, since it
        /// does not work properly with other types of arguments.
        /// </summary>
        internal bool RestrictedIsEmptyArguments { get; init; }

        /// <summary>
        /// Updates the FirstN/LastN functions to require a second argument, instead of
        /// defaulting to 1.
        /// </summary>
        internal bool FirstLastNRequiresSecondArguments { get; init; }

        internal bool PowerFxV1CompatibilityRules { get; init; }

        /// <summary>
        /// This is required by AsType() in PA delegation analysis.
        /// </summary>
        internal bool AsTypeLegacyCheck { get; init; }

        /// <summary>
        /// Removes support for coercing a control to it's primary output property. 
        /// This only impacts PA Client scenarios, but some code still lives in PFx. 
        /// </summary>
        internal bool PrimaryOutputPropertyCoercionDeprecated { get; init; }

        /// <summary>
        /// This is specific for PVA team and it is a temporary feature.
        /// </summary>
        internal bool JsonFunctionAcceptsLazyTypes { get; init; }

        /// <summary>
        /// Enables more robust lookup reduction delegation.
        /// </summary>
        internal bool IsLookUpReductionDelegationEnabled { get; init; }

        /// <summary>
        /// Enables User-defined types functionality.
        /// </summary>
        internal bool IsUserDefinedTypesEnabled { get; init; } = false;

        internal static readonly Features None = new Features();

        /// <summary>
        /// The default V1 Power Fx feature set. 
        /// </summary>
        public static Features PowerFxV1 => _powerFxV1;

        private static readonly Features _powerFxV1 = new Features
        {
            TableSyntaxDoesntWrapRecords = true,
            ConsistentOneColumnTableResult = true,
            DisableRowScopeDisambiguationSyntax = true,
            SupportColumnNamesAsIdentifiers = true,
            StronglyTypedBuiltinEnums = true,
            RestrictedIsEmptyArguments = true,
            FirstLastNRequiresSecondArguments = true,
            PowerFxV1CompatibilityRules = true,
            PrimaryOutputPropertyCoercionDeprecated = true,
            AsTypeLegacyCheck = false,
            JsonFunctionAcceptsLazyTypes = true,
            IsUserDefinedTypesEnabled = true,
        };

        internal Features()
        {
        }

        internal Features(Features other)
        {
            TableSyntaxDoesntWrapRecords = other.TableSyntaxDoesntWrapRecords;
            ConsistentOneColumnTableResult = other.ConsistentOneColumnTableResult;
            DisableRowScopeDisambiguationSyntax = other.DisableRowScopeDisambiguationSyntax;
            SupportColumnNamesAsIdentifiers = other.SupportColumnNamesAsIdentifiers;
            StronglyTypedBuiltinEnums = other.StronglyTypedBuiltinEnums;
            RestrictedIsEmptyArguments = other.RestrictedIsEmptyArguments;
            FirstLastNRequiresSecondArguments = other.FirstLastNRequiresSecondArguments;
            PowerFxV1CompatibilityRules = other.PowerFxV1CompatibilityRules;
            PrimaryOutputPropertyCoercionDeprecated = other.PrimaryOutputPropertyCoercionDeprecated;
            IsUserDefinedTypesEnabled = other.IsUserDefinedTypesEnabled;
            AsTypeLegacyCheck = other.AsTypeLegacyCheck;
            JsonFunctionAcceptsLazyTypes = other.JsonFunctionAcceptsLazyTypes;
            IsLookUpReductionDelegationEnabled = other.IsLookUpReductionDelegationEnabled;
        }
    }
}
