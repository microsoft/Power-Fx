// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Types
{
    internal static class DTypeSpecParser
    {
        private const string _typeEncodings = "?ebnshdipmgo$cDT!*%lLNZPQqV";
        private static readonly DType[] _types = new DType[]
        {
            DType.Unknown, DType.Error, DType.Boolean, DType.Number, DType.String, DType.Hyperlink,
            DType.DateTime, DType.Image, DType.PenImage, DType.Media, DType.Guid, DType.Blob, DType.Currency, DType.Color,
            DType.Date, DType.Time, DType.EmptyRecord, DType.EmptyTable, DType.EmptyEnum,
            DType.OptionSetValue, DType.OptionSet, DType.ObjNull, DType.DateTimeNoTimeZone, DType.Polymorphic, DType.View, DType.ViewValue,
            DType.NamedValue
        };

        // Parses a type specification, returns true and sets 'type' on success.
        public static bool TryParse(DTypeSpecLexer lexer, out DType type)
        {
            Contracts.AssertValue(lexer);
            Contracts.Assert(_typeEncodings.Length == _types.Length);
            Contracts.Assert(_typeEncodings.ToCharArray().Zip(_types, (c, t) => DType.MapKindToStr(t.Kind) == c.ToString()).All(x => x));

            string token;
            if (!lexer.TryNextToken(out token) || token.Length != 1)
            {
                type = DType.Invalid;
                return false;
            }

            // Older documents may use an "a" type, which for legacy reasons is a duplicate of "o" type.
            if (token == DType.MapKindToStr(DKind.LegacyBlob))
                token = DType.MapKindToStr(DKind.Blob);

            // Note that control types "v" or "E" are parsed to Error, since the type spec language is not a mechanism for serializing/deserializing controls.
            if (token == DType.MapKindToStr(DKind.Control) || token == DType.MapKindToStr(DKind.DataEntity))
            {
                type = DType.Error;
                return true;
            }

            int typeIdx = _typeEncodings.IndexOf(token);
            if (typeIdx < 0)
            {
                type = DType.Invalid;
                return false;
            }

            Contracts.AssertIndex(typeIdx, _types.Length);
            DType result = _types[typeIdx];

            if (result == DType.ObjNull)
            {
                // For null value
                type = result;
                return true;
            }

            if (!result.IsAggregate)
            {
                if (result.IsEnum)
                {
                    DType enumSupertype;
                    ValueTree valueMap;

                    if (!TryParse(lexer, out enumSupertype) ||
                        (!enumSupertype.IsPrimitive && !enumSupertype.IsUnknown) ||
                        !TryParseValueMap(lexer, out valueMap))
                    {
                        type = DType.Invalid;
                        return false;
                    }

                    // For enums
                    type = new DType(enumSupertype.Kind, valueMap);
                    return true;
                }

                // For non-enums, non-aggregates
                type = result;
                return true;
            }

            Contracts.Assert(result.IsRecord || result.IsTable);

            TypeTree typeMap;
            if (!TryParseTypeMap(lexer, out typeMap))
            {
                type = DType.Invalid;
                return false;
            }

            type = new DType(result.Kind, typeMap);
            return true;
        }

        // Parses a typed name map specification, returns true and sets 'map' on success.
        // A map specification has the form: [name:type, ...]
        private static bool TryParseTypeMap(DTypeSpecLexer lexer, out TypeTree map)
        {
            Contracts.AssertValue(lexer);

            string token;
            if (!lexer.TryNextToken(out token) || token != "[")
            {
                map = default(TypeTree);
                return false;
            }

            map = new TypeTree();

            while (lexer.TryNextToken(out token) && token != "]")
            {
                string name = token;
                if (name.Length >= 2 && name.StartsWith("'") && name.EndsWith("'"))
                    name = TexlLexer.UnescapeName(name);

                DType type;
                if (!DName.IsValidDName(name) ||
                    !lexer.TryNextToken(out token) ||
                    token != ":" ||
                    map.Contains(name) ||
                    !TryParse(lexer, out type))
                {
                    map = default(TypeTree);
                    return false;
                }

                map = map.SetItem(name, type);

                if (!lexer.TryNextToken(out token) || (token != "," && token != "]"))
                {
                    map = default(TypeTree);
                    return false;
                }
                else if (token == "]")
                    return true;
            }

            if (token != "]")
            {
                map = default(TypeTree);
                return false;
            }

            return true;
        }

        // Parses a value map specification, returns true and sets 'map' on success.
        // A map specification has the form: [name:value, ...]
        private static bool TryParseValueMap(DTypeSpecLexer lexer, out ValueTree map)
        {
            Contracts.AssertValue(lexer);

            string token;
            if (!lexer.TryNextToken(out token) || token != "[")
            {
                map = default(ValueTree);
                return false;
            }

            map = new ValueTree();

            while (lexer.TryNextToken(out token) && token != "]")
            {
                string name = token;
                if (name.Length >= 2 && name.StartsWith("'") && name.EndsWith("'"))
                    name = name.TrimStart('\'').TrimEnd('\'');

                EquatableObject value;
                if (!lexer.TryNextToken(out token) || token != ":" ||
                    !TryParseEquatableObject(lexer, out value))
                {
                    map = default(ValueTree);
                    return false;
                }

                map = map.SetItem(name, value);

                if (!lexer.TryNextToken(out token) || (token != "," && token != "]"))
                {
                    map = default(ValueTree);
                    return false;
                }
                else if (token == "]")
                    return true;
            }

            if (token != "]")
            {
                map = default(ValueTree);
                return false;
            }

            return true;
        }

        // Only primitive values are supported:
        //  - strings, such as "hello", etc.
        //  - numbers, such as 123.66124, etc.
        //  - booleans: true and false.
        private static bool TryParseEquatableObject(DTypeSpecLexer lexer, out EquatableObject value)
        {
            Contracts.AssertValue(lexer);

            string token;
            if (!lexer.TryNextToken(out token) || token.Length == 0)
            {
                value = default(EquatableObject);
                return false;
            }

            // String support
            if (token[0] == '"')
            {
                int tokenLen = token.Length;
                if (tokenLen < 2 || token[tokenLen - 1] != '"')
                {
                    value = default(EquatableObject);
                    return false;
                }
                value = new EquatableObject(token.Substring(1, tokenLen - 2));
                return true;
            }

            // Number (hex) support
            if (token[0] == '#' && token.Length > 1)
            {
                if (uint.TryParse(token.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint intValue))
                {
                    value = new EquatableObject((double)intValue);
                    return true;
                }

                value = default(EquatableObject);
                return false;
            }

            // Number (double) support
            double numValue;
            if (double.TryParse(token, out numValue))
            {
                value = new EquatableObject(numValue);
                return true;
            }

            // Boolean support
            bool boolValue;
            if (bool.TryParse(token, out boolValue))
            {
                value = new EquatableObject(boolValue);
                return true;
            }

            value = default(EquatableObject);
            return false;
        }
    }
}