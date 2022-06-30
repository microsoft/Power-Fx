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

        /// <inheritdoc/>
        public bool TryGetMarshaller(Type type, TypeMarshallerCache cache, int maxDepth, out ITypeMarshaller marshaller)
        {        
            if (!type.IsClass || 
                typeof(FormulaValue).IsAssignableFrom(type) ||
                typeof(FormulaType).IsAssignableFrom(type))
            {
                // Explicitly reject FormulaValue/FormulaType to catch common bugs. 
                marshaller = null;
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

            marshaller = new ObjectMarshaller(fxType, mapping);
            return true;
        }      
    }
}
