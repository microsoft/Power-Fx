// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // PagedRecordValue is a RecordValue that can be paged through.
    // It is composed of the "nextLink" string and a table with the results
    internal class PagedRecordValue : RecordValue
    {
        public readonly int MaxRows;
        public readonly RecordValue CurrentPage;
        public readonly TableValue CurrentTable;
        public readonly string TableFieldName;

        // Method used to retrieve the next page
        public readonly Func<CancellationToken, Task<FormulaValue>> GetNextRecordAsync;

        public PagedRecordValue(RecordValue recordValue, Func<CancellationToken, Task<FormulaValue>> getNextRecordAsync, int maxRows)
            : base(recordValue.Type)
        {
            CurrentPage = recordValue;
            GetNextRecordAsync = getNextRecordAsync;
            MaxRows = maxRows;

            NamedValue tableProp = CurrentPage.Fields.FirstOrDefault((NamedValue nv) => nv.Value is TableValue) ?? throw new InvalidOperationException("PagedRecordValue must contain a table");
            TableFieldName = tableProp.Name;
            CurrentTable = tableProp.Value as TableValue;
        }

        protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
        {
            bool b;
            (b, result) = CurrentPage.TryGetFieldAsync(fieldType, fieldName, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

            if (fieldName == TableFieldName)
            {
                result = new PagedTableValue(this);
            }

            return b;
        }
    }

    internal class PagedTableValue : TableValue
    {
        public readonly PagedRecordValue PagedRecordValue;

        public int MaxRows => PagedRecordValue.MaxRows;

        public PagedTableValue(PagedRecordValue prv)
            : base(prv.CurrentTable.Type)
        {
            PagedRecordValue = prv;
        }

        public override IEnumerable<DValue<RecordValue>> Rows
        {
            get
            {
                int rowCount = 0;                
                PagedRecordValue currentRecord = PagedRecordValue;
                TableValue currentTable = currentRecord.CurrentTable;
               
                while (true)
                {
                    foreach (DValue<RecordValue> currentRow in currentTable.Rows)
                    {
                        rowCount++;
                        if (rowCount > MaxRows)
                        {
                            yield break;
                        }

                        yield return currentRow;
                    }

                    if (currentRecord == null)
                    {
                        yield break;
                    }

                    FormulaValue nextPage = currentRecord.GetNextRecordAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

                    if (nextPage is ErrorValue ev)
                    {
                        yield return DValue<RecordValue>.Of(ev);
                        yield break;
                    }

                    // nextRecord could be RecordValue (final page) or PagedRecordValue (with next link)
                    if (nextPage is not RecordValue nextRecord) 
                    {
                        yield return DValue<RecordValue>.Of(ErrorValue.NewError(new ExpressionError() { Kind = ErrorKind.Custom, Message = $"Invalid next page, type = {nextPage.GetType().Name}" }));
                        yield break;
                    }
                    
                    (bool b, FormulaValue nextTableValue) = nextRecord.TryGetFieldAsync(currentRecord.CurrentTable.Type, currentRecord.TableFieldName, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

                    if (!b || nextTableValue is not TableValue newTable)
                    {
                        yield break;
                    }
                    
                    currentTable = newTable;

                    // If this page has a nextLink, we'll receive a PagedTableValue otherwise it will be a TableValue (last page)
                    if (nextTableValue is PagedTableValue newPage)
                    {
                        currentRecord = newPage.PagedRecordValue;
                        currentTable = newPage.PagedRecordValue.CurrentTable;                                            
                    }
                    else
                    {                        
                        currentRecord = null;
                    }                                     
                }
            }
        }
    }
}
