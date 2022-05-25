// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Handles marshalling a given type. Invoked by the <see cref="TypeMarshallerCache"/>.
    /// </summary>
    [ThreadSafeImmutable]
    public interface ITypeMarshallerProvider
    {
        /// <summary>
        /// Return false if it doesn't handle it. 
        /// A single ITypeMarshaller can be created once per type and then reused for each instance. 
        /// Pass in a cache for aggregate types that need to marshal sub types.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="cache"></param>
        /// <param name="maxDepth"></param>
        /// <param name="marshaller"></param>
        /// <returns></returns>
        public bool TryGetMarshaller(Type type, TypeMarshallerCache cache, int maxDepth, out ITypeMarshaller marshaller);
    }

    /// <summary>
    /// A marshaller for a given System.Type to a given power fx type. .
    /// This can only marshal types that have a static mapping to a FormulaType.
    /// </summary>
    [ThreadSafeImmutable]
    public interface ITypeMarshaller
    {
        public FormulaType Type { get; }

        /// <summary>
        /// Marshal an dotnet object instance to a FormulaValue or throws on error. 
        /// If value is null, then this returns a blank value. 
        /// </summary>
        /// <param name="value">an object instance. </param>
        /// <returns>a formulaValue for this instance of type <see cref="Type"/>.</returns>
        /// <remarks>
        /// Implementor can assume that:
        /// - value is not null. The cache wrapper it in a <see cref="TypeMarshallerCache.NullCheckerMarshaller"/>
        /// - the value matches the type check in the provider.
        /// </remarks>
        public FormulaValue Marshal(object value);
    }

    /// <summary>
    /// Try to marshal a dynamic type such as a Dictionary, JObject, or DataTable.
    /// Unlike <see cref="ITypeMarshaller"/>, the static type does not infer the runtime FormulaType. 
    /// </summary>
    [ThreadSafeImmutable]
    public interface IDynamicTypeMarshaller
    {
        bool TryMarshal(TypeMarshallerCache cache, object value, out FormulaValue result);
    }
}
