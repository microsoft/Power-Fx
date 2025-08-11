// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;

namespace Microsoft.PowerFx.Connectors
{
    internal class SwaggerParameter : SwaggerExtensions, ISwaggerParameter
    {
        private readonly string _name;

        private readonly ISwaggerSchema _schema;

        private readonly bool _required;

        public static ISwaggerParameter New(OpenApiParameter parameter)
        {
            return parameter == null ? null : new SwaggerParameter(parameter);
        }

        private SwaggerParameter(OpenApiParameter parameter)
            : base(parameter)
        {
            _name = parameter.Name;
            _required = parameter.Required;
            _schema = SwaggerSchema.New(parameter.Schema);
        }

        public SwaggerParameter(string name, bool required, ISwaggerSchema schema, IDictionary<string, IOpenApiExtension> extensions)
            : base(extensions)
        {
            _name = name;
            _required = required;
            _schema = schema;
        }

        public SwaggerParameter(ISwaggerParameter parameter)
            : base()
        {
            if (parameter is OpenApiParameter oap)
            {
                _name = oap.Name;
                _required = oap.Required;
                _schema = SwaggerSchema.New(oap.Schema);
                base._extensions = oap.Extensions;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public ISwaggerSchema Schema => _schema;

        public string Name => _name;

        public bool Required => _required;
    }
}
