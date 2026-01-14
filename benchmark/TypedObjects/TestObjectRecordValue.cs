using Microsoft.PowerFx.Types;
using Newtonsoft.Json.Linq;
using PowerFXBenchmark.Inputs.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PowerFXBenchmark.TypedObjects
{
    public class TestObjectRecordValue : RecordValue
    {
        public TestObject testObj;
        public MetadataRecordValue metadata;
        public TestObjectSchema schema;

        public TestObjectRecordValue(TestObject testObj, TestObjectSchema schema, RecordType recordType) : base(recordType)
        {
            this.testObj = testObj;
            this.schema = schema;
            metadata = new MetadataRecordValue(testObj.RootMetadata, schema, (RecordType)recordType.GetFieldType("metadata"));
        }

        public override object ToObject() => testObj;


        protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
        {
            result = FormulaValue.NewBlank(fieldType);

            // Metadata
            if (fieldName == "id")
            {
                result = New(testObj.Id);
                return true;
            }

            if(fieldName == "metadata")
            {
                result = metadata;
                return true;
            }


            if (schema.NestedSchemas.ContainsKey(fieldName) &&
                testObj.JTokenBag.TryGetValue(fieldName, out JToken? jtoken))
            {
                result = Helper.ConvertJTokenToFormulaValue(jtoken, schema.NestedSchemas[fieldName], () => Type.GetFieldType(fieldName));
                return true;
            }

            return true;
        }
    }

    public class MetadataRecordValue : RecordValue
    {
        public RootMetadata metadata;
        public TestObjectSchema schema;

        public MetadataRecordValue(RootMetadata metadata, TestObjectSchema schema, RecordType recordType) : base(recordType)
        {
            this.metadata = metadata;
            this.schema = schema;
        }

        public override object ToObject() => metadata;
        protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
        {
            result = FormulaValue.NewBlank(fieldType);

            if (metadata.PropertyMetadata.ContainsKey(fieldName))
            {
                result = NewRecordFromFields(
                    new NamedValue("time", New(DateTime.Parse(metadata.PropertyMetadata[fieldName].Time))),
                    new NamedValue("type", New(metadata.PropertyMetadata[fieldName].Type)));
                return true;
            }

            return false;
        }
    }

    public class PropertyMetadataRecordValue : RecordValue
    {
        public PropertyMetadata metadata;

        public PropertyMetadataRecordValue(PropertyMetadata metadata, RecordType recordType) : base(recordType)
        {
            this.metadata = metadata;
        }

        public override object ToObject() => metadata;
        protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
        {
            result = FormulaValue.NewBlank(fieldType);

            if (fieldName == "time")
            {
                result = New(DateTime.Parse(metadata.Time));
                return true;
            }

            if (fieldName == "type")
            {
                result = New(metadata.Type);
                return true;
            }

            return false;
        }
    }

    public static class Helper
    {

        public static FormulaValue ConvertJTokenToFormulaValue(JToken jtoken, ITestObjectBaseSchema schema, Func<FormulaType> getChildType)
        {
            return schema.Kind switch
            {
                TestObjDataKind.Array => Helper.ConvertJArrayToFormulaValue((JArray)jtoken, (ArraySchema)schema, (TableType)getChildType()),
                TestObjDataKind.Boolean => FormulaValue.New((bool)jtoken),
                TestObjDataKind.Date => FormulaValue.New(DateTime.Parse(jtoken.ToString()).Date),
                TestObjDataKind.DateTime => FormulaValue.New(DateTime.Parse(jtoken.ToString())),
                TestObjDataKind.Double => FormulaValue.New((double)jtoken),
                TestObjDataKind.Float => FormulaValue.New((float)jtoken),
                TestObjDataKind.Integer => FormulaValue.New((int)jtoken),
                TestObjDataKind.Long => FormulaValue.New((long)jtoken),
                TestObjDataKind.Map => Helper.ConvertJObjectToFormluaValue((JObject)jtoken, (MapSchema)schema, (TableType)getChildType()),
                TestObjDataKind.String => FormulaValue.New(jtoken.ToString()),
                TestObjDataKind.Time => FormulaValue.New(DateTime.Parse(jtoken.ToString()).TimeOfDay),
                _ => throw new NotImplementedException(),
            };
        }

        public static TableValue ConvertJArrayToFormulaValue(JArray jarray, ArraySchema arraySchema, TableType tableType)
        {
            return FormulaValue.NewTable(
                tableType.ToRecord(),
                jarray
                .AsJEnumerable()
                .Select(
                    jt => RecordValue.NewRecordFromFields(
                        new NamedValue(
                            tableType.FieldNames.First(),
                            ConvertJTokenToFormulaValue(
                                jt,
                                arraySchema.ArrayElementSchema,
                                () => tableType.GetFieldType(tableType.FieldNames.First())))))
                .ToArray());
        }

        public static TableValue ConvertJObjectToFormluaValue(JObject jobject, MapSchema mapInfo, TableType tableType)
        {
            //return new MapTableValue(tableType.ToRecord(), mapInfo, jobject.AsJEnumerable().ToDictionary(
            //keySelector: jt => ((JProperty)jt).Name, elementSelector: jt => ConvertJTokenToFormulaValue(((JProperty)jt).Value, mapInfo.MapValue.Schema, () => tableType.GetFieldType(mapInfo.MapValue.Name))));

            return FormulaValue.NewTable(
                tableType.ToRecord(),
                jobject.AsJEnumerable().Select(jt => FormulaValue.NewRecordFromFields(
                    new NamedValue("key", FormulaValue.New(((JProperty)jt).Name)),
                    new NamedValue("value", ConvertJTokenToFormulaValue(((JProperty)jt).Value, mapInfo.MapElementSchema.Value, () => tableType.GetFieldType(mapInfo.MapElementSchema.Name))))));
        }
    }
}
