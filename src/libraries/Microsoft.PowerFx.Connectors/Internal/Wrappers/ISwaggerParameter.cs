// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Connectors
{
    internal interface ISwaggerParameter : ISwaggerExtensions
    {
        public ISwaggerSchema Schema { get; }

        public string Name { get; }

        public bool Required { get; }
    }
}
