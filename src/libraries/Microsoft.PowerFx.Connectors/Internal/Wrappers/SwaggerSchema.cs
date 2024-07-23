// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;

namespace Microsoft.PowerFx.Connectors
{
    internal class SwaggerSchema : SwaggerExtensions, ISwaggerSchema, IEquatable<SwaggerSchema>, IEquatable<ISwaggerSchema>
    {
        internal readonly OpenApiSchema _schema;

        private readonly string _type = null;

        private readonly string _format = null;

        public static ISwaggerSchema New(OpenApiSchema schema)
        {
            return schema == null ? null : new SwaggerSchema(schema);
        }

        private SwaggerSchema(OpenApiSchema schema)
            : base(schema)
        {
            _schema = schema;
        }

        public SwaggerSchema(ISwaggerSchema schema)
            : base()
        {
            if (schema is OpenApiSchema oas)
            {
                _schema = oas;
                base._extensions = oas.Extensions;
            }
            else if (schema is SwaggerSchema cas)
            {
                _schema = cas._schema;
                base._extensions = cas.Extensions;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public SwaggerSchema(string type, string format)
        {
            _schema = null;
            _type = type;
            _format = format;
        }

        public string Description => _schema?.Description;

        public string Title => _schema?.Title;

        public string Format => _format ?? _schema?.Format;

        public string Type => _type ?? _schema?.Type ?? GetTypeFromExtension(_schema);

        public IOpenApiAny Default => _schema?.Default;

        public ISet<string> Required => _schema?.Required;

        public ISwaggerDiscriminator Discriminator => SwaggerDiscriminator.New(_schema?.Discriminator);

        public ISwaggerSchema AdditionalProperties => SwaggerSchema.New(_schema?.AdditionalProperties);

        public IDictionary<string, ISwaggerSchema> Properties => _schema?.Properties == null
                                                                        ? new Dictionary<string, ISwaggerSchema>()
                                                                        : _schema.Properties.ToDictionary(kvp => kvp.Key, kvp => SwaggerSchema.New(kvp.Value));

        public ISwaggerSchema Items => _schema?.Items == null ? null : new SwaggerSchema(_schema.Items);

        private IList<IOpenApiAny> _enum = null;

        public IList<IOpenApiAny> Enum
        {
            get => _enum ?? _schema?.Enum;
            set => _enum = value;
        }

        public ISwaggerReference Reference => SwaggerReference.New(_schema?.Reference);

        // SalesForce specific
        public ISet<string> ReferenceTo => null;

        // SalesForce specific
        public string RelationshipName => null;

        // SalesForce specific
        public string DataType => null;

        private static string GetTypeFromExtension(OpenApiSchema schema)
        {
            if (schema == null || schema.Extensions == null)
            {
                return null;
            }

            // OpenAI special extension to indicate the type of the property
            if (schema.Extensions.TryGetValue("x-oaiTypeLabel", out IOpenApiExtension ext) &&
                ext is OpenApiString str &&
                !string.IsNullOrEmpty(str.Value))
            {
                return str.Value;
            }

            return null;
        }

        public static bool Equals(ISwaggerSchema schema1, ISwaggerSchema schema2)
        {
            if (ReferenceEquals(schema1, null) && ReferenceEquals(schema2, null))
            {
                return true;
            }

            if (ReferenceEquals(schema1, null) || ReferenceEquals(schema2, null))
            {
                return false;
            }

            if (schema1.Description != schema2.Description)
            {
                return false;
            }

            if (schema1.Title != schema2.Title)
            {
                return false;
            }

            if (schema1.Format != schema2.Format)
            {
                return false;
            }

            if (schema1.Type != schema2.Type)
            {
                return false;
            }

            if (!ConnectorType.SequenceEquals(schema1.Required, schema2.Required))
            {
                return false;
            }

            if (!ConnectorType.DictionaryEquals(schema1.Properties, schema2.Properties))
            {
                return false;
            }

            if (schema1.Reference?.Id != schema2.Reference?.Id)
            {
                return false;
            }

            if (schema1.Discriminator?.PropertyName != schema2.Discriminator?.PropertyName)
            {
                return false;
            }

            if (!ConnectorType.SequenceEquals(schema1.ReferenceTo, schema2.ReferenceTo))
            {
                return false;
            }

            if (schema1.RelationshipName != schema2.RelationshipName)
            {
                return false;
            }

            if (schema1.DataType != schema2.DataType)
            {
                return false;
            }

            return true;
        }

        public bool Equals(ISwaggerSchema other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return Equals(this, other);
        }

        public bool Equals(SwaggerSchema obj) => Equals(obj as ISwaggerSchema);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (ReferenceEquals(obj, null))
            {
                return false;
            }

            if (obj is ISwaggerSchema sws)
            {
                return SwaggerSchema.Equals(this, sws);
            }

            return false;
        }

        public static bool operator ==(SwaggerSchema left, SwaggerSchema right)
        {
            if (ReferenceEquals(left, null) && ReferenceEquals(right, null))
            {
                return true;
            }

            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
            {
                return false;
            }

            return left.Equals(right);
        }

        public static bool operator !=(SwaggerSchema left, SwaggerSchema right) => !(left == right);

        public override int GetHashCode() => _schema.GetHashCode();
    }
}
