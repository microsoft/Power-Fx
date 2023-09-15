// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Represents a parameter of a connector function.
    /// </summary>
    public class ConnectorParameter : ConnectorSchema
    {
        public string Name { get; private set; }

        public string Description { get; }

        internal ConnectorParameter(OpenApiParameter openApiParameter, bool numberIsFloat)
            : this(openApiParameter, null, false, numberIsFloat)
        {
        }

        internal ConnectorParameter(OpenApiParameter openApiParameter, bool useHiddenTypes, bool numberIsFloat)
            : this(openApiParameter, null, useHiddenTypes, numberIsFloat)
        {
        }

        internal ConnectorParameter(OpenApiParameter openApiParameter, IOpenApiExtensible bodyExtensions, bool numberIsFloat)
            : this(openApiParameter, bodyExtensions, false, numberIsFloat)
        {
        }

        internal ConnectorParameter(OpenApiParameter openApiParameter, IOpenApiExtensible bodyExtensions, bool useHiddenTypes, bool numberIsFloat)
            : base(openApiParameter, bodyExtensions, useHiddenTypes, numberIsFloat)
        {
            Name = openApiParameter.Name;
            Description = openApiParameter.Description;
        }

        internal ConnectorParameter(ConnectorParameter connectorParameter, ConnectorType connectorType)
            : base(connectorParameter, connectorType)
        {
            Name = connectorParameter.Name;
            Description = connectorParameter.Description;
        }

        internal void SetName(string name)
        {
            Name = name;
        }
    }
}
