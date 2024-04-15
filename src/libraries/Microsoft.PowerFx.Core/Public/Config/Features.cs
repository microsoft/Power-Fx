// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx
{
    public sealed class Features
    {
        /// <summary>
        /// Enable Table syntax to not add "Value:" extra layer.
        /// </summary>
        internal bool TableSyntaxDoesntWrapRecords { get; set; }

        /// <summary>
        /// Enable functions to consistently return one dimension tables with
        /// a "Value" column rather than some other name like "Result".
        /// </summary>
        internal bool ConsistentOneColumnTableResult { get; set; }

        /// <summary>
        /// Disables support for row-scope disambiguation syntax.
        /// Now,for example user would need to use Filter(A, ThisRecord.Value = 2) or Filter(A As Foo, Foo.Value = 2)
        /// instead of
        /// Filter(A, A[@Value] = 2).
        /// </summary>
        internal bool DisableRowScopeDisambiguationSyntax { get; set; }

        /// <summary>
        /// Enable Identifier support for describing column names.
        /// </summary>
        internal bool SupportColumnNamesAsIdentifiers { get; set; }

        /// <summary>
        /// Enforces strong-typing for builtin enums, rather than treating
        /// them as aliases for values of string/number/boolean types.
        /// </summary>
        internal bool StronglyTypedBuiltinEnums { get; set; }

        /// <summary>
        /// Updates the IsEmpty function to only allow table arguments, since it
        /// does not work properly with other types of arguments.
        /// </summary>
        internal bool RestrictedIsEmptyArguments { get; set; }

        /// <summary>
        /// Allow delegation for async calls (delegate using awaited call result).
        /// </summary>
        internal bool AllowAsyncDelegation { get; set; }

        /// <summary>
        /// Allow delegation for impure nodes.
        /// </summary>
        internal bool AllowImpureNodeDelegation { get; set; }

        /// <summary>
        /// Updates the FirstN/LastN functions to require a second argument, instead of
        /// defaulting to 1.
        /// </summary>
        internal bool FirstLastNRequiresSecondArguments { get; set; }

        internal bool PowerFxV1CompatibilityRules { get; set; }

        internal bool SkipExpandableSetSemantics { get; set; }

        /// <summary>
        /// This is required by AsType() in PA delegation analysis.
        /// </summary>
        internal bool AsTypeLegacyCheck { get; set; }

        /// <summary>
        /// This is required by AsType() in Legacy Analysis.
        /// </summary>
        internal bool IsLegacyAnalysis { get; set; }

        /// <summary>
        /// Removes support for coercing a control to it's primary output property. 
        /// This only impacts PA Client scenarios, but some code still lives in PFx. 
        /// </summary>
        internal bool PrimaryOutputPropertyCoercionDeprecated { get; set; }

        /// <summary>
        /// This is specific for PVA team and it is a temporary feature.
        /// </summary>
        internal bool JsonFunctionAcceptsLazyTypes { get; set; }

        /// <summary>
        /// Enables more robust lookup reduction delegation.
        /// </summary>
        internal bool IsLookUpReductionDelegationEnabled { get; set; }

        /// <summary>
        /// This is specific for Cards team and it is a temporary feature.
        /// It will be soon deleted.
        /// </summary>
        [Obsolete]
        internal static Features PowerFxV1AllowSetExpandedTypes
        {
            get 
            {
                var ret = PowerFxV1;

                ret.SkipExpandableSetSemantics = true;

                return ret;
            }
        }

        internal static Features None => new Features();

        public static Features PowerFxV1 => new Features
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
            JsonFunctionAcceptsLazyTypes = true
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
        }
    }
}
