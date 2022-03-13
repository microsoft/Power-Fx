// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerFx.Core
{
    /// <summary>
    /// Handles marshalling a given type. Invoked by the <see cref="TypeMarshallerCache"/>.
    /// </summary>
    public interface ITypeMashallerProvider
    {
        // Return null if it doesn't handle it. 
        // A single ITypeMarshaler can be created once per type and then reused for each instance.
        // Pass in a cache for aggregate types that need to marshal sub types. 
        public bool TryGetMarshaller(Type type, TypeMarshallerCache cache, int maxDepth, out ITypeMarshaller marshaller);
    }

    /// <summary>
    /// A marshaller for a given System.Type.
    /// This can only marshal types that have a static mapping to a FormulaType.
    /// </summary>
    public interface ITypeMarshaller
    {
        public FormulaType Type { get; }

        // Implementor can assume that:
        // - value is not null. 
        // - the value matches the type check in the provider.
        // Throws on error. 
        // returned FormulaValue must match the given type. 
        public FormulaValue Marshal(object value);
    }
}
