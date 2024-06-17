// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Writers;

namespace Microsoft.PowerFx.Connectors
{
    internal class SwaggerJsonObject : IDictionary<string, IOpenApiAny>, IOpenApiExtension, IOpenApiAny
    {
        private readonly JsonElement _je;

        public SwaggerJsonObject(JsonElement je)
        {
            _je = je;
        }

        public IOpenApiAny this[string key] 
        { 
            get => _je.TryGetProperty(key, out JsonElement val) ? (IOpenApiAny)val.ToIOpenApiExtension() : null;
            set => throw new NotImplementedException(); 
        }

        public ICollection<string> Keys => throw new NotImplementedException();

        public ICollection<IOpenApiAny> Values => throw new NotImplementedException();

        public int Count => _je.EnumerateObject().Count();

        public bool IsReadOnly => throw new NotImplementedException();

        public AnyType AnyType => AnyType.Object;

        public void Add(string key, IOpenApiAny value)
        {
            throw new NotImplementedException();
        }

        public void Add(KeyValuePair<string, IOpenApiAny> item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(KeyValuePair<string, IOpenApiAny> item)
        {
            throw new NotImplementedException();
        }

        public bool ContainsKey(string key)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(KeyValuePair<string, IOpenApiAny>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<string, IOpenApiAny>> GetEnumerator()
        {
            Dictionary<string, IOpenApiAny> dict = new Dictionary<string, IOpenApiAny>();

            foreach (JsonProperty jp in _je.EnumerateObject())
            {
                dict.Add(jp.Name, (IOpenApiAny)jp.Value.ToIOpenApiExtension());
            }

            return dict.GetEnumerator();
        }

        public bool Remove(string key)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<string, IOpenApiAny> item)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(string key, out IOpenApiAny value)
        {
            if (!_je.TryGetProperty(key, out JsonElement val))
            {
                value = null;
                return false;
            }

            value = (IOpenApiAny)val.ToIOpenApiExtension();
            return true;
        }

        public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
