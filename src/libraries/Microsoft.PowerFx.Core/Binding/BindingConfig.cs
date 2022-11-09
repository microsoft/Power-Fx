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

        /// <summary>
        /// This would allow users to do partial type checking by enabling support for Unknown type in input.
        /// </summary>
        public bool AllowDeferredType { get; }

        public BindingConfig(bool allowsSideEffects = false, bool useThisRecordForRuleScope = false, bool allowDeferredType = false)
        {
            AllowsSideEffects = allowsSideEffects;
            UseThisRecordForRuleScope = useThisRecordForRuleScope;
            AllowDeferredType = allowDeferredType;
        }
    }
}
