using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Types;
using PowerFXBenchmark.Inputs.Models;
using PowerFXBenchmark.TypedObjects;

namespace PowerFXBenchmark
{
    public static class PowerFXJsonRecordExtensions
    {
        public static FormulaValue ToDefaultBlankFormulaValue(this JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null => FormulaValue.NewBlank(),
                JsonValueKind.Number => FormulaValue.New(element.GetDouble()),
                JsonValueKind.String => FormulaValue.New(element.GetString()),
                JsonValueKind.False => FormulaValue.New(false),
                JsonValueKind.True => FormulaValue.New(true),
                JsonValueKind.Object => RecordFromJsonObject(element),
                JsonValueKind.Array => TableFromJsonArray(element),
                _ => throw new NotImplementedException($"Unrecognized JsonElement {element.ValueKind}"),
            };
        }

        private static RecordValue RecordFromJsonObject(JsonElement element)
        {
            var list = new List<NamedValue>();
            var recordType = RecordType.Empty();
            foreach (JsonProperty item in element.EnumerateObject())
            {
                var name = item.Name;
                FormulaValue formulaValue = ToDefaultBlankFormulaValue(item.Value);
                list.Add(new NamedValue(name, formulaValue));
                recordType = recordType.Add(new NamedFormulaType(name, formulaValue.Type));
            }

            return FormulaValue.NewRecordFromFields(new DefaultBlankRecordType(recordType), list);
        }

        internal static TableValue TableFromJsonArray(JsonElement array)
        {
            var list = new List<RecordValue>();
            for (var i = 0; i < array.GetArrayLength(); i++)
            {
                var item = (RecordValue)ToDefaultBlankFormulaValue(array[i]);
                list.Add(item);
            }

            RecordType resultType = ((list.Count != 0) ? list[0].Type : RecordType.Empty());
            return FormulaValue.NewTable(resultType, list);
        }
    }
}
