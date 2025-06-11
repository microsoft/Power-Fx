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
        /// <summary>
        /// Gets the name of the parameter.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the description of the parameter.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the location of the parameter (Query, Header, Path, or Cookie).
        /// </summary>
        public ParameterLocation? Location { get; }

        internal bool IsBodyParameter = false;

        internal ConnectorParameter(OpenApiParameter openApiParameter, ConnectorSettings settings)
            : this(openApiParameter, null, false, settings)
        {
        } 

        internal ConnectorParameter(OpenApiParameter openApiParameter, bool useHiddenTypes, ConnectorSettings settings)
            : this(openApiParameter, null, useHiddenTypes, settings)
        {
        }

        internal ConnectorParameter(OpenApiParameter openApiParameter, IOpenApiExtensible bodyExtensions, ConnectorSettings settings)
            : this(openApiParameter, bodyExtensions, false, settings)
        {
            IsBodyParameter = true;
        }

        internal ConnectorParameter(OpenApiParameter openApiParameter, IOpenApiExtensible bodyExtensions, bool useHiddenTypes, ConnectorSettings settings)
            : base(SwaggerParameter.New(openApiParameter), SwaggerExtensions.New(bodyExtensions), useHiddenTypes, settings)
        {
            Name = openApiParameter.Name;
            Description = openApiParameter.Description;
            Location = openApiParameter.In;
        }

        // Intellisense only
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
