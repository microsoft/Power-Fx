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
    internal class JsonArrayExtension : IList<IOpenApiAny>, IOpenApiExtension, IOpenApiAny
    {
        private readonly JsonElement _je;

        public JsonArrayExtension(JsonElement je)
        {
            _je = je;
        }

        public IOpenApiAny this[int index] 
        {
            get => (IOpenApiAny)_je.EnumerateArray().ElementAt(index).ToIOpenApiExtension();
            set => throw new NotImplementedException(); 
        }

        public int Count => _je.EnumerateArray().Count();

        public bool IsReadOnly => throw new NotImplementedException();

        public AnyType AnyType => AnyType.Array;

        public void Add(IOpenApiAny item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(IOpenApiAny item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(IOpenApiAny[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<IOpenApiAny> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public int IndexOf(IOpenApiAny item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, IOpenApiAny item)
        {
            throw new NotImplementedException();
        }

        public bool Remove(IOpenApiAny item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
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
