// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.IR
{
    internal class CoercionMatrix
    {
        public static CoercionKind GetCoercionKind(DType fromType, DType toType)
        {
            Contracts.AssertValid(fromType);
            Contracts.AssertValid(toType);

            if (!fromType.IsAggregate && !fromType.IsOptionSet && !fromType.IsView && fromType == toType)
            {
                return CoercionKind.None;
            }

            if (fromType.IsAggregate && toType.Kind == DKind.DataEntity)
            {
                return CoercionKind.AggregateToDataEntity;
            }

            if (toType.IsLargeImage && (fromType.Kind == DKind.Image || fromType == DType.MinimalLargeImage))
            {
                if (fromType.Kind == DKind.Image)
                {
                    return CoercionKind.ImageToLargeImage;
                }
                else
                {
                    return CoercionKind.SingleColumnRecordToLargeImage;
                }
            }

            if (fromType.IsUntypedObject)
            {
                return GetUntypedObjectCoercion(toType);
            }

            return FlattenCoercionMatrix(fromType, toType);
        }

        private static CoercionKind FlattenCoercionMatrix(DType fromType, DType toType)
        {
            switch (toType.Kind)
            {
                case DKind.Number:
                case DKind.Currency:
                    return GetToNumberCoercion(fromType);

                case DKind.Color:
                case DKind.PenImage:
                    // It is not safe to coerce these.
                    Contracts.Assert(false, "Unsupported type coercion");
                    break;

                case DKind.Hyperlink:
                    if (DType.String.Accepts(fromType))
                    {
                        switch (fromType.Kind)
                        {
                            case DKind.Blob:
                                return CoercionKind.BlobToHyperlink;
                            case DKind.Image:
                                return CoercionKind.ImageToHyperlink;
                            case DKind.Media:
                                return CoercionKind.MediaToHyperlink;
                            default:
                                return CoercionKind.TextToHyperlink;
                        }
                    }

                    Contracts.Assert(false, "Unsupported type coercion");
                    break;

                case DKind.Image:
                    if (fromType.IsLargeImage)
                    {
                        return CoercionKind.LargeImageToImage;
                    }

                    if (fromType.Kind != DKind.Media && fromType.Kind != DKind.Blob && DType.String.Accepts(fromType))
                    {
                        return CoercionKind.TextToImage;
                    }

                    Contracts.Assert(false, "Unsupported type coercion");
                    break;

                case DKind.Media:
                    if (fromType.Kind != DKind.Image && fromType.Kind != DKind.Blob && DType.String.Accepts(fromType))
                    {
                        return CoercionKind.TextToMedia;
                    }

                    Contracts.Assert(false, "Unsupported type coercion");
                    break;

                case DKind.Blob:
                    if (DType.String.Accepts(fromType))
                    {
                        return CoercionKind.TextToBlob;
                    }

                    Contracts.Assert(false, "Unsupported type coercion");
                    break;

                case DKind.String:
                    return GetToStringCoercion(fromType);

                case DKind.Enum:
                    return GetToEnumCoercion(fromType, toType);

                case DKind.Boolean:
                    Contracts.Assert(DType.Number.Accepts(fromType) || DType.String.Accepts(fromType) || (DType.OptionSetValue.Accepts(fromType) && (fromType.OptionSetInfo?.IsBooleanValued ?? false)), "Unsupported type coercion");
                    if (DType.Number.Accepts(fromType))
                    {
                        return CoercionKind.NumberToBoolean;
                    }

                    if (DType.String.Accepts(fromType))
                    {
                        return CoercionKind.TextToBoolean;
                    }

                    if (DType.OptionSetValue.Accepts(fromType) && (fromType.OptionSetInfo?.IsBooleanValued ?? false))
                    {
                        return CoercionKind.BooleanOptionSetToBoolean;
                    }

                    return CoercionKind.None; // Implicit coercion?

                case DKind.Record:
                    Contracts.Assert(fromType.IsAggregate);
                    Contracts.Assert(fromType.Kind == toType.Kind);

                    return CoercionKind.RecordToRecord;

                case DKind.Table:
                    Contracts.Assert(fromType.IsAggregate);
                    Contracts.Assert(fromType.Kind == DKind.Table || fromType.Kind == DKind.Record);

                    if (fromType.Kind == DKind.Table)
                    {
                        return CoercionKind.TableToTable;
                    }

                    if (fromType.Kind == DKind.Record)
                    {
                        return CoercionKind.RecordToTable;
                    }

                    Contracts.Assert(false, "Unexpected type for coercion.");
                    break;

                case DKind.DateTime:
                case DKind.DateTimeNoTimeZone:
                    Contracts.Assert(DType.String.Accepts(fromType) || DType.Number.Accepts(fromType) || DType.Time.Accepts(fromType) || DType.Date.Accepts(fromType), "Unsupported type coercion");
                    if (DType.Number.Accepts(fromType))
                    {
                        return CoercionKind.NumberToDateTime;
                    }
                    else if (DType.Date.Accepts(fromType))
                    {
                        return CoercionKind.DateToDateTime;
                    }
                    else if (DType.Time.Accepts(fromType))
                    {
                        return CoercionKind.TimeToDateTime;
                    }

                    return CoercionKind.TextToDateTime;

                case DKind.Time:
                    Contracts.Assert(DType.String.Accepts(fromType) || DType.Number.Accepts(fromType) || DType.DateTime.Accepts(fromType) || DType.Date.Accepts(fromType), "Unsupported type coercion");
                    if (DType.Number.Accepts(fromType))
                    {
                        return CoercionKind.NumberToTime;
                    }
                    else if (DType.Date.Accepts(fromType))
                    {
                        return CoercionKind.DateToTime;
                    }
                    else if (DType.DateTime.Accepts(fromType))
                    {
                        return CoercionKind.DateTimeToTime;
                    }

                    return CoercionKind.TextToTime;

                case DKind.Date:
                    Contracts.Assert(DType.String.Accepts(fromType) || DType.Number.Accepts(fromType) || DType.DateTime.Accepts(fromType) || DType.Time.Accepts(fromType), "Unsupported type coercion");
                    if (DType.Number.Accepts(fromType))
                    {
                        return CoercionKind.NumberToDate;
                    }
                    else if (DType.Time.Accepts(fromType))
                    {
                        return CoercionKind.TimeToDate;
                    }
                    else if (DType.DateTime.Accepts(fromType))
                    {
                        return CoercionKind.DateTimeToDate;
                    }

                    return CoercionKind.TextToDate;

                case DKind.OptionSetValue:
                    Contracts.Assert(DType.OptionSetValue.Accepts(fromType) || (DType.Boolean.Accepts(fromType) && (toType.OptionSetInfo?.IsBooleanValued ?? false)), "Unsupported type coercion");

                    if (DType.Boolean.Accepts(fromType) && (toType.OptionSetInfo?.IsBooleanValued ?? false))
                    {
                        return CoercionKind.BooleanToOptionSet;
                    }

                    return CoercionKind.None; // Implicit coercion?

                case DKind.ViewValue:
                    Contracts.Assert(DType.ViewValue.Accepts(fromType), "Unsupported type coercion");
                    return CoercionKind.None; // Implicit coercion?
                default:
                    // Nothing else can be coerced.
                    Contracts.Assert(false, "Unsupported type coercion");
                    break;
            }

            // This should be impossible, the caller can catch and treat it as CoercionKind.None but should investigate.
            throw new InvalidCoercionException($"Attempting to generate invalid coercion from {fromType.GetKindString()} to {toType.GetKindString()}");
        }

        private static CoercionKind GetToNumberCoercion(DType fromType)
        {
            Contracts.Assert(
                DType.String.Accepts(fromType) || DType.Boolean.Accepts(fromType) || DType.Number.Accepts(fromType) ||
                DType.DateTime.Accepts(fromType) || DType.Time.Accepts(fromType) || DType.Date.Accepts(fromType) || DType.DateTimeNoTimeZone.Accepts(fromType) ||
                fromType.IsControl || (DType.OptionSetValue.Accepts(fromType) && (fromType.OptionSetInfo?.IsBooleanValued ?? false)), "Unsupported type coercion");

            if (DType.String.Accepts(fromType))
            {
                return CoercionKind.TextToNumber;
            }

            if (DType.Boolean.Accepts(fromType))
            {
                return CoercionKind.BooleanToNumber;
            }

            if (fromType.Kind == DKind.DateTime || fromType.Kind == DKind.DateTimeNoTimeZone)
            {
                return CoercionKind.DateTimeToNumber;
            }

            if (fromType.Kind == DKind.Time)
            {
                return CoercionKind.TimeToNumber;
            }

            if (fromType.Kind == DKind.Date)
            {
                return CoercionKind.DateToNumber;
            }

            if (DType.OptionSetValue.Accepts(fromType) && (fromType.OptionSetInfo?.IsBooleanValued ?? false))
            {
                return CoercionKind.BooleanOptionSetToNumber;
            }

            return CoercionKind.None;
        }

        /// <summary>
        /// Resolves the coercion type for any type to a type with <see cref="DKind.Enum"/> kind.
        /// </summary>
        /// <param name="fromType">
        /// Type that is being coerced to an enum type.
        /// </param>
        /// <param name="toType">
        /// An enum type that a value of <paramref name="fromType"/> is being coerced to.
        /// </param>
        /// <returns>
        /// The result will generally resemble the coercion kind whose meaning resembles "fromType to
        /// toType.EnumSuperKind", but with special cases evident within.
        /// </returns>
        private static CoercionKind GetToEnumCoercion(DType fromType, DType toType)
        {
            Contracts.Assert(toType.Kind == DKind.Enum);

            return toType.EnumSuperkind switch
            {
                DKind.Number => GetCoercionKind(fromType, DType.Number),
                _ => GetToStringCoercion(fromType)
            };
        }

        private static CoercionKind GetToStringCoercion(DType fromType)
        {
            var acceptsN = DType.Number.Accepts(fromType);
            var acceptsDT = DType.DateTime.Accepts(fromType);
            var acceptsD = DType.Date.Accepts(fromType);
            var acceptsT = DType.Time.Accepts(fromType);
            var acceptsB = DType.Boolean.Accepts(fromType);
            var acceptsS = DType.String.Accepts(fromType);
            var acceptsG = DType.Guid.Accepts(fromType);
            var acceptsOS = DType.OptionSetValue.Accepts(fromType);
            var acceptsV = DType.ViewValue.Accepts(fromType);
            Contracts.Assert(acceptsN || acceptsB || acceptsDT || acceptsD || acceptsT || acceptsS || acceptsG || acceptsOS || acceptsV, "Unsupported type coercion");

            if (DType.Number.Accepts(fromType) || DType.DateTime.Accepts(fromType))
            {
                if (fromType.Kind == DKind.Date)
                {
                    return CoercionKind.DateToText;
                }
                else if (fromType.Kind == DKind.Time)
                {
                    return CoercionKind.TimeToText;
                }
                else if (fromType.Kind == DKind.DateTime)
                {
                    return CoercionKind.DateTimeToText;
                }

                return CoercionKind.NumberToText;
            }
            else if (DType.Boolean.Accepts(fromType))
            {
                return CoercionKind.BooleanToText;
            }
            else if (DType.Hyperlink.Accepts(fromType))
            {
                switch (fromType.Kind)
                {
                    case DKind.Blob:
                        return CoercionKind.BlobToHyperlink;
                    case DKind.Image:
                        return CoercionKind.ImageToHyperlink;
                    case DKind.Media:
                        return CoercionKind.MediaToHyperlink;
                    default:
                        return CoercionKind.None;
                }
            }
            else if (DType.OptionSetValue.Accepts(fromType))
            {
                return CoercionKind.OptionSetToText;
            }
            else if (DType.ViewValue.Accepts(fromType))
            {
                return CoercionKind.ViewToText;
            }
            else
            {
                return CoercionKind.None; // Implicit coercion?
            }
        }

        private static CoercionKind GetUntypedObjectCoercion(DType toType)
        {
            switch (toType.Kind)
            {
                case DKind.String:
                    return CoercionKind.UntypedToText;
                case DKind.Boolean:
                    return CoercionKind.UntypedToBoolean;
                case DKind.Number:
                    return CoercionKind.UntypedToNumber;
                case DKind.Date:
                    return CoercionKind.UntypedToDate;
                case DKind.Time:
                    return CoercionKind.UntypedToTime;
                case DKind.DateTime:
                    return CoercionKind.UntypedToDateTime;
                default:
                    return CoercionKind.None;
            }
        }
    }
}
