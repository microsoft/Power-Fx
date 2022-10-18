// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Binding;

namespace Microsoft.PowerFx.Core.Functions
{
    internal class TexlFunctionConfig
    {
        public Features Features { get; }

        public INameResolver NameResolver { get; }

        public bool IsEnhancedDelegationEnabled { get; }

        public TexlFunctionConfig(Features features, INameResolver nameResolver, bool isEnhancedDelegationEnabled)
        {
            Features = features;
            NameResolver = nameResolver;
            IsEnhancedDelegationEnabled = isEnhancedDelegationEnabled;
        }
    }
}
