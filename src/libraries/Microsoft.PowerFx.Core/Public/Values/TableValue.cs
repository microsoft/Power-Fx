﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
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

            return DValue<RecordValue>.Of(ArgumentOutOfRange(IRContext));
        }

        // Index() does standard error messaging and then call TryGetIndex().
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

        private static ErrorValue ArgumentOutOfRange(IRContext irContext)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = "Argument out of range",
                Span = irContext.SourceContext,
                Kind = ErrorKind.Numeric
            });
        }

        // Return appended value 
        // - Error, 
        // - with updated values
        // Async because derived classes may back this with a network call. 
        public virtual async Task<DValue<RecordValue>> AppendAsync(RecordValue record)
        {
            // fails by default
            throw new System.NotImplementedException("It is not possible to append to a TableValue directly.");
        }

        public override object ToObject()
        {
            if (IsColumn)
            {
                var array = Rows.Select(val =>
                {
                    if (val.IsValue)
                    {
                        return val.Value.Fields.First().Value.ToObject();
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
    }
}
