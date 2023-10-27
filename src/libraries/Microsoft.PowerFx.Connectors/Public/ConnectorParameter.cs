// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
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

        // Query, Header, Path or Cookie (not supported yet)
        public ParameterLocation? Location { get; }

        internal bool IsBodyParameter = false;

        internal ConnectorParameter(OpenApiParameter openApiParameter, Dictionary<int, ConnectorType> openApiParameterCache)
            : this(openApiParameter, null, false, openApiParameterCache)
        {
        }

        internal ConnectorParameter(OpenApiParameter openApiParameter, bool useHiddenTypes, Dictionary<int, ConnectorType> openApiParameterCache)
            : this(openApiParameter, null, useHiddenTypes, openApiParameterCache)
        {
        }

        internal ConnectorParameter(OpenApiParameter openApiParameter, IOpenApiExtensible bodyExtensions, Dictionary<int, ConnectorType> openApiParameterCache)
            : this(openApiParameter, bodyExtensions, false, openApiParameterCache)
        {
            IsBodyParameter = true;
        }

        internal ConnectorParameter(OpenApiParameter openApiParameter, IOpenApiExtensible bodyExtensions, bool useHiddenTypes, Dictionary<int, ConnectorType> openApiParameterCache)
            : base(openApiParameter, bodyExtensions, useHiddenTypes, openApiParameterCache)
        {
            Name = openApiParameter.Name;
            Description = openApiParameter.Description;
            Location = openApiParameter.In;
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
