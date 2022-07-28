using Microsoft.PowerFx.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PowerFXBenchmark.UntypedObjects
{
    /// <summary>
    /// Copied from Power FX src code.
    /// </summary>
    public class JsonUntypedObject : IUntypedObject
    {
        private readonly JsonElement _element;

        public JsonUntypedObject(JsonElement element)
        {
            _element = element;
        }

        public FormulaType Type
        {
            get
            {
                switch (_element.ValueKind)
                {
                    case JsonValueKind.Object:
                        return ExternalType.ObjectType;
                    case JsonValueKind.Array:
                        return ExternalType.ArrayType;
                    case JsonValueKind.String:
                        return FormulaType.String;
                    case JsonValueKind.Number:
                        return FormulaType.Number;
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        return FormulaType.Boolean;
                    case JsonValueKind.Undefined:
                        break;
                    case JsonValueKind.Null:
                        break;
                }

                return FormulaType.Blank;
            }
        }

        public IUntypedObject this[int index] => new JsonUntypedObject(_element[index]);

        public int GetArrayLength()
        {
            return _element.GetArrayLength();
        }

        public double GetDouble()
        {
            return _element.GetDouble();
        }

        public string GetString()
        {
            return _element.GetString() ?? string.Empty;
        }

        public bool GetBoolean()
        {
            return _element.GetBoolean();
        }

        public bool TryGetProperty(string value, out IUntypedObject result)
        {
            var res = _element.TryGetProperty(value, out var je);
            result = new JsonUntypedObject(je);
            return res;
        }
    }

}
