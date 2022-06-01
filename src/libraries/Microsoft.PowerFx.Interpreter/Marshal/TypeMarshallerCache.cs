// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Marshal .net objects into Power Fx values.  
    /// This allows customizing the marshallers, as well as caching the conversion rules for a given type. 
    /// This is an immutable object representing a collection of immutable providers. 
    /// </summary>
    [ThreadSafeImmutable]
    public class TypeMarshallerCache
    {
        // Limit marshalling depth until we have full recusion support in type system.
        // https://github.com/microsoft/Power-Fx/issues/225
        public const int DefaultDepth = 3;

        // Map from a .net type to the marshaller for that type
        // Cache must be thread-safe. 
        [ThreadSafeProtectedByLock("_cache")]
        private readonly Dictionary<Type, ITypeMarshaller> _cache = new Dictionary<Type, ITypeMarshaller>();

        /// <summary>
        /// Empty type marshaller, without any defaults. 
        /// </summary>
        public static TypeMarshallerCache Empty { get; } = new TypeMarshallerCache(new ITypeMarshallerProvider[0]);

        private static readonly IEnumerable<ITypeMarshallerProvider> _defaults = NewList(new ObjectMarshallerProvider());

        /// <summary>
        /// Ordered list of type marshallers. First marshaller to handle is used. 
        /// </summary>
        private readonly IEnumerable<ITypeMarshallerProvider> _marshallers;

        private readonly IEnumerable<IDynamicTypeMarshaller> _dynamicMarshallers;

        // Take a private array to get a snapshot and ensure the enumeration doesn't change
        private TypeMarshallerCache(
            IEnumerable<ITypeMarshallerProvider> marshallers,
            IDynamicTypeMarshaller[] dynamicMarshallers = null)
        {
            _marshallers = marshallers;
            _dynamicMarshallers = dynamicMarshallers;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeMarshallerCache"/> class.
        /// Create marshaller with default list.
        /// </summary>
        public TypeMarshallerCache()
            : this(_defaults.ToArray(), null)
        {
        }     

        /// <summary>
        /// Create a new cache that includes the new providers and then chains to this cache.
        /// </summary>
        /// <param name="providers">list of providers.</param>
        /// <returns></returns>
        public TypeMarshallerCache NewPrepend(IEnumerable<ITypeMarshallerProvider> providers)
        {
            // The new providers are handled first, and will claim a type before the existing
            // provider has a chance. 
            var list = providers.Concat(_marshallers).ToArray();
            return new TypeMarshallerCache(list);
        }

        public TypeMarshallerCache NewPrepend(params ITypeMarshallerProvider[] providers)
        {
            IEnumerable<ITypeMarshallerProvider> list = providers;

            return NewPrepend(list);
        }

        /// <summary>
        /// Return a new cache that includes the given dynamic marshallers. 
        /// These will be invoked on <see cref="TypeMarshallerCache.Marshal{T}(T)"/>.
        /// </summary>
        /// <param name="dynamicMarshallers"></param>
        /// <returns></returns>
        public TypeMarshallerCache WithDynamicMarshallers(params IDynamicTypeMarshaller[] dynamicMarshallers)
        {
            if (dynamicMarshallers == null)
            {
                throw new ArgumentNullException(nameof(dynamicMarshallers));
            }

            return new TypeMarshallerCache(_marshallers, dynamicMarshallers);
        }

        private static ITypeMarshallerProvider[] NewList(ObjectMarshallerProvider objectProvider)
        {
            if (objectProvider == null)
            {
                throw new ArgumentNullException(nameof(objectProvider));
            }
                
            return new ITypeMarshallerProvider[]
            {
                new PrimitiveMarshallerProvider(),
                new TableMarshallerProvider(),
                objectProvider
            };
        }

        /// <summary>
        ///  Create a cache with the the default marshallers and the specified object marshaller.
        /// </summary>
        /// <param name="objectProvider"></param>
        /// <returns></returns>
        public static TypeMarshallerCache New(ObjectMarshallerProvider objectProvider)
        {
            return new TypeMarshallerCache(NewList(objectProvider));
        }

        /// <summary>
        /// Returns a marshaller for the given type. 
        /// </summary>
        /// <param name="type">dot net type to marshal.</param>
        /// <param name="maxDepth">maximum depth to marshal.</param>
        /// <returns>A marshaller instance that can marshal objects of the given type.</returns>
        public ITypeMarshaller GetMarshaller(Type type, int maxDepth = DefaultDepth)
        {
            if (maxDepth < 0)
            {
                return new EmptyMarshaller();
            }

            // The cache requires an exact type match and doesn't handle base types.
            ITypeMarshaller tm;

            lock (_cache)
            {
                if (_cache.TryGetValue(type, out tm))
                {
                    return tm;
                }
            }

            foreach (var marshaller in _marshallers)
            {
                if (marshaller.TryGetMarshaller(type, this, maxDepth - 1, out tm))
                {
                    tm = new NullCheckerMarshaller(tm);
                    lock (_cache)
                    {
                        _cache[type] = tm;
                    }

                    return tm;
                }
            }

            // Failed to marshal!
            throw new InvalidOperationException($"Can't marshal {type.FullName}");
        }

        /// <summary>
        /// Helper to marshal an arbitrary object to a FormulaValue. 
        /// This can use runtime checks (like null to blank), and then 
        /// just calls <see cref="GetMarshaller"/>. 
        /// </summary>
        /// <param name="value">the object instance to marshal.</param>
        /// <param name="type">The type to marshal as. For example, if this is a base type, then 
        /// the derived properties available at runtime are not marshalled.</param>
        /// <returns></returns>
        public FormulaValue Marshal(object value, Type type)
        {
            if (value is FormulaValue fxValue)
            {
                return fxValue;
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            // Object is just as bad null. Likely a hosting bug - host should provide specific type. 
            if (type == typeof(object))
            {
                throw new ArgumentException($"Must provide specific type");
            }

            // Dynamic marshallers can only act on the runtime value.
            if (_dynamicMarshallers != null)
            {
                foreach (var dynamicMarshaller in _dynamicMarshallers)
                {
                    if (dynamicMarshaller.TryMarshal(this, value, out var result))
                    {
                        return result;
                    }
                }
            }

            var tm = GetMarshaller(type);
            return tm.Marshal(value);
        }

        public FormulaValue Marshal<T>(T value)
        {
            // T will be the compile-time type, not necessarily the runtime type. 
            return Marshal(value, typeof(T));
        }

        private class EmptyMarshaller : ITypeMarshaller
        {
            public FormulaType Type => FormulaType.Blank;

            public FormulaValue Marshal(object value)
            {
                return RecordValue.Empty();
            }
        }

        // Wrapper to check for null and return blank. 
        // This lets us avoid every other ITypeMarshaller implementation having to do the same check.
        [DebuggerDisplay("{_inner}")]
        private class NullCheckerMarshaller : ITypeMarshaller
        {
            private readonly ITypeMarshaller _inner;

            public FormulaType Type => _inner.Type;

            public NullCheckerMarshaller(ITypeMarshaller inner)
            {
                _inner = inner;
            }

            public FormulaValue Marshal(object value)
            {
                if (value == null)
                {
                    return FormulaValue.NewBlank(Type);
                }

                var fxValue = _inner.Marshal(value);

                // debug check to ensure that the marshaller actually returns the right type.
                Contract.Assert(Type.GetType().IsAssignableFrom(fxValue.Type.GetType()));

                return fxValue;
            }
        }
    }
}
