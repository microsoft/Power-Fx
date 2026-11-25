using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Types;
using PowerFXBenchmark.Inputs.Models;
using PowerFXBenchmark.TypedObjects;

namespace PowerFXBenchmark
{
    public static class PowerFXRecordExtensions
    {
        public static RecordType ToRecordType(this TestObjectSchema schema)
        {
            var recordType = RecordType.Empty()
                .Add("id", FormulaType.String)
                .Add("sessionId", FormulaType.String);
            var metadataType = RecordType.Empty()
                .Add("time", FormulaType.DateTime)
                .Add("type", FormulaType.String);

            foreach (var nestedSchema in schema.NestedSchemas)
            {
                recordType = recordType.Add(nestedSchema.Key, ConvertTestObjSchemaToRecordType(nestedSchema.Value));
                metadataType = metadataType.Add(nestedSchema.Key, RecordType.Empty()
                    .Add("type", FormulaType.String)
                    .Add("time", FormulaType.DateTime));
            }

            recordType = recordType.Add("metadata", metadataType);


            return new DefaultBlankRecordType(recordType);
        }

        private static FormulaType ConvertTestObjSchemaToRecordType(ITestObjectBaseSchema schema)
        {
            return schema.Kind switch
            {
                TestObjDataKind.Array => ConvertArraySchemaToFormulaType((ArraySchema)schema),
                TestObjDataKind.Boolean => FormulaType.Boolean,
                TestObjDataKind.Date => FormulaType.Date,
                TestObjDataKind.DateTime => FormulaType.DateTime,
                TestObjDataKind.Double => FormulaType.Number,
                TestObjDataKind.Float => FormulaType.Number,
                TestObjDataKind.Integer => FormulaType.Number,
                TestObjDataKind.Long => FormulaType.Number,
                TestObjDataKind.Map => ConvertDtdlMapToRecordType((MapSchema)schema),
                TestObjDataKind.String => FormulaType.String,
                TestObjDataKind.Time => FormulaType.Time,
                _ => throw new NotImplementedException(),
            };
        }

        private static FormulaType ConvertArraySchemaToFormulaType(ArraySchema arraySchema)
        {
            var itemType = ConvertTestObjSchemaToRecordType(arraySchema.ArrayElementSchema);
            return TableType.Empty().Add("Value", itemType);
        }

        private static FormulaType ConvertDtdlMapToRecordType(MapSchema mapSchema)
        {
            var tableType = TableType.Empty()
                .Add(new NamedFormulaType("MapName", FormulaType.String))
                .Add(new NamedFormulaType("MapValue", ConvertTestObjSchemaToRecordType(mapSchema.MapElementSchema)));

            return tableType;
        }
    }
}
