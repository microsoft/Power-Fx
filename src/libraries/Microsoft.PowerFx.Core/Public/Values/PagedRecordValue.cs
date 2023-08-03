// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Public.Values
{
    // PagedRecordValue is a RecordValue that can be paged through.
    // It is composed of the "nextLink" string and a table with the results
    internal class PagedRecordValue : RecordValue
    {
        public RecordValue CurrentPage { get; private set; }

        // Method used to retrieve the next page
        public Func<FormulaValue> GetNextRecord { get; private set; }

        public int MaxRows { get; private set; }

        public PagedRecordValue(RecordValue recordValue, Func<FormulaValue> getNextRecord, int maxRows)
            : base(recordValue.Type)
        {
            CurrentPage = recordValue;
            GetNextRecord = getNextRecord;
            MaxRows = maxRows;
        }

        protected internal override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
        {
            bool b = CurrentPage.TryGetField(fieldType, fieldName, out result);

            // If we retrieve the table (via "value" field), we need to wrap it in a PagedTableValue
            if (fieldType is TableType && result is TableValue tv)
            {
                result = new PagedTableValue(tv, GetNextRecord, fieldType, fieldName, MaxRows);
            }

            return b;
        }
    }

    internal class PagedTableValue : TableValue
    {
        public TableValue CurrentTable { get; private set; }

        public Func<FormulaValue> GetNextRecord { get; private set; }

        public FormulaType FieldType { get; private set; }

        public string FieldName { get; private set; }

        public int MaxRows { get; private set; }

        public PagedTableValue(TableValue tableValue, Func<FormulaValue> getNextRecord, FormulaType fieldType, string fieldName, int maxRows)
            : base(tableValue.Type)
        {
            CurrentTable = tableValue;
            GetNextRecord = getNextRecord;
            FieldType = fieldType;
            FieldName = fieldName;
            MaxRows = maxRows;
        }

        public override IEnumerable<DValue<RecordValue>> Rows
        {
            get
            {
                int rowCount = 0;

                // Enumerate rows of the current page
                foreach (DValue<RecordValue> currentRow in CurrentTable.Rows)
                {
                    rowCount++;
                    if (rowCount > MaxRows)
                    {
                        break;
                    }

                    yield return currentRow;
                }                

                PagedTableValue ptv = this;

                while (ptv != null)
                {
                    if (rowCount > MaxRows)
                    {
                        break;
                    }

                    // Get the next page and if an error occurs, let's stop
                    if (ptv.GetNextRecord() is not RecordValue nextRecord)
                    {
                        break;
                    }

                    // We should always get a new record containing the next page
                    if (nextRecord.TryGetField(FieldType, FieldName, out FormulaValue nextTableValue) && nextTableValue is TableValue tv)
                    {
                        TableValue tv2 = tv;

                        // If this page has a nextLink, we'll receive a PagedTableValue otherwise it will be a TableValue (last page)
                        if (nextTableValue is PagedTableValue ptv2)
                        {
                            tv2 = ptv2.CurrentTable;
                            ptv = ptv2;
                        }
                        else
                        {
                            // After last page, we stop
                            ptv = null;
                        }

                        // Enumerate rows of the current 'next' page
                        foreach (DValue<RecordValue> currentRow in tv2.Rows)
                        {
                            rowCount++;

                            if (rowCount > MaxRows)
                            {
                                break;
                            }

                            yield return currentRow;
                        }
                    }
                    else
                    {
                        // This is dead code but we need to set ptv to null to avoid infinite loop
                        ptv = null;
                    }
                }
            }
        }
    }
}
