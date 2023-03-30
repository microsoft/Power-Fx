// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
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

        internal static Features None => new Features();

        public static Features PowerFxV1 => new Features
        {
            TableSyntaxDoesntWrapRecords = true,
            ConsistentOneColumnTableResult = true,
            DisableRowScopeDisambiguationSyntax = true,
            SupportColumnNamesAsIdentifiers = true,
            StronglyTypedBuiltinEnums = true,
            RestrictedIsEmptyArguments = true,
            AllowAsyncDelegation = true,
            AllowImpureNodeDelegation = true,
            FirstLastNRequiresSecondArguments = true,
            PowerFxV1CompatibilityRules = true,
        };

        /// <summary>
        /// All features enabled
        /// [USE WITH CAUTION] In using this value, you expose your code to future features.
        /// </summary>
        public static Features All => PowerFxV1;

        internal Features()
        {
        }

        public override bool Equals(object obj)
        {
            if (obj is not Features other)
            {
                return false;
            }

            return
                this.AllowAsyncDelegation == other.AllowAsyncDelegation &&
                this.AllowImpureNodeDelegation == other.AllowImpureNodeDelegation &&
                this.ConsistentOneColumnTableResult == other.ConsistentOneColumnTableResult &&
                this.DisableRowScopeDisambiguationSyntax == other.DisableRowScopeDisambiguationSyntax &&
                this.FirstLastNRequiresSecondArguments == other.FirstLastNRequiresSecondArguments &&
                this.PowerFxV1CompatibilityRules == other.PowerFxV1CompatibilityRules &&
                this.StronglyTypedBuiltinEnums == other.StronglyTypedBuiltinEnums &&
                this.SupportColumnNamesAsIdentifiers == other.SupportColumnNamesAsIdentifiers &&
                this.TableSyntaxDoesntWrapRecords == other.TableSyntaxDoesntWrapRecords;
        }

        public override int GetHashCode()
        {
            return Hashing.CombineHash(
                this.AllowAsyncDelegation.GetHashCode(),
                this.AllowImpureNodeDelegation.GetHashCode(),
                this.ConsistentOneColumnTableResult.GetHashCode(),
                this.DisableRowScopeDisambiguationSyntax.GetHashCode(),
                this.FirstLastNRequiresSecondArguments.GetHashCode(),
                this.PowerFxV1CompatibilityRules.GetHashCode(),
                this.StronglyTypedBuiltinEnums.GetHashCode(),
                this.SupportColumnNamesAsIdentifiers.GetHashCode(),
                this.TableSyntaxDoesntWrapRecords.GetHashCode());
        }
    }
}
