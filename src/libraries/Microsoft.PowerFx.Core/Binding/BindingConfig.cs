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
        public static readonly BindingConfig Default = new BindingConfig(allowsSideEffects: false);

        public bool AllowsSideEffects { get; }

        public BindingConfig(bool allowsSideEffects)
        {
            AllowsSideEffects = allowsSideEffects;
        }
    }
}
