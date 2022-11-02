﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
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

        public CultureInfo CultureInfo { get; }

        public BindingConfig(bool allowsSideEffects = false, bool useThisRecordForRuleScope = false, CultureInfo locale = null)
        {
            AllowsSideEffects = allowsSideEffects;
            UseThisRecordForRuleScope = useThisRecordForRuleScope;
            CultureInfo = locale;
        }
    }
}
