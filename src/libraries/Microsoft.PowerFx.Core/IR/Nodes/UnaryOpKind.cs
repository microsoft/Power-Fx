// Copyright (c) Microsoft Corporation.
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

        // Coercion operations
        BooleanToNumber,
        BooleanOptionSetToNumber,         // TODO Decimal: this, and others, don't appear in the interpreter's LibaryUnary.cs?
        DateToNumber,
        TimeToNumber,
        DateTimeToNumber,

        BooleanToDecimal,
        BooleanOptionSetToDecimal,
        DateToDecimal,
        TimeToDecimal,
        DateTimeToDecimal,

        BlobToHyperlink,
        ImageToHyperlink,
        MediaToHyperlink,
        TextToHyperlink,

        SingleColumnRecordToLargeImage,
        ImageToLargeImage,
        LargeImageToImage,
        TextToImage,

        TextToMedia,
        TextToBlob,

        NumberToText,
        BooleanToText,
        OptionSetToText,
        ViewToText,
        DecimalToText,

        NumberToBoolean,
        TextToBoolean,
        BooleanOptionSetToBoolean,
        DecimalToBoolean,

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
        AggregateToDataEntity,
    }
}
