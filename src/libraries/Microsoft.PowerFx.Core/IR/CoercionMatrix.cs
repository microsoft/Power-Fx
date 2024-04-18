// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.IR
{
    internal class CoercionMatrix
    {
        public static CoercionKind GetCoercionKind(DType fromType, DType toType, bool usePowerFxV1CompatibilityRules)
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

            return FlattenCoercionMatrix(fromType, toType, usePowerFxV1CompatibilityRules);
        }

        private static CoercionKind FlattenCoercionMatrix(DType fromType, DType toType, bool usePowerFxV1CompatibilityRules)
        {
            switch (toType.Kind)
            {
                case DKind.Number:
                    return GetToNumberCoercion(fromType, usePowerFxV1CompatibilityRules);

                case DKind.Currency:
                    if (usePowerFxV1CompatibilityRules)
                    {
                        switch (fromType.Kind)
                        {
                            case DKind.Number:
                                return CoercionKind.NumberToCurrency;
                            case DKind.Boolean:
                                return CoercionKind.BooleanToCurrency;
                            case DKind.String:
                                return CoercionKind.TextToCurrency;
                        }
                    }
                    else
                    {
                        return GetToNumberCoercion(fromType, usePowerFxV1CompatibilityRules: false);
                    }

                    Contracts.Assert(false, "Unsupported type coercion");
                    break;

                case DKind.Decimal:
                    return GetToDecimalCoercion(fromType, usePowerFxV1CompatibilityRules);

                case DKind.Color:
                    if (DType.OptionSetValue.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) && fromType.OptionSetInfo?.BackingKind == DKind.Color)
                    {
                        return CoercionKind.OptionSetToColor;
                    }

                    Contracts.Assert(false, "Unsupported type coercion");
                    break;

                case DKind.PenImage:
                    // It is not safe to coerce this type.
                    Contracts.Assert(false, "Unsupported type coercion");
                    break;

                case DKind.Hyperlink:
                    if (usePowerFxV1CompatibilityRules)
                    {
                        switch (fromType.Kind)
                        {
                            case DKind.Blob:
                                return CoercionKind.BlobToHyperlink;
                            case DKind.Image:
                                return CoercionKind.ImageToHyperlink;
                            case DKind.Media:
                                return CoercionKind.MediaToHyperlink;
                            case DKind.String:
                                return CoercionKind.TextToHyperlink;
                            case DKind.PenImage:
                                return CoercionKind.PenImageToHyperlink;
                        }
                    }
                    else
                    {
                        if (DType.String.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
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
                    }

                    Contracts.Assert(false, "Unsupported type coercion");
                    break;

                case DKind.Image:
                    if (fromType.IsLargeImage)
                    {
                        return CoercionKind.LargeImageToImage;
                    }

                    if (usePowerFxV1CompatibilityRules)
                    {
                        switch (fromType.Kind)
                        {
                            case DKind.Blob:
                                return CoercionKind.BlobToImage;
                            case DKind.PenImage:
                                return CoercionKind.PenImageToImage;
                            case DKind.Hyperlink:
                                return CoercionKind.HyperlinkToImage;
                        }
                    }

                    if (fromType.Kind != DKind.Media && fromType.Kind != DKind.Blob && DType.String.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                    {
                        return CoercionKind.TextToImage;
                    }

                    Contracts.Assert(false, "Unsupported type coercion");
                    break;

                case DKind.Media:
                    if (usePowerFxV1CompatibilityRules)
                    {
                        switch (fromType.Kind)
                        {
                            case DKind.Blob:
                                return CoercionKind.BlobToMedia;
                            case DKind.Hyperlink:
                                return CoercionKind.HyperlinkToMedia;
                        }
                    }

                    if (fromType.Kind != DKind.Image && fromType.Kind != DKind.Blob && DType.String.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                    {
                        return CoercionKind.TextToMedia;
                    }

                    Contracts.Assert(false, "Unsupported type coercion");
                    break;

                case DKind.Blob:
                    if (usePowerFxV1CompatibilityRules)
                    {
                        if (fromType.Kind == DKind.Hyperlink)
                        {
                            return CoercionKind.HyperlinkToBlob;
                        }
                    }

                    if (DType.String.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                    {
                        return CoercionKind.TextToBlob;
                    }

                    Contracts.Assert(false, "Unsupported type coercion");
                    break;

                case DKind.String:
                    return GetToStringCoercion(fromType, usePowerFxV1CompatibilityRules);

                case DKind.Enum:
                    return GetToEnumCoercion(fromType, toType, usePowerFxV1CompatibilityRules);

                case DKind.Boolean:
                    Contracts.Assert(
                        DType.Number.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) ||
                        DType.Decimal.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) ||
                        DType.Currency.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) ||
                        DType.String.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) ||
                        (DType.OptionSetValue.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) && fromType.OptionSetInfo?.BackingKind == DKind.Boolean),
                        "Unsupported type coercion");
                    if (DType.Number.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                    {
                        return CoercionKind.NumberToBoolean;
                    }

                    if (DType.Decimal.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                    {
                        return CoercionKind.DecimalToBoolean;
                    }

                    if (DType.Currency.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                    {
                        return CoercionKind.CurrencyToBoolean;
                    }

                    if (DType.String.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                    {
                        return CoercionKind.TextToBoolean;
                    }

                    if (DType.OptionSetValue.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) && fromType.OptionSetInfo?.BackingKind == DKind.Boolean)
                    {
                        return CoercionKind.OptionSetToBoolean;
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
                    Contracts.Assert(
                        DType.String.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) ||
                        DType.Number.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) ||
                        DType.Decimal.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) ||
                        DType.Time.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) ||
                        DType.Date.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules),
                        "Unsupported type coercion");
                    if (DType.Number.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                    {
                        return CoercionKind.NumberToDateTime;
                    }
                    else if (DType.Decimal.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                    {
                        return CoercionKind.DecimalToDateTime;
                    }
                    else if (DType.Date.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                    {
                        return CoercionKind.DateToDateTime;
                    }
                    else if (DType.Time.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                    {
                        return CoercionKind.TimeToDateTime;
                    }

                    return CoercionKind.TextToDateTime;

                case DKind.Time:
                    Contracts.Assert(
                        DType.String.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) ||
                        DType.Number.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) ||
                        DType.Decimal.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) ||
                        DType.DateTime.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) || 
                        DType.Date.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules),
                        "Unsupported type coercion");
                    if (DType.Number.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                    {
                        return CoercionKind.NumberToTime;
                    }
                    else if (DType.Decimal.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                    {
                        return CoercionKind.DecimalToTime;
                    }
                    else if (DType.Date.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                    {
                        return CoercionKind.DateToTime;
                    }
                    else if (DType.DateTime.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                    {
                        return CoercionKind.DateTimeToTime;
                    }

                    return CoercionKind.TextToTime;

                case DKind.Date:
                    Contracts.Assert(
                        DType.String.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) ||
                        DType.Number.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) ||
                        DType.Decimal.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) ||
                        DType.DateTime.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) || 
                        DType.Time.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules),
                        "Unsupported type coercion");
                    if (DType.Number.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                    {
                        return CoercionKind.NumberToDate;
                    }
                    else if (DType.Decimal.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                    {
                        return CoercionKind.DecimalToDate;
                    }
                    else if (DType.Time.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                    {
                        return CoercionKind.TimeToDate;
                    }
                    else if (DType.DateTime.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                    {
                        return CoercionKind.DateTimeToDate;
                    }

                    return CoercionKind.TextToDate;

                case DKind.OptionSetValue:
                    Contracts.Assert(
                        DType.OptionSetValue.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) ||
                        (DType.Boolean.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) && toType.OptionSetInfo?.BackingKind == DKind.Boolean) ||
                        (DType.Decimal.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) && toType.OptionSetInfo?.BackingKind == DKind.Number) ||
                        (DType.Number.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) && toType.OptionSetInfo?.BackingKind == DKind.Number) ||
                        (DType.String.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) && toType.OptionSetInfo?.BackingKind == DKind.String),
                        "Unsupported type coercion");

                    if (DType.Boolean.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) && toType.OptionSetInfo?.BackingKind == DKind.Boolean)
                    {
                        return CoercionKind.BooleanToOptionSet;
                    }
                    else if (DType.Decimal.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) && toType.OptionSetInfo?.BackingKind == DKind.Number)
                    {
                        return CoercionKind.DecimalToOptionSet;
                    }
                    else if (DType.Number.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) && toType.OptionSetInfo?.BackingKind == DKind.Number)
                    {
                        return CoercionKind.NumberToOptionSet;
                    }
                    else if (DType.String.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) && toType.OptionSetInfo?.BackingKind == DKind.String)
                    {
                        return CoercionKind.StringToOptionSet;
                    }

                    return CoercionKind.None; // Implicit coercion?

                case DKind.ViewValue:
                    Contracts.Assert(DType.ViewValue.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules), "Unsupported type coercion");
                    return CoercionKind.None; // Implicit coercion?
                case DKind.Guid:
                    Contracts.Assert(DType.String.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                    if (DType.String.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
                    {
                        return CoercionKind.TextToGUID;
                    }

                    break;
                default:
                    // Nothing else can be coerced.
                    Contracts.Assert(false, "Unsupported type coercion");
                    break;
            }

            // This should be impossible, the caller can catch and treat it as CoercionKind.None but should investigate.
            throw new InvalidCoercionException($"Attempting to generate invalid coercion from {fromType.GetKindString()} to {toType.GetKindString()}");
        }

        private static CoercionKind GetToNumberCoercion(DType fromType, bool usePowerFxV1CompatibilityRules)
        {
            Contracts.Assert(
                DType.String.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) || 
                DType.Boolean.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) ||
                DType.Currency.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) ||
                DType.Number.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) ||
                DType.Decimal.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) ||
                DType.DateTime.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) ||
                DType.Time.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) || 
                DType.Date.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) ||
                DType.DateTimeNoTimeZone.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) ||
                fromType.IsControl || 
                (DType.OptionSetValue.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) && ((fromType.OptionSetInfo?.BackingKind == DKind.Boolean) || fromType.OptionSetInfo?.BackingKind == DKind.Number)),
                "Unsupported type coercion");

            if (DType.String.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
            {
                return CoercionKind.TextToNumber;
            }

            if (DType.Decimal.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
            {
                return CoercionKind.DecimalToNumber;
            }

            if (DType.Currency.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
            {
                return CoercionKind.CurrencyToNumber;
            }

            if (DType.Boolean.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
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

            if (DType.OptionSetValue.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) && (fromType.OptionSetInfo?.BackingKind == DKind.Number))
            {
                return CoercionKind.OptionSetToNumber;
            }

            return CoercionKind.None;
        }

        private static CoercionKind GetToDecimalCoercion(DType fromType, bool usePowerFxV1CompatibilityRules)
        {
            Contracts.Assert(
                DType.String.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) || DType.Boolean.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) || DType.Number.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) || DType.Decimal.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) ||
                DType.DateTime.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) || DType.Time.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) || DType.Date.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) || DType.DateTimeNoTimeZone.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) ||
                fromType.IsControl || (DType.OptionSetValue.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) && ((fromType.OptionSetInfo?.BackingKind == DKind.Boolean) || fromType.OptionSetInfo?.BackingKind == DKind.Number)), "Unsupported type coercion");

            if (DType.Number.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
            {
                return CoercionKind.NumberToDecimal;
            }

            if (DType.String.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
            {
                return CoercionKind.TextToDecimal;
            }

            if (DType.Boolean.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
            {
                return CoercionKind.BooleanToDecimal;
            }

            if (fromType.Kind == DKind.DateTime || fromType.Kind == DKind.DateTimeNoTimeZone)
            {
                return CoercionKind.DateTimeToDecimal;
            }

            if (fromType.Kind == DKind.Time)
            {
                return CoercionKind.TimeToDecimal;
            }

            if (fromType.Kind == DKind.Date)
            {
                return CoercionKind.DateToDecimal;
            }

            if (DType.OptionSetValue.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules) && (fromType.OptionSetInfo?.BackingKind == DKind.Number))
            {
                return CoercionKind.OptionSetToDecimal;
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
        /// <param name="usePowerFxV1CompatibilityRules">Use PFx v1 compatibility rules if enabled (less
        /// permissive Accepts relationships).</param>
        /// <returns>
        /// The result will generally resemble the coercion kind whose meaning resembles "fromType to
        /// toType.EnumSuperKind", but with special cases evident within.
        /// </returns>
        private static CoercionKind GetToEnumCoercion(DType fromType, DType toType, bool usePowerFxV1CompatibilityRules)
        {
            Contracts.Assert(toType.Kind == DKind.Enum);

            return toType.EnumSuperkind switch
            {
                DKind.Number => GetCoercionKind(fromType, DType.Number, usePowerFxV1CompatibilityRules),
                _ => GetToStringCoercion(fromType, usePowerFxV1CompatibilityRules)
            };
        }

        private static CoercionKind GetToStringCoercion(DType fromType, bool usePowerFxV1CompatibilityRules)
        {
            var acceptsN = DType.Number.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            var acceptsW = DType.Decimal.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            var acceptsCurrency = DType.Currency.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            var acceptsDT = DType.DateTime.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            var acceptsD = DType.Date.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            var acceptsT = DType.Time.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            var acceptsB = DType.Boolean.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            var acceptsS = DType.String.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            var acceptsG = DType.Guid.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            var acceptsO = DType.Blob.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            var acceptsI = DType.Image.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            var acceptsM = DType.Media.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            var acceptsP = DType.PenImage.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            var acceptsOS = DType.OptionSetValue.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            var acceptsV = DType.ViewValue.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Contracts.Assert(
                acceptsN || acceptsW || acceptsCurrency || acceptsB ||
                acceptsDT || acceptsD || acceptsT ||
                acceptsS || acceptsG || 
                acceptsO || acceptsI || acceptsM || acceptsP ||
                acceptsOS || acceptsV,
                "Unsupported type coercion");

            if (acceptsN || acceptsDT)
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
            else if (acceptsG)
            {
                return CoercionKind.GUIDToText;
            }
            else if (acceptsW)
            {
                return CoercionKind.DecimalToText;
            }
            else if (acceptsT)
            {
                return CoercionKind.TimeToText;
            }
            else if (acceptsD)
            {
                return CoercionKind.DateToText;
            }
            else if (acceptsB)
            {
                return CoercionKind.BooleanToText;
            }
            else if (DType.Hyperlink.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
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
            else if (DType.OptionSetValue.Accepts(fromType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules))
            {
                return CoercionKind.OptionSetToText;
            }
            else if (acceptsV)
            {
                return CoercionKind.ViewToText;
            }
            else if (acceptsO)
            {
                return CoercionKind.BlobToText;
            }
            else if (acceptsM)
            {
                return CoercionKind.MediaToText;
            }
            else if (acceptsI)
            {
                return CoercionKind.ImageToText;
            }
            else if (acceptsP)
            {
                return CoercionKind.PenImageToText;
            }
            else if (acceptsCurrency)
            {
                return CoercionKind.CurrencyToText;
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
                case DKind.Decimal:
                    return CoercionKind.UntypedToDecimal;
                case DKind.Date:
                    return CoercionKind.UntypedToDate;
                case DKind.Time:
                    return CoercionKind.UntypedToTime;
                case DKind.DateTime:
                    return CoercionKind.UntypedToDateTime;
                case DKind.Color:
                    return CoercionKind.UntypedToColor;
                case DKind.Guid:
                    return CoercionKind.UntypedToGUID;
                default:
                    return CoercionKind.None;
            }
        }
    }
}
