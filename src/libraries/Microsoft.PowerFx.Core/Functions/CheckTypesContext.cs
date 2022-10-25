// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Binding;

namespace Microsoft.PowerFx.Core.Functions
{
    internal class CheckTypesContext
    {
        public Features Features { get; }

        public INameResolver NameResolver { get; }

        public string EntityName { get; }

        public string PropertyName { get; }

        public bool IsEnhancedDelegationEnabled { get; }

        public bool AllowsSideEffects { get; }

        public CheckTypesContext(Features features, INameResolver nameResolver, string entityName, string propertyName, bool isEnhancedDelegationEnabled, bool allowsSideEffects)
        {
            Features = features;
            NameResolver = nameResolver;
            EntityName = entityName;
            PropertyName = propertyName;
            IsEnhancedDelegationEnabled = isEnhancedDelegationEnabled;
            AllowsSideEffects = allowsSideEffects;
        }
    }
}
