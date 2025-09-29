// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Core.Binding
{
    /// <summary>
    /// Configuration for an invocation of the binder.
    /// </summary>
    [ThreadSafeImmutable]
    internal class BindingConfig
    {
        public static readonly BindingConfig Default = new BindingConfig();

        public bool AllowsSideEffects { get; }

        public bool UseThisRecordForRuleScope { get; }

        public bool NumberIsFloat { get; }

        public bool AnalysisMode { get; }

        public bool MarkAsAsyncOnLazilyLoadedControlRef { get; } = false;

        public bool UserDefinitionsMode { get; }

        /// <summary>
        /// Enforces the expression must be "Simple", i.e. no async, no global access, no impure functions
        /// This is mainly used for controls like combobox where the expression is used to populate a dropdown
        /// and must be FAST to run on large datasets.
        /// </summary>
        internal bool EnforceSimpleExpressionConstraint { get; }

        public BindingConfig(
            bool allowsSideEffects = false,
            bool useThisRecordForRuleScope = false,
            bool numberIsFloat = false,
            bool analysisMode = false,
            bool markAsAsyncOnLazilyLoadedControlRef = false,
            bool userDefinitionsMode = false,
            bool enforceSimpleExpressions = false)
        {
            AllowsSideEffects = allowsSideEffects;
            UseThisRecordForRuleScope = useThisRecordForRuleScope;
            NumberIsFloat = numberIsFloat;
            AnalysisMode = analysisMode;
            MarkAsAsyncOnLazilyLoadedControlRef = markAsAsyncOnLazilyLoadedControlRef;
            UserDefinitionsMode = userDefinitionsMode;
            EnforceSimpleExpressionConstraint = enforceSimpleExpressions;
        }

        public BindingConfig Clone(bool allowsSideEffects)
        {
            return new BindingConfig(
                allowsSideEffects: allowsSideEffects, // overrides value in object
                useThisRecordForRuleScope: this.UseThisRecordForRuleScope,
                numberIsFloat: this.NumberIsFloat,
                analysisMode: this.AnalysisMode,
                markAsAsyncOnLazilyLoadedControlRef: this.MarkAsAsyncOnLazilyLoadedControlRef,
                userDefinitionsMode: this.UserDefinitionsMode);
        }
    }
}
