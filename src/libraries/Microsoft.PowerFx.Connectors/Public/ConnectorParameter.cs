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

        // Query, Header, Path or Cookie (not supported yet)
        public ParameterLocation? Location { get; }

        internal bool IsBodyParameter = false;

        internal ConnectorParameter(OpenApiParameter openApiParameter, string tableName, OptionSetList optionSets, ConnectorCompatibility compatibility)
            : this(openApiParameter, null, false, tableName, optionSets, compatibility)
        {
        }

        internal ConnectorParameter(OpenApiParameter openApiParameter, bool useHiddenTypes, string tableName, OptionSetList optionSets, ConnectorCompatibility compatibility)
            : this(openApiParameter, null, useHiddenTypes, tableName, optionSets, compatibility)
        {
        }

        internal ConnectorParameter(OpenApiParameter openApiParameter, IOpenApiExtensible bodyExtensions, string tableName, OptionSetList optionSets, ConnectorCompatibility compatibility)
            : this(openApiParameter, bodyExtensions, false, tableName, optionSets, compatibility)
        {
            IsBodyParameter = true;
        }

        internal ConnectorParameter(OpenApiParameter openApiParameter, IOpenApiExtensible bodyExtensions, bool useHiddenTypes, string tableName, OptionSetList optionSets, ConnectorCompatibility compatibility)
            : base(SwaggerParameter.New(openApiParameter), SwaggerExtensions.New(bodyExtensions), useHiddenTypes, tableName, optionSets, compatibility)
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
