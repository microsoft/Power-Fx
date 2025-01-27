// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Entities;

namespace Microsoft.PowerFx.Connectors
{
    public class NavigationProperty : INavigationProperty
    {
        public string Name { get; init; }

        public string SourceField { get; init; }

        public string ForeignKey { get; init; }

        public string RelationshipName { get; init; }

        public string RelationshipType { get; init; }
    }
}
