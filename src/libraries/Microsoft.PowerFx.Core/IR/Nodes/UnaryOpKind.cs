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
        OptionSetToNumber,  // Decimal TODO: this, and others, don't appear in the interpreter's LibaryUnary.cs?
        DateToNumber,
        TimeToNumber,
        DateTimeToNumber,

        BooleanToDecimal,
        OptionSetToDecimal,
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
        DecimalToBoolean,
        OptionSetToBoolean,
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
        AggregateToDataEntity,

        // Argument pre-processesor in IR Phase.

        /// <summary>
        /// Used for pre-processing function arguments from blank to empty string.
        /// All Interpreter(backed) must implement this.
        /// </summary>
        BlankToEmptyString,
    }
}
