// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;

namespace Microsoft.PowerFx.Connectors
{
    internal class ConnectorApiParameter : ConnectorApiExtensions, IConnectorParameter
    {
        private readonly string _name;

        private readonly IConnectorSchema _schema;

        private readonly bool _required;

        public static IConnectorParameter New(OpenApiParameter parameter)
        {
            return parameter == null ? null : new ConnectorApiParameter(parameter);
        }

        private ConnectorApiParameter(OpenApiParameter parameter)
            : base(parameter)
        {
            _name = parameter.Name;
            _required = parameter.Required;
            _schema = ConnectorApiSchema.New(parameter.Schema);
        }

        public ConnectorApiParameter(string name, bool required, IConnectorSchema schema, IDictionary<string, IOpenApiExtension> extensions)
            : base(extensions)
        {
            _name = name;
            _required = required;
            _schema = schema;
        }

        public ConnectorApiParameter(IConnectorParameter parameter)
            : base()
        {
            if (parameter is OpenApiParameter oap)
            {
                _name = oap.Name;
                _required = oap.Required;
                _schema = ConnectorApiSchema.New(oap.Schema);
                base._extensions = oap.Extensions;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public IConnectorSchema Schema => _schema;

        public string Name => _name;

        public bool Required => _required;
    }
}
