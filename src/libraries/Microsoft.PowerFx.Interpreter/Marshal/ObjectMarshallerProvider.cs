// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Marshal .Net classes (with fields) to <see cref="RecordValue"/>. 
    /// This supports both strong typing and lazy marshalling. 
    /// It will return a <see cref="ObjectMarshaller"/>. 
    /// </summary>
    public class ObjectMarshallerProvider : ITypeMarshallerProvider
    {
        /// <summary>
        /// Provides a customization point to control how properties are marshalled. 
        /// This returns null to skip the property, else return the name it should have on the power fx record.
        /// If this is insufficient, a caller can always implement their own marshaller and return a 
        /// a <see cref="ObjectMarshaller"/> directly. 
        /// </summary>
        public virtual string GetFxName(PropertyInfo propertyInfo)
        {
            // By default, the C# name is the Fx name. 
            return propertyInfo.Name;
        }

        /// <summary>
        /// Provides customization point to control if this provider will handle the specific type.
        /// </summary>
        /// <param name="type">The type to decide to handle or not.</param>
        /// <returns></returns>
        public virtual bool CanHandleType(Type type)
        {
            return !(!(type.IsClass || type.IsInterface) ||
                typeof(FormulaValue).IsAssignableFrom(type) ||
                typeof(FormulaType).IsAssignableFrom(type));
        }

        /// <inheritdoc/>
        public bool TryGetMarshaller(Type type, TypeMarshallerCache cache, int maxDepth, out ITypeMarshaller marshaler)
        {
            if (!CanHandleType(type))
            {
                // Explicitly reject FormulaValue/FormulaType to catch common bugs. 
                marshaler = null;
                return false;
            }

            var mapping = new Dictionary<string, Func<object, FormulaValue>>(StringComparer.OrdinalIgnoreCase);

            var fxType = new RecordType();

            fxType = GetProperties(type, cache, maxDepth, mapping, fxType);

            marshaler = GetObjectMarshaller(fxType, mapping);

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fxType"></param>
        /// <param name="mapping"></param>
        /// <returns></returns>
        protected virtual ObjectMarshaller GetObjectMarshaller(RecordType fxType, Dictionary<string, Func<object, FormulaValue>> mapping)
        {
            return new ObjectMarshaller(fxType, mapping);
        }

        /// <summary>
        /// Provides customization point to control the properties this provider will find.
        /// </summary>
        /// <returns>The record type containing the properties.</returns>
        protected virtual RecordType GetProperties(Type type, TypeMarshallerCache cache, int maxDepth, Dictionary<string, Func<object, FormulaValue>> mapping, RecordType fxType)
        {
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead)
                {
                    continue;
                }

                var fxName = GetFxName(prop);
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

            return fxType;
        }
    }
}
