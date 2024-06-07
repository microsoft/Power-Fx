// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.OpenApi.Any;

namespace Microsoft.PowerFx.Connectors
{
    internal interface IConnectorSchema : IConnectorExtensions
    {
        string Description { get; }

        string Title { get; }

        string Format { get; }

        string Type { get; }

        IOpenApiAny Default { get; }

        ISet<string> Required { get; }

        IConnectorSchema AdditionalProperties { get; }

        IDictionary<string, IConnectorSchema> Properties { get; }

        IConnectorSchema Items { get; }

        IList<IOpenApiAny> Enum { get; set; }

        IConnectorReference Reference { get; }

        IConnectorDiscriminator Discriminator { get; }

        // SalesForce specific
        ISet<string> ReferenceTo { get; }

        // SalesForce specific
        string RelationshipName { get; }

        // SalesForce specific
        string DataType { get; }
    }
}
