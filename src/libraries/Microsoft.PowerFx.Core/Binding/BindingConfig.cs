﻿// Copyright (c) Microsoft Corporation.
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

        public BindingConfig(bool allowsSideEffects = false, bool useThisRecordForRuleScope = false, bool numberIsFloat = false, bool analysisMode = false, bool markAsAsyncOnLazilyLoadedControlRef = false)
        {
            AllowsSideEffects = allowsSideEffects;
            UseThisRecordForRuleScope = useThisRecordForRuleScope;
            NumberIsFloat = numberIsFloat;
            AnalysisMode = analysisMode;
            MarkAsAsyncOnLazilyLoadedControlRef = markAsAsyncOnLazilyLoadedControlRef;
        }
    }
}
