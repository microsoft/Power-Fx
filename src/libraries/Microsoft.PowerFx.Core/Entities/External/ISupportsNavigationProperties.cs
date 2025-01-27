// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.PowerFx.Core.Entities
{
    public interface ISupportsNavigationProperties
    {
        public IReadOnlyList<INavigationProperty> GetNavigationProperties(string fieldName);
    }
}
