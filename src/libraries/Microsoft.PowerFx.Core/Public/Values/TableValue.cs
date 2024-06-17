// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Functions;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// Implemented by a <see cref="TableValue"/> if it supports delegation.
    /// </summary>
    public interface IDelegatableTableValue
    {
        /// <summary>
        /// Evaluation will invoke this method on delegated calls.
        /// </summary>
        /// <param name="services">Per-eval services.</param>
        /// <param name="parameters">delegation parameters.</param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task<IReadOnlyCollection<DValue<RecordValue>>> GetRowsAsync(IServiceProvider services, DelegationParameters parameters, CancellationToken cancel);
    }

    /// <summary>
    /// Represents a table (both single columna and multi-column). 
    /// </summary>
    public abstract class TableValue : ValidFormulaValue
    {
        /// <summary>
        /// Often marshalling an array will create a Single Column Tables with a single "Value" column. 
        /// </summary>
        public const string ValueName = "Value";

        /// <summary>
        /// DName for ValueName.
        /// </summary>
        public static readonly DName ValueDName = new DName(ValueName);

        public abstract IEnumerable<DValue<RecordValue>> Rows { get; }

        public bool IsColumn => IRContext.ResultType._type.IsColumn;

        public new TableType Type => (TableType)base.Type;

        /// <summary>
        /// Casts <paramref name="record"/> to the table's record type.
        /// </summary>
        /// <param name="record"> Record to cast.</param>
        /// <param name="cancellationToken"></param>
        public virtual DValue<RecordValue> CastRecord(RecordValue record, CancellationToken cancellationToken)
        {
            if (record.Type == Type.ToRecord())
            {
                return DValue<RecordValue>.Of(record);
            }

            var error = new ErrorValue(IRContext, new ExpressionError()
            {
                Span = IRContext.SourceContext,
                Kind = ErrorKind.InvalidArgument,
                ResourceKey = TexlStrings.InvalidCast,
                MessageArgs = new object[] { record.Type, Type.ToRecord() }
            });

            return DValue<RecordValue>.Of(error);
        }

        public TableValue(RecordType recordType)
            : this(IRContext.NotInSource(recordType.ToTable()))
        {
        }

        public TableValue(TableType type)
            : this(IRContext.NotInSource(type))
        {
        }

        internal TableValue(IRContext irContext)
            : base(irContext)
        {
            Contract.Assert(IRContext.ResultType is TableType);
        }

        public virtual int Count()
        {
            return Rows.Count();
        }

        /// <summary>
        /// Lookup the record at the given 1-based index, or return an error value if out of range.
        /// </summary>
        /// <param name="index1">1-based index.</param>
        /// <returns>The record or an errorValue.</returns>
        public DValue<RecordValue> Index(int index1)
        {
            if (TryGetIndex(index1, out var record))
            {
                return record;
            }

            return DValue<RecordValue>.Of(ArgumentOutOfRangeError(IRContext));
        }

        /// <summary>
        /// With mutation support, lookup the record at the given 1-based index, or return an error value if out of range.
        /// </summary>
        /// <param name="index1">1-based index.</param>
        /// <param name="mutationCopy">copies the element, and the table entry pointing to it, when in a mutation context.</param>
        /// <returns>The record or an errorValue.</returns>
        public DValue<RecordValue> Index(int index1, bool mutationCopy)
        {
            if (TryGetIndex(index1, out var record, mutationCopy))
            {
                return record;
            }

            return DValue<RecordValue>.Of(ArgumentOutOfRangeError(IRContext));
        }

        // Index() does standard error messaging and then call TryGetIndex().
        // Can't mutate through this entry point.
        // It is OK to just override this overload if the table is not mutable.
        protected virtual bool TryGetIndex(int index1, out DValue<RecordValue> record)
        {
            var index0 = index1 - 1;
            if (index0 < 0)
            {
                record = null;
                return false;
            }

            record = Rows.ElementAtOrDefault(index0);
            return record != null;
        }

        // Index() does standard error messaging and then call TryGetIndex().
        // This needs to be overriden to support mutation.
        protected virtual bool TryGetIndex(int index1, out DValue<RecordValue> record, bool mutationCopy)
        {
            if (!mutationCopy)
            {
                return TryGetIndex(index1, out record);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Lookup the first record, or return blank if the table is empty.
        /// </summary>
        /// <param name="mutationCopy">copies the element, and the table entry pointing to it, when in a mutation context.</param>
        /// <returns>The record or blank.</returns>
        public virtual DValue<RecordValue> First(bool mutationCopy = false)
        {
            if (mutationCopy)
            {
                return DValue<RecordValue>.Of(ImmutableTableError(IRContext));
            }

            return Rows.FirstOrDefault() ?? DValue<RecordValue>.Of(FormulaValue.NewBlank());
        }

        /// <summary>
        /// Lookup the last record, or return blank if the table is empty.
        /// </summary>
        /// <param name="mutationCopy">copies the element, and the table entry pointing to it, when in a mutation context.</param>
        /// <returns>The record or blank.</returns>
        public virtual DValue<RecordValue> Last(bool mutationCopy = false)
        {
            if (mutationCopy)
            {
                return DValue<RecordValue>.Of(ImmutableTableError(IRContext));
            }

            return Rows.LastOrDefault() ?? DValue<RecordValue>.Of(FormulaValue.NewBlank());
        }

        private static ErrorValue ImmutableTableError(IRContext irContext)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = "Table is immutable",
                Span = irContext.SourceContext,
                Kind = ErrorKind.InvalidArgument
            });
        }

        private static ErrorValue ArgumentOutOfRangeError(IRContext irContext)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = "Argument out of range",
                Span = irContext.SourceContext,
                Kind = ErrorKind.InvalidArgument
            });
        }

        private static ErrorValue NotImplementedError(IRContext irContext, [CallerMemberName] string methodName = null)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = $"{methodName} is not supported on this table instance.",
                Span = irContext.SourceContext,
                Kind = ErrorKind.Internal
            });
        }

        // Return appended value 
        // - Error, 
        // - with updated values
        // Async because derived classes may back this with a network call. 
        public virtual async Task<DValue<RecordValue>> AppendAsync(RecordValue record, CancellationToken cancellationToken)
        {
            return DValue<RecordValue>.Of(NotImplementedError(IRContext));
        }

        public virtual async Task<DValue<BooleanValue>> RemoveAsync(IEnumerable<FormulaValue> recordsToRemove, bool all, CancellationToken cancellationToken)
        {
            return DValue<BooleanValue>.Of(NotImplementedError(IRContext));
        }

        public virtual async Task<DValue<BooleanValue>> ClearAsync(CancellationToken cancellationToken)
        {
            return DValue<BooleanValue>.Of(NotImplementedError(IRContext));
        }

        /// <summary>
        /// Patch implementation for derived classes.
        /// </summary>
        /// <param name="baseRecord">A record to modify.</param>
        /// <param name="changeRecord">A record that contains properties to modify the base record. All display names are resolved.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns></returns>
        protected virtual async Task<DValue<RecordValue>> PatchCoreAsync(RecordValue baseRecord, RecordValue changeRecord, CancellationToken cancellationToken)
        {
            return DValue<RecordValue>.Of(NotImplementedError(IRContext));
        }

        /// <summary>
        /// Patch single record implementation for derived classes.
        /// </summary>
        /// <returns></returns>
        protected virtual async Task<DValue<RecordValue>> PatchSingleRecordCoreAsync(RecordValue recordValue, CancellationToken cancellationToken)
        {
            return DValue<RecordValue>.Of(CommonErrors.NotYetImplementedError(IRContext, "Patch single record is invalid for tables/records with no primary key."));
        }

        /// <summary>
        /// Modifies one record in a data source.
        /// </summary>
        /// <param name="baseRecord">A record to modify.</param>
        /// <param name="changeRecord">A record that contains properties to modify the base record.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated record.</returns>
        public async Task<DValue<RecordValue>> PatchAsync(RecordValue baseRecord, RecordValue changeRecord, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // IR has already resolved to logical names because of 
            // RequiresDataSourceScope, ArgMatchesDatasourceType on function.
            return await PatchCoreAsync(baseRecord, changeRecord, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Modifies a single record based on primary key within the record itself.
        /// </summary>
        /// <param name="recordValue">Record containing a primary key and fields to update.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns></returns>
        public async Task<DValue<RecordValue>> PatchAsync(RecordValue recordValue, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await PatchSingleRecordCoreAsync(recordValue, cancellationToken).ConfigureAwait(false);
        }

        public override object ToObject()
        {
            if (IsColumn)
            {
                var array = Rows.Select(async val =>
                {
                    if (val.IsValue)
                    {
                        await foreach (var field in val.Value.GetFieldsAsync(CancellationToken.None).ConfigureAwait(false))
                        {
                            return field.Value.ToObject();
                        }

                        return null;
                    }
                    else if (val.IsBlank)
                    {
                        return val.Blank.ToObject();
                    }
                    else
                    {
                        return val.Error.ToObject();
                    }
                }).ToArray();
                Task.WaitAll(array);
                return array.Select(tsk => tsk.Result).ToArray();
            }
            else
            {
                var array = Rows.Select(val =>
                {
                    if (val.IsValue)
                    {
                        return val.Value.ToObject();
                    }
                    else if (val.IsBlank)
                    {
                        return val.Blank.ToObject();
                    }
                    else
                    {
                        return val.Error.ToObject();
                    }
                }).ToArray();
                return array;
            }
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings)
        {
            // Table() is not legal, so we need an alternate expression to capture the table's type.
            if (!Rows.Any())
            {
                if (settings.UseCompactRepresentation)
                {
                    sb.Append("Table()");

                    return;
                }

                sb.Append("FirstN(");
                Type.DefaultExpressionValue(sb);
                sb.Append(",0)");
            }
            else
            {
                var flag = true;

                sb.Append("Table(");

                foreach (var row in Rows)
                {
                    if (!flag)
                    {
                        sb.Append(",");
                    }

                    flag = false;

                    row.ToFormulaValue().ToExpression(sb, settings);
                }

                sb.Append(")");
            }
        }

        public virtual int GetRowsHashCode()
        {
            throw new NotImplementedException();
        }
    }
}
