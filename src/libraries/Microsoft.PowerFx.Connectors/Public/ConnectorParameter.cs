// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Represents a parameter of a connector function.
    /// </summary>
    [DebuggerDisplay("{Name} {ConnectorType}")]
    public class ConnectorParameter : ConnectorSchema
    {
        public string Name { get; private set; }

        public string Description { get; }

        internal bool IsBodyParameter = false;

        internal ConnectorParameter(OpenApiParameter openApiParameter)
            : this(openApiParameter, null, false)
        {
        }

        internal ConnectorParameter(OpenApiParameter openApiParameter, bool useHiddenTypes)
            : this(openApiParameter, null, useHiddenTypes)
        {
        }

        internal ConnectorParameter(OpenApiParameter openApiParameter, IOpenApiExtensible bodyExtensions)
            : this(openApiParameter, bodyExtensions, false)
        {
            IsBodyParameter = true;
        }

        internal ConnectorParameter(OpenApiParameter openApiParameter, IOpenApiExtensible bodyExtensions, bool useHiddenTypes)
            : base(openApiParameter, bodyExtensions, useHiddenTypes)
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
