// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    internal class JsonUntypedObject : UntypedObjectBase
    {
        private readonly JsonElement _element;

        public JsonUntypedObject(JsonElement element)
            : base(GetCapabilities(element))
        {
            _element = element;           
        }

        private static UntypedObjectCapabilities GetCapabilities(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => UntypedObjectCapabilities.SupportsString,
                JsonValueKind.Number => UntypedObjectCapabilities.SupportsDouble,
                JsonValueKind.True => UntypedObjectCapabilities.SupportsBoolean,
                JsonValueKind.False => UntypedObjectCapabilities.SupportsBoolean,
                JsonValueKind.Array => UntypedObjectCapabilities.SupportsArray,
                JsonValueKind.Object => UntypedObjectCapabilities.SupportsProperties,
                JsonValueKind.Null => UntypedObjectCapabilities.SupportsString,                
                _ => throw new NotImplementedException() // JsonValueKind.Undefined
            };
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
                        return FormulaType.Number;
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        return FormulaType.Boolean;
                    case JsonValueKind.Null:
                        return FormulaType.Blank;
                }

                return FormulaType.Blank;
            }
        }

        public override bool IsBlank()
        {
            return Type == FormulaType.Blank;
        }

        public override UntypedObjectBase IndexOf(int index) => new JsonUntypedObject(_element[index]);

        public override int ArrayLength()
        {
            return _element.GetArrayLength();
        }

        public override double AsDouble()
        {
            return _element.GetDouble();
        }

        public override string AsString()
        {
            return _element.GetString();
        }

        public override bool AsBoolean()
        {
            return _element.GetBoolean();
        }

        public override UntypedObjectBase GetProperty(string propertyName)
        {                        
            return new JsonUntypedObject(_element.GetProperty(propertyName));            
        }

        public override string[] PropertyNames()
        {
            return _element.EnumerateObject().Select(je => je.Name).ToArray();
        }
    }
}
