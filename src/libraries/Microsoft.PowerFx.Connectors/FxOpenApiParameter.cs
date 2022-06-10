// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;

namespace Microsoft.PowerFx.Connectors
{
    internal class FxOpenApiParameter
    {
        private readonly FxParameterLocation? _in;
        private readonly OpenApiParameter _param;

        public FxOpenApiParameter(OpenApiSchema schema, string name, string description, FxParameterLocation? @in, bool required)
        {
            _param = new OpenApiParameter()
            {
                Schema = schema,
                Name = name,
                Description = description,
                Required = required,
            };

            _in = @in;
        }

        public FxOpenApiParameter(OpenApiParameter param)
        {
            _param = param;
        }

        public FxParameterLocation? In => _in ?? _param.In.ToFxParameterLocation();

        public bool IsInternal() => _param.IsInternal();      

        public string Name => _param.Name;

        public string Description => _param.Description;

        public OpenApiSchema Schema => _param.Schema;

        public bool Required => _param.Required;

        public IDictionary<string, IOpenApiExtension> Extensions => _param.Extensions;
    }

    internal static partial class FxExtensions
    {
        public static FxOpenApiParameter ToFxOpenApiParameter(this OpenApiParameter param) => new (param);        
    }
}
