// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.OpenApi.Any;

namespace Microsoft.PowerFx.Connectors
{
    internal interface ISwaggerSchema : ISwaggerExtensions
    {
        string Description { get; }

        string Title { get; }

        string Format { get; }

        string Type { get; }

        IOpenApiAny Default { get; }

        ISet<string> Required { get; }

        ISwaggerSchema AdditionalProperties { get; }

        IDictionary<string, ISwaggerSchema> Properties { get; }

        ISwaggerSchema Items { get; }

        IList<IOpenApiAny> Enum { get; set; }

        ISwaggerReference Reference { get; }

        ISwaggerDiscriminator Discriminator { get; }

        // SalesForce specific
        ISet<string> ReferenceTo { get; }

        // SalesForce specific
        string RelationshipName { get; }

        // SalesForce specific
        string DataType { get; }
    }
}
