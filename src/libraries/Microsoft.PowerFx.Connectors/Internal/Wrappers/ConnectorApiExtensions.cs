// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Text.Json;
using Microsoft.OpenApi.Interfaces;

namespace Microsoft.PowerFx.Connectors
{
    internal class ConnectorApiExtensions : IConnectorExtensions
    {
        protected IDictionary<string, IOpenApiExtension> _extensions;

        protected ConnectorApiExtensions()
        {
        }

        public static IConnectorExtensions New(IOpenApiExtensible schema)
        {
            return schema == null ? null : new ConnectorApiExtensions(schema);
        }

        protected ConnectorApiExtensions(IOpenApiExtensible schema)
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

        public IDictionary<string, IOpenApiExtension> Extensions
        {
            get
            {
                Dictionary<string, IOpenApiExtension> exts = new Dictionary<string, IOpenApiExtension>();

                if (_schema.ValueKind != JsonValueKind.Object)
                {
                    return exts;
                }

                foreach (JsonProperty jp in _schema.EnumerateObject())
                {                    
                    exts.Add(jp.Name, jp.Value.ToIOpenApiExtension());
                }

                return exts;
            }
        }
    }      
}
