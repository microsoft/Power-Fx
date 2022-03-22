﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;

namespace Microsoft.PowerFx.Core.Public.Values
{
    // Marshalling a tables. 
    // Tables need to know their record type.
    // - a ITypeMarshaller  (which can be obtained from a TypeMarshallerCache and a T)
    // - an explicit RecordType
    public partial class FormulaValue
    {
        #region Host Records API

        /// <summary>
        /// Create a record by reflecting over the object's public properties.
        /// </summary>
        /// <typeparam name="T">static type to reflect over.</typeparam>
        /// <param name="obj"></param>
        /// <returns>a new record value.</returns>
        public static RecordValue NewRecord<T>(T obj, TypeMarshallerCache cache = null)
        {
            return NewRecord(obj, typeof(T), cache);
        }

        public static RecordValue NewRecord(object obj, Type type, TypeMarshallerCache cache = null)
        {
            if (cache == null)
            {
                cache = new TypeMarshallerCache();
            }

            var value = (RecordValue)cache.Marshal(obj, type);
            return value;
        }

        /// <summary>
        /// Create a record from the list of fields provided. 
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public static RecordValue NewRecordFromFields(params NamedValue[] fields)
        {
            return NewRecordFromFields(fields.AsEnumerable());
        }

        public static RecordValue NewRecordFromFields(IEnumerable<NamedValue> fields)
        {
            var type = new RecordType();
            foreach (var field in fields)
            {
                type = type.Add(new NamedFormulaType(field.Name, field.Value.IRContext.ResultType));
            }

            return NewRecordFromFields(type, fields);
        }

        public static RecordValue NewRecordFromFields(RecordType recordType, params NamedValue[] fields)
        {
            return NewRecordFromFields(recordType, fields.AsEnumerable());
        }

        public static RecordValue NewRecordFromFields(RecordType recordType, IEnumerable<NamedValue> fields)
        {
            return new InMemoryRecordValue(IRContext.NotInSource(recordType), fields);
        }
        #endregion
    }
}
