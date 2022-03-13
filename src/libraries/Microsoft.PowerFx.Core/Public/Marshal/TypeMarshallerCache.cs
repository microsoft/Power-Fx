// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerFx.Core
{
    /// <summary>
    /// Marshal .net objects into Power Fx values.  
    /// This allows customizing the marshallers, as well as caching the conversion rules for a given type. 
    /// </summary>
    public class TypeMarshallerCache
    {
        // Limit marshalling depth until we have full recusion support in type system.
        // https://github.com/microsoft/Power-Fx/issues/225
        public const int DefaultDepth = 3;

        // Map from a .net type to the marshaller for that type
        private readonly Dictionary<Type, ITypeMarshaller> _cache = new Dictionary<Type, ITypeMarshaller>();

        /// <summary>
        /// Ordered list of type marshallers. First marshaller to handle is used. 
        /// </summary>
        public List<ITypeMashallerProvider> Marshallers { get;  } = new List<ITypeMashallerProvider>
        { 
            new PrimitiveMarshallerProvider(),
            new TableMarshallerProvider(),
            new ObjectMarshallerProvider()
        };

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
                return new Empty();
            }

            // The ccahe requires an exact type match and doesn't handle base types.
            if (_cache.TryGetValue(type, out var tm))
            {
                return tm;
            }

            foreach (var marshaller in Marshallers)
            {
                if (marshaller.TryGetMarshaller(type, this, maxDepth - 1, out tm))
                {
                    tm = new NullCheckerMarshaler(tm);
                    _cache[type] = tm;
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
        /// <param name="value"></param>
        /// <returns></returns>
        public FormulaValue Marshal(object value, Type type = null)
        {
            if (value == null)
            {
                FormulaType fxType = null;
                if (type != null)
                {
                    fxType = GetMarshaller(type).Type;
                }

                return FormulaValue.NewBlank(fxType);                
            }

            if (value is FormulaValue fxValue)
            {
                return fxValue;
            }

            if (type == null)
            {
                type = value.GetType();
            }             

            var tm = GetMarshaller(value.GetType());
            return tm.Marshal(value);
        }

        public FormulaValue Marshal<T>(T value)
        {
            return Marshal(value, typeof(T));
        }

        private class Empty : ITypeMarshaller
        {
            public FormulaType Type => FormulaType.Blank;

            public FormulaValue Marshal(object value)
            {
                return RecordValue.Empty();
            }
        }

        // Wrapper to check for null and return blank. 
        // This lets us avoid every other ITypeMarshaler implementation having to do the same check.
        private class NullCheckerMarshaler : ITypeMarshaller
        {
            private readonly ITypeMarshaller _inner;

            public FormulaType Type => _inner.Type;

            public NullCheckerMarshaler(ITypeMarshaller inner)
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
