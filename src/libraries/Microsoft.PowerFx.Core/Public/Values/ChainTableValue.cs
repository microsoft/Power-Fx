// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

#pragma warning disable IDE0011
#pragma warning disable SA1503

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PowerFx.Types
{
    // Glue together multiple tables (with the same type).
    internal class ChainTableValue : TableValue
    {
        private readonly TableValue[] _tables;

        public override IEnumerable<DValue<RecordValue>> Rows
        {
            get
            {
                foreach (var tbl in _tables)
                {
                    foreach (var row in tbl.Rows)
                        yield return row;
                }
            }
        }

        public ChainTableValue(TableType type, TableValue[] tables)
            : base(type)
        {
            _tables = tables;
        }
    }
}
