// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerFx.Core
{
    /// <summary>
    /// Marshal .Net classes (with fields) to <see cref="RecordValue"/>. 
    /// This supports both strong typing and lazy marshalling. 
    /// It will return a <see cref="ObjectMarshaler"/>. 
    /// </summary>
    public class ObjectMarshallerProvider : ITypeMashallerProvider
    {
        /// <summary>
        /// Provides a customization point to control how properties are marshalled. 
        /// This returns null to skip the property, else return the name it should have on the power fx record.
        /// If this is insufficient, a caller can always implement their own marshaller and return a 
        /// a <see cref="ObjectMarshaler"/> directly. 
        /// </summary>
        public Func<PropertyInfo, string> PropertyMapperFunc = (propInfo) => propInfo.Name;

        /// <inheritdoc/>
        public bool TryGetMarshaller(Type type, TypeMarshallerCache cache, int maxDepth, out ITypeMarshaller marshaler)
        {        
            if (!type.IsClass || 
                typeof(FormulaValue).IsAssignableFrom(type) ||
                typeof(FormulaType).IsAssignableFrom(type))
            {
                // Explicitly reject FormulaValue/FormulaType to catch common bugs. 
                marshaler = null;
                return false;
            }

            var mapping = new Dictionary<string, Func<object, FormulaValue>>(StringComparer.OrdinalIgnoreCase);

            var fxType = new RecordType();

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead)
                {
                    continue;
                }

                var fxName = PropertyMapperFunc(prop);
                if (fxName == null)
                {
                    continue;
                }

                var tm = cache.GetMarshaller(prop.PropertyType, maxDepth);
                var fxFieldType = tm.Type;

                // Basic .net property
                if (mapping.ContainsKey(fxName))
                {
                    throw new NameCollisionException(fxName);
                }

                mapping[fxName] = (object objSource) =>
                {
                    var propValue = prop.GetValue(objSource);
                    return tm.Marshal(propValue);
                };

                fxType = fxType.Add(fxName, fxFieldType);
            }

            marshaler = new ObjectMarshaler(fxType, mapping);
            return true;
        }      
    }

    /// <summary>
    /// Marshal a specific type of object to a record. 
    /// </summary>
    [DebuggerDisplay("ObjMarshal({Type})")]
    public class ObjectMarshaler : ITypeMarshaller
    {
        // Map fx field name to a function produces the formula value given the dotnet object.
        private readonly IReadOnlyDictionary<string, Func<object, FormulaValue>> _mapping;

        public FormulaType Type { get; private set; }

        // FormulaType must be a record, and the dictionary provders getters for each field in that record. 
        public ObjectMarshaler(FormulaType type, IReadOnlyDictionary<string, Func<object, FormulaValue>> fieldMap)
        {
            if (!(type is RecordType))
            {
                throw new ArgumentException($"type must be a record, not ${type}");
            }

            Type = type;
            _mapping = fieldMap;
        }

        public FormulaValue Marshal(object source)
        {
            var value = new ObjectRecordValue(IRContext.NotInSource(Type), source, this);
            return value;
        }

        // Get the value of the field. 
        // Return null on missing
        public FormulaValue TryGetField(object source, string name)
        {
            if (_mapping.TryGetValue(name, out var getter))
            {
                var fieldValue = getter(source);
                return fieldValue;
            }

            return null;
        }

        public IEnumerable<NamedValue> GetFields(object source)
        {
            foreach (var kv in _mapping)
            {
                var fieldName = kv.Key;
                var getter = kv.Value;

                var value = getter(source);
                yield return new NamedValue(fieldName, value);
            }
        }
    }
}
