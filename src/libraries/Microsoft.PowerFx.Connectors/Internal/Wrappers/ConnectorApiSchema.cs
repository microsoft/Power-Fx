// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Microsoft.PowerFx.Connectors
{
    internal class ConnectorApiSchema : ConnectorApiExtensions, IConnectorSchema
    {
        internal readonly OpenApiSchema _schema;

        private readonly string _type = null;

        private readonly string _format = null;

        public static IConnectorSchema New(OpenApiSchema schema)
        {           
            return schema == null ? null : new ConnectorApiSchema(schema);
        }

        private ConnectorApiSchema(OpenApiSchema schema)
            : base(schema)
        {
            _schema = schema;
        }

        public ConnectorApiSchema(IConnectorSchema schema)
            : base()
        {
            if (schema is OpenApiSchema oas)
            {
                _schema = oas;
                base._extensions = oas.Extensions;
            }
            else if (schema is ConnectorApiSchema cas)
            {
                _schema = cas._schema;
                base._extensions = cas.Extensions;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public ConnectorApiSchema(string type, string format)
        {
            _schema = null;
            _type = type;
            _format = format;
        }

        public string Description => _schema?.Description;

        public string Title => _schema?.Title;

        public string Format => _format ?? _schema?.Format;

        public string Type => _type ?? _schema?.Type;

        public IOpenApiAny Default => _schema?.Default;

        public ISet<string> Required => _schema?.Required;

        public IConnectorDiscriminator Discriminator => ConnectorApiDiscriminator.New(_schema?.Discriminator);

        public IConnectorSchema AdditionalProperties => ConnectorApiSchema.New(_schema?.AdditionalProperties);

        public IDictionary<string, IConnectorSchema> Properties => _schema?.Properties == null
                                                                        ? new Dictionary<string, IConnectorSchema>()
                                                                        : _schema.Properties.ToDictionary(kvp => kvp.Key, kvp => ConnectorApiSchema.New(kvp.Value));

        public IConnectorSchema Items => _schema.Items == null ? null : new ConnectorApiSchema(_schema.Items);

        private IList<IOpenApiAny> _enum = null;

        public IList<IOpenApiAny> Enum
        {
            get => _enum ?? _schema?.Enum;
            set => _enum = value;
        }

        public IConnectorReference Reference => ConnectorApiReference.New(_schema.Reference);

        // SalesForce specific
        public ISet<string> ReferenceTo => null;

        // SalesForce specific
        public string RelationshipName => null;

        // SalesForce specific
        public string DataType => null;
    }
}
