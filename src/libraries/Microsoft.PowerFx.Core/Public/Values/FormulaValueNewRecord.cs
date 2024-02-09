// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    // Helpers for creating tables from records.
    // For marshalling a dotnet object (T) to a Table, see TypeMarshallerCache.. 
    public partial class FormulaValue
    {
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
            var type = RecordType.Empty();
            var fieldArray = fields.ToArray();

            foreach (var field in fieldArray)
            {
                type = type.Add(new NamedFormulaType(field.Name, field.Value.IRContext.ResultType));
            }

            return NewRecordFromFields(type, fieldArray);
        }

        public static RecordValue NewRecordFromFields(RecordType recordType, params NamedValue[] fields)
        {
            return new InMemoryRecordValue(IRContext.NotInSource(recordType), fields);
        }

        public static RecordValue NewRecordFromFields(RecordType recordType, IEnumerable<NamedValue> fields)
        {
            return new InMemoryRecordValue(IRContext.NotInSource(recordType), fields);
        }
    }
}
