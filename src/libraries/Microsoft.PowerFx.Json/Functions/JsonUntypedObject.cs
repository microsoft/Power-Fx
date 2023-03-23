// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    internal class JsonUntypedObject
    {
        public static IUntypedObject New(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Object => new JsonObject(element),
                JsonValueKind.Array => new JsonArray(element),
                _ => new JsonValue(element)
            };
        }
    }

    internal class JsonObject : UntypedPropertyBag
    {
        private readonly JsonElement _element;

        internal JsonObject(JsonElement je)
        {
            _element = je;
        }

        public override string[] PropertyNames => _element.EnumerateObject().Select(o => o.Name).ToArray();

        public override bool IsBlank()
        {
            return !_element.EnumerateObject().Any();
        }

        public override bool TryGetProperty(string value, out IUntypedObject result)
        {
            var res = _element.TryGetProperty(value, out JsonElement je);
            result = JsonUntypedObject.New(je);
            return res;
        }
    }

    internal class JsonArray : UntypedArray
    {
        private readonly JsonElement _element;

        internal JsonArray(JsonElement je)
        {
            _element = je;
        }

        public override IUntypedObject this[int index] => JsonUntypedObject.New(_element[index]);

        public override int Length => _element.GetArrayLength();

        public override bool IsBlank()
        {
            return false;
        }
    }

    internal class JsonValue : UntypedValue
    {
        internal JsonValue(JsonElement je)
            : base(je.ValueKind switch
            {
                JsonValueKind.Null => FormulaValue.NewBlank(),
                JsonValueKind.True => FormulaValue.New(true),
                JsonValueKind.False => FormulaValue.New(false),
                JsonValueKind.String => FormulaValue.New(je.GetString()),
                JsonValueKind.Undefined => FormulaValue.NewBlank(),
                JsonValueKind.Number => FormulaValue.New(je.GetDouble()),                
                _ => null
            })
        { 
        }
    }
}
