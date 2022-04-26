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
            return new InMemoryRecordValue(IRContext.NotInSource(type), new Dictionary<string, FormulaValue>());
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

        // Create an exception object for when the host violates the TryGetField() contract. 
        private Exception HostException(string fieldName, string message)
        {
            return new InvalidOperationException($"{GetType().Name}.TryGetField({fieldName}): {message}");
        }

        // Internal, for already verified values.
        internal FormulaValue GetField(FormulaType fieldType, string fieldName)
        {
            if (TryGetField(fieldType, fieldName, out var result))
            {
                if (result == null)
                {
                    throw HostException(fieldName, $"returned true but null.");
                }

                // Ensure that type is properly projected. 
                if (result is RecordValue recordValue)
                {
                    var compileTimeType = (RecordType)fieldType;
                    result = CompileTimeTypeWrapperRecordValue.AdjustType(compileTimeType, recordValue);
                }
                else if (result is TableValue tableValue)
                {
                    result = new InMemoryTableValue(IRContext.NotInSource(fieldType), tableValue.Rows);
                } 
                else
                {
                    // Ensure that the actual type matches the expected type.
                    if (!result.Type.Equals(fieldType))
                    {
                        if (result is not ErrorValue && result.Type is not BlankType)
                        {
                            throw HostException(fieldName, $"Wrong field type. Retuned {result.Type._type}, expected {fieldType._type}.");
                        }
                    }
                }

                Contract.Assert(result.Type.Equals(fieldType) || result is ErrorValue || result.Type is BlankType);

                return result;
            } 
            else if (result != null)
            {
                throw HostException(fieldName, $"returned false with non-null result.");
            }

            // The binder can allow this if the record's static type contains fields not present in the runtime value.
            // This can happen with record unions: 
            //   First(Table({a:5}, {b:10})).b 
            //
            // Fx semantics are to return blank on missing fields. 
            return FormulaValue.NewBlank(fieldType);
        }

        /// <summary>
        /// Derived classes must override to provide values for fields. 
        /// </summary>
        /// <param name="fieldType">Expected type of the field.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="result"></param>
        /// <returns>true if field is present, else false.</returns>
        protected abstract bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result);

        /// <summary>
        /// Return an object, which can be used as 'dynamic' to fetch fields. 
        /// If this RecordValue was created around a host object, the host can override and return the source object.
        /// </summary>
        /// <returns></returns>
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
