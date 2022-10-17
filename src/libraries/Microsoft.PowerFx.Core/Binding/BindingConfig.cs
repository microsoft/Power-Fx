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
        public static readonly BindingConfig Default = new BindingConfig(Features.None);

        public Features Features { get; }

        public bool AllowsSideEffects { get; }

        public bool UseThisRecordForRuleScope { get; }

        public BindingConfig(Features features, bool allowsSideEffects = false, bool useThisRecordForRuleScope = false)
        {
            AllowsSideEffects = allowsSideEffects;
            UseThisRecordForRuleScope = useThisRecordForRuleScope;
        }
    }
}
