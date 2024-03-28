﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.IR.Nodes
{
    internal enum UnaryOpKind
    {
        // Value operations
        Negate,
        Percent,
        NegateDecimal,
        PercentDecimal,
        NegateDate,
        NegateDateTime,
        NegateTime,

        // Coercion operations
        BooleanToNumber,
        OptionSetToNumber,
        DateToNumber,
        TimeToNumber,
        DateTimeToNumber,
        CurrencyToNumber,

        BooleanToDecimal,
        OptionSetToDecimal,
        DateToDecimal,
        TimeToDecimal,
        DateTimeToDecimal,
        CurrencyToDecimal,

        BlobToHyperlink,
        ImageToHyperlink,
        MediaToHyperlink,
        TextToHyperlink,
        PenImageToHyperlink,

        SingleColumnRecordToLargeImage,
        ImageToLargeImage,
        LargeImageToImage,
        TextToImage,
        PenImageToImage,
        BlobToImage,
        HyperlinkToImage,

        TextToMedia,
        BlobToMedia,
        HyperlinkToMedia,

        TextToBlob,
        HyperlinkToBlob,

        NumberToText,
        DecimalToText,
        BooleanToText,
        OptionSetToText,
        ViewToText,
        CurrencyToText,
        GUIDToText,
        ImageToText,
        MediaToText,
        BlobToText,
        PenImageToText,

        TextToGUID,

        NumberToCurrency,
        TextToCurrency,
        BooleanToCurrency,

        NumberToBoolean,
        TextToBoolean,
        DecimalToBoolean,
        OptionSetToBoolean,
        CurrencyToBoolean,

        OptionSetToColor,

        RecordToRecord, // See field mappings
        TableToTable,
        RecordToTable,

        NumberToDateTime,
        NumberToDate,
        NumberToTime,
        TextToDateTime,
        TextToDate,
        TextToTime,
        DecimalToDateTime,
        DecimalToDate,
        DecimalToTime,

        DateTimeToTime,
        DateToTime,
        TimeToDate,
        DateTimeToDate,
        TimeToDateTime,
        DateToDateTime,

        BooleanToOptionSet,
        NumberToOptionSet,
        DecimalToOptionSet,
        StringToOptionSet,

        AggregateToDataEntity,

        // Argument pre-processesor in IR Phase.

        /// <summary>
        /// Used for pre-processing function arguments from blank to empty string.
        /// All Interpreter(backed) must implement this.
        /// </summary>
        BlankToEmptyString,

        /// <summary>
        /// Used for pre-processing untyped function arguments from string to number.
        /// All Interpreter(backed) must implement this.
        /// </summary>
        UntypedStringToUntypedFloat,
        UntypedStringToUntypedDecimal,
    }
}
