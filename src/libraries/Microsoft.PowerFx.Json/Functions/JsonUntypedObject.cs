// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    internal class JsonUntypedObject : UntypedObjectBase
    {
        internal readonly JsonElement _element;

        public JsonUntypedObject(JsonElement element)
        {
            _element = element;
        }

        public override FormulaType Type
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
                        // Do not be tempted to use FormulaType.Number here.  JSON numbers can be interpreted as either
                        // a float or a decimal and connectors take advantage of this to interop with decimals in databases.
                        // Each place that uses this value needs to interpret it appropriately given the context and NumberIsFloat mode.
                        // In cases where the distinction is not important such as converting to Boolean, use Float as it can hold any Decimal value.
                        //
                        // The ECMA 404 2017 JSON spec states:
                        // JSON is agnostic about the semantics of numbers. In any programming language, there can be a variety of
                        // number types of various capacities and complements, fixed or floating, binary or decimal.That can make
                        // interchange between different programming languages difficult. JSON instead offers only the representation of
                        // numbers that humans use: a sequence of digits. All programming languages know how to make sense of digit
                        // sequences even if they disagree on internal representations. That is enough to allow interchange 
                        return ExternalType.UntypedNumber;
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        return FormulaType.Boolean;
                }

                return FormulaType.Blank;
            }
        }

        public override IUntypedObject this[int index] => new JsonUntypedObject(_element[index]);

        public override int GetArrayLength()
        {
            return _element.GetArrayLength();
        }

        public override double GetDouble()
        {
            return _element.GetDouble();
        }

        public override string GetString()
        {
            return _element.GetString();
        }

        public override bool GetBoolean()
        {
            return _element.GetBoolean();
        }

        public override decimal GetDecimal()
        {
            return _element.GetDecimal();
        }

        public override string GetUntypedNumber()
        {
            if (Type == ExternalType.UntypedNumber)
            {
                return _element.GetRawText();
            }
            else
            {
                throw new FormatException();
            }
        }

        public override bool TryGetProperty(string value, out IUntypedObject result)
        {
            var res = _element.TryGetProperty(value, out var je);
            result = new JsonUntypedObject(je);
            return res;
        }

        public override bool TryGetPropertyNames(out IEnumerable<string> result)
        {
            if (_element.ValueKind != JsonValueKind.Object)
            {
                result = null;
                return false;
            }

            result = _element.EnumerateObject().Select(x => x.Name);
            return true;
        }
    }
}
