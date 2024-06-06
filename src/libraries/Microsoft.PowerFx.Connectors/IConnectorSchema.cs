// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using static Microsoft.PowerFx.Connectors.ConnectorApiSchema;

namespace Microsoft.PowerFx.Connectors
{
    internal class ConnectorApiParameter : ConnectorApiExtensions, IConnectorParameter
    {
        private readonly string _name;
        private readonly IConnectorSchema _schema;
        private readonly bool _required;

        public ConnectorApiParameter(OpenApiParameter parameter)
            : base(parameter)
        {
            _name = parameter.Name;
            _required = parameter.Required;
            _schema = new ConnectorApiSchema(parameter.Schema);
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
                _schema = new ConnectorApiSchema(oap.Schema);
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

    internal class ConnectorApiSchema : ConnectorApiExtensions, IConnectorSchema
    {
        internal readonly OpenApiSchema _schema;
        private readonly string _type = null;
        private readonly string _format = null;

        public ConnectorApiSchema(OpenApiSchema schema)
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

        public IConnectorDiscriminator Discriminator => _schema?.Discriminator == null ? null : new ConnectorApiDiscriminator(_schema.Discriminator);

        public IConnectorSchema AdditionalProperties => _schema?.AdditionalProperties == null ? null : new ConnectorApiSchema(_schema.AdditionalProperties);

        public IDictionary<string, IConnectorSchema> Properties => _schema?.Properties == null 
                                                                        ? new Dictionary<string, IConnectorSchema>()
                                                                        : _schema.Properties.ToDictionary(kvp => kvp.Key, kvp => (IConnectorSchema)new ConnectorApiSchema(kvp.Value));

        public IConnectorSchema Items => _schema.Items == null ? null : new ConnectorApiSchema(_schema.Items);

        private IList<IOpenApiAny> _enum = null;

        public IList<IOpenApiAny> Enum
        {
            get => _enum ?? _schema?.Enum;
            set => _enum = value;
        }

        public IConnectorReference Reference => _schema?.Reference != null ? new ConnectorApiReference(_schema.Reference) : null;
    }

    internal class ConnectorJsonSchema : ConnectorJsonExtensions, IConnectorSchema
    {
        private readonly JsonElement _schema;
        private readonly string _type = null;
        private readonly string _format = null;

        public ConnectorJsonSchema(JsonElement schema)
            : base(schema)
        {
            _schema = schema;
        }

        public string Description => throw new NotImplementedException();

        public string Title => throw new NotImplementedException();

        public string Format => throw new NotImplementedException();

        public string Type => throw new NotImplementedException();

        public IOpenApiAny Default => throw new NotImplementedException();

        public ISet<string> Required => throw new NotImplementedException();

        public IConnectorSchema AdditionalProperties => throw new NotImplementedException();

        public IDictionary<string, IConnectorSchema> Properties => throw new NotImplementedException();

        public IConnectorSchema Items => throw new NotImplementedException();

        public IList<IOpenApiAny> Enum 
        { 
            get => throw new NotImplementedException(); 
            set => throw new NotImplementedException(); 
        }

        public IConnectorReference Reference => throw new NotImplementedException();

        public IConnectorDiscriminator Discriminator => throw new NotImplementedException();
    }

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

        public IConnectorReference Reference { get; }

        IConnectorDiscriminator Discriminator { get; }
    }

    internal class ConnectorApiExtensions : IConnectorExtensions
    {
        protected IDictionary<string, IOpenApiExtension> _extensions;

        protected ConnectorApiExtensions()
        {
        }

        public ConnectorApiExtensions(IOpenApiExtensible schema)
        {
            _extensions = schema?.Extensions;
        }

        public ConnectorApiExtensions(IDictionary<string, IOpenApiExtension> extensions)
        {
            _extensions = extensions;
        }

        public IDictionary<string, IOpenApiExtension> Extensions => _extensions ?? new Dictionary<string, IOpenApiExtension>();
    }

    internal class ConnectorJsonExtensions : IConnectorExtensions
    {
        private readonly JsonElement _schema;

        public ConnectorJsonExtensions(JsonElement je)
        {
            _schema = je;
        }

        public IDictionary<string, IOpenApiExtension> Extensions => throw new NotImplementedException();
    }

    internal interface IConnectorExtensions
    {
        IDictionary<string, IOpenApiExtension> Extensions { get; }
    }

    internal interface IConnectorDiscriminator
    {
        string PropertyName { get; }
    }

    public class ConnectorApiDiscriminator : IConnectorDiscriminator
    {
        private readonly OpenApiDiscriminator _discriminator;

        public ConnectorApiDiscriminator(OpenApiDiscriminator discriminator)
        {
            _discriminator = discriminator;
        }

        public string PropertyName => _discriminator?.PropertyName;
    }

    public class ConnectorApiReference : IConnectorReference
    {
        private readonly OpenApiReference _reference;

        public ConnectorApiReference(OpenApiReference reference)
        {
            _reference = reference ?? throw new ArgumentNullException(nameof(reference));
        }

        public string Id => _reference.Id;
    }

    internal interface IConnectorReference
    {
        string Id { get; }
    }

    internal interface IConnectorParameter : IConnectorExtensions
    {
        public IConnectorSchema Schema { get; }

        public string Name { get; }

        public bool Required { get; }

    }
}
