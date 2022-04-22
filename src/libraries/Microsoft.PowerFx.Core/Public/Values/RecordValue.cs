// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Dynamic;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;

namespace Microsoft.PowerFx.Core.Public.Values
{
    /// <summary>
    /// Represent a Record. Records have named fields which can be other values. 
    /// </summary>
    public abstract class RecordValue : ValidFormulaValue
    {
        /// <summary>
        /// Fields and their values directly available on this record. 
        /// The field names should match the names on <see cref="Type"/>. 
        /// </summary>
        public IEnumerable<NamedValue> Fields
        {
            get
            {
                var fields = Type.GetNames();
                foreach (var kv in fields)
                {
                    var fieldName = kv.Name;
                    var fieldType = kv.Type;

                    var value = GetField(fieldType, fieldName);
                    yield return new NamedValue(fieldName, value);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecordValue"/> class.
        /// </summary>
        /// <param name="type"></param>
        public RecordValue(RecordType type) 
            : base(IRContext.NotInSource(type))
        {
        }

        internal RecordValue(IRContext irContext)
            : base(irContext)
        {
            Contract.Assert(IRContext.ResultType is RecordType);
        }

        /// <summary>
        /// The RecordType of this value. 
        /// </summary>
        public new RecordType Type => (RecordType)base.Type;

        public static RecordValue Empty()
        {
            var type = new RecordType();
            return new InMemoryRecordValue(IRContext.NotInSource(type), new List<NamedValue>());
        }

        /// <summary>
        /// Get a field on this record.         
        /// </summary>
        /// <param name="fieldName">Name of field on this record.</param>
        /// <returns>Field value or blank if missing. </returns>
        public FormulaValue GetField(string fieldName)
        {
            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            var fieldType = Type.MaybeGetFieldType(fieldName) ?? FormulaType.Blank;

            return GetField(fieldType, fieldName);
        }

        // Internal, for already verified values.
        internal FormulaValue GetField(FormulaType fieldType, string fieldName)
        {
            if (TryGetField(fieldType, fieldName, out var result))
            {
                if (result == null)
                {
                    throw new InvalidOperationException($"{GetType().Name}.TryGetField({fieldName}) returned true but null.");
                }

                Contract.Assert(result.Type.Equals(fieldType) || result is ErrorValue || result.Type is BlankType);

                return result;
            }

            // This should be a compiler-error. 
            // Fx semantics are to return blank on missing fields. 
            return FormulaValue.NewBlank(fieldType);
        }

        /// <summary>
        /// Derived classes can override. 
        /// </summary>
        /// <param name="fieldType"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        protected virtual bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
        {
            // Derived class can have more optimized lookup.
            foreach (var field in Fields)
            {
                if (fieldName == field.Name)
                {
                    result = field.Value;
                    return true;
                }
            }

            result = null;
            return false;
        }

        // Return an object, which can be used as 'dynamic' to fetch fields. 
        public override object ToObject()
        {
            var e = new ExpandoObject();
            IDictionary<string, object> dict = e;
            foreach (var field in Fields)
            {
                dict[field.Name] = field.Value?.ToObject();
            }

            return e;
        }

        public sealed override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
