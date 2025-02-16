// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Entities
{
    public interface INavigationProperty
    {
        string Name { get; }

        string SourceField { get; }

        string ForeignKey { get; }

        string RelationshipName { get; }

        string RelationshipType { get; }
    }
}
