// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;

namespace Microsoft.PowerFx.Core.Public.Values
{
    /// <summary>
    /// Represents a table (both single columna and multi-column). 
    /// </summary>
    public abstract class TableValue : ValidFormulaValue
    {
        public abstract IEnumerable<DValue<RecordValue>> Rows { get; }

        public bool IsColumn => IRContext.ResultType._type.IsColumn;

        internal TableValue(IRContext irContext)
            : base(irContext)
        {
            Contract.Assert(IRContext.ResultType is TableType);
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
