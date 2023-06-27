// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Types
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
        public IEnumerable<NamedValue> Fields => GetFields();

        /// <summary>
        /// Unique key associated to each record in application.
        /// NOTE: If two table has a same record instance, then the key should be same.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual bool TryGetPrimaryKey(out string key)
        {
            key = default;
            return false;
        }

        /// <summary>
        /// Returns the name of the primary key, null if there is no primary key.
        /// </summary>
        /// <returns>Primary key name. Returns null in absence.</returns>
        public virtual string GetPrimaryKeyName()
        {
            return null;
        }

        private IEnumerable<NamedValue> GetFields()
        {
            foreach (var fieldName in Type.FieldNames)
            {
                // Since fieldName is being enumerated from Type, backing type should alway be found and below will always succeed.
                Type.TryGetBackingDType(fieldName, out var backingDType);
                yield return new NamedValue(fieldName, async () => GetField(fieldName), backingDType);
            }
        }

        public async IAsyncEnumerable<NamedValue> GetFieldsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var fieldName in Type.FieldNames)
            {
                // below will always succeed.
                Type.TryGetBackingDType(fieldName, out var backingDType);
                Func<Task<FormulaValue>> getFormulaValue = async () => await GetFieldAsync(fieldName, cancellationToken).ConfigureAwait(false);
                yield return new NamedValue(fieldName, getFormulaValue, backingDType);
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
            var type = RecordType.Empty();
            return new InMemoryRecordValue(IRContext.NotInSource(type), new Dictionary<string, FormulaValue>());
        }

        public FormulaValue GetField(string fieldName)
        {
            return GetFieldAsync(fieldName, CancellationToken.None).Result;
        }

        /// <summary>
        /// Get a field on this record.         
        /// </summary>
        /// <param name="fieldName">Name of field on this record.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Field value or blank if missing. </returns>
        public async Task<FormulaValue> GetFieldAsync(string fieldName, CancellationToken cancellationToken)
        {
            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            if (!Type.TryGetFieldType(fieldName, out var fieldType))
            {
                fieldType = FormulaType.Blank;
            }

            return await GetFieldAsync(fieldType, fieldName, cancellationToken).ConfigureAwait(false);
        }

        // Create an exception object for when the host violates the TryGetField() contract. 
        private Exception HostException(string fieldName, string message)
        {
            return new InvalidOperationException($"{GetType().Name}.TryGetField({fieldName}): {message}");
        }

        // Internal, for already verified values.
        internal FormulaValue GetField(FormulaType fieldType, string fieldName)
        {
            return GetFieldAsync(fieldType, fieldName, CancellationToken.None).Result;
        }

        internal async Task<FormulaValue> GetFieldAsync(FormulaType fieldType, string fieldName, CancellationToken cancellationToken)
        {
            var (res, result) = await TryGetFieldAsync(fieldType, fieldName, cancellationToken).ConfigureAwait(false);
            if (res)
            {
                if (result == null)
                {
                    throw HostException(fieldName, $"returned true but null.");
                }

                // Ensure that type is properly projected. 
                if (result is RecordValue recordValue)
                {
                    if (fieldType._type == DType.Polymorphic)
                    {
                        return result;
                    }

                    var compileTimeType = (RecordType)fieldType;
                    result = CompileTimeTypeWrapperRecordValue.AdjustType(compileTimeType, recordValue);
                }
                else if (result is TableValue tableValue)
                {
                    result = CompileTimeTypeWrapperTableValue.AdjustType((TableType)fieldType, tableValue);
                }
                else
                {
                    // Ensure that the actual type matches the expected type.
                    if (!result.Type.Equals(fieldType))
                    {
                        if (result is not ErrorValue && result.Type is not BlankType && fieldType is not BlankType)
                        {
                            throw HostException(fieldName, $"Wrong field type. Returned {result.Type._type}, expected {fieldType._type}.");
                        }
                    }
                }

                Contract.Assert(result.Type.Equals(fieldType) || result is ErrorValue || result.Type is BlankType || fieldType is BlankType);

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
        
        protected virtual Task<(bool Result, FormulaValue Value)> TryGetFieldAsync(FormulaType fieldType, string fieldName, CancellationToken cancellationToken)
        {
            var b = TryGetField(fieldType, fieldName, out FormulaValue result);
            return Task.FromResult((b, result));
        }

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

        public DValue<RecordValue> UpdateField(string name, FormulaValue value)
        {
            var list = new List<NamedValue>() { new NamedValue(name, value) };

            return UpdateFields(FormulaValue.NewRecordFromFields(list));
        }

        public DValue<RecordValue> UpdateFields(RecordValue changeRecord)
        {
            return UpdateFieldsAsync(changeRecord, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public virtual async Task<DValue<RecordValue>> UpdateFieldsAsync(RecordValue changeRecord, CancellationToken cancellationToken)
        {
            var errorValue = NewError(new ExpressionError()
            {
                Kind = ErrorKind.ReadOnlyValue,
                Severity = ErrorSeverity.Critical,
                Message = "It is not possible to update a RecordValue directly."
            });

            return DValue<RecordValue>.Of(errorValue);
        }

        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings)
        {
            var flag = true;

            sb.Append("{");

            // Deterministic. Printing fields in order.
            var fields = Fields.ToArray();
            Array.Sort(fields, (a, b) => string.CompareOrdinal(a.Name, b.Name));

            foreach (var field in fields)
            {
                if (!flag)
                {
                    sb.Append(",");
                }

                flag = false;

                sb.Append(this.ToExpressionField(field.Name));
                sb.Append(':');

                field.Value.ToExpression(sb, settings);
            }

            sb.Append("}");
        }

        protected string ToExpressionField(string tableFieldName)
        {
            var fieldName = IdentToken.MakeValidIdentifier(tableFieldName);

            if ((TexlLexer.IsKeyword(fieldName, out _) || TexlLexer.IsReservedKeyword(fieldName)) &&
                !fieldName.StartsWith("'", StringComparison.Ordinal) && !fieldName.EndsWith("'", StringComparison.Ordinal))
            {
                fieldName = $"'{fieldName}'";
            }

            return fieldName;
        }

        /// <summary>
        /// It is assumed that all records can be copied to an InMemoryRecordValue during a mutation copy-on-write.
        /// This is possible for records, which will have a finite number of fields, but not for tables
        /// and number of rows which could be unbounded.
        /// </summary>
        public override bool TryShallowCopy(out FormulaValue copy)
        {
            copy = new InMemoryRecordValue(this.IRContext, this.Fields);
            return true;
        }
    }

    /// <summary>
    /// Copy a single record field and shallow copy contents, used during mutation copy-on-write.
    /// For example: Set( aa, [[1,2,3], [4,5,6]] ); Set( ab, First(aa) ); Patch( ab.Value, {Value:2}, {Value:9});
    /// No copies are made until the mutation in Patch, and then copies are made as the first argument's 
    /// value is traversed through EvalVisitor:
    /// 1. ab (record) shallow copies the root record and dictionary which references fields with IMutationCopy.
    /// 2. .Value (field) is copied with IMutationCopyField, which shallow copies the inner table with IMutationCopy.
    /// </summary>
    internal interface IMutationCopyField
    {
        /// <summary>
        /// Makes a shallow copy of a field within a record, in place, and does not return the copy.
        /// Earlier copies of the record will reference the original field.
        /// </summary>
        void ShallowCopyFieldInPlace(string fieldName);
    }
}
