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

        public bool AllowsSideEffects { get; }

        public bool NumberIsFloat { get; }

        public CheckTypesContext(Features features, INameResolver nameResolver, string entityName, string propertyName, bool allowsSideEffects, bool numberIsFloat)
        {
            Features = features;
            NameResolver = nameResolver;
            EntityName = entityName;
            PropertyName = propertyName;
            AllowsSideEffects = allowsSideEffects;
            NumberIsFloat = numberIsFloat;
        }
    }
}
