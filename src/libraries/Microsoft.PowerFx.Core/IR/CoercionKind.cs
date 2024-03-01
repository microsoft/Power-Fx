// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.IR
{
    internal enum CoercionKind
    {
        None,

        DecimalToNumber,
        NumberToDecimal,

        TextToNumber,
        BooleanToNumber,
        OptionSetToNumber,
        DateToNumber,
        TimeToNumber,
        DateTimeToNumber,

        TextToDecimal,
        BooleanToDecimal,
        OptionSetToDecimal,
        DateToDecimal,
        TimeToDecimal,
        DateTimeToDecimal,

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

        BlobToMedia,
        HyperlinkToMedia,
        TextToMedia,

        TextToBlob,
        HyperlinkToBlob,

        NumberToText,
        DecimalToText,
        BooleanToText,
        OptionSetToText,
        ViewToText,
        DateToText,
        TimeToText,
        DateTimeToText,

        NumberToBoolean,
        DecimalToBoolean,
        TextToBoolean,
        OptionSetToBoolean,

        RecordToRecord, // See field mappings
        TableToTable,
        RecordToTable,

        NumberToDateTime,
        DecimalToDateTime,
        NumberToDate,
        DecimalToDate,
        NumberToTime,
        DecimalToTime,
        TextToDateTime,
        TextToDate,
        TextToTime,

        DateTimeToTime,
        DateToTime,
        TimeToDate,
        DateTimeToDate,
        TimeToDateTime,
        DateToDateTime,

        BooleanToOptionSet,
        AggregateToDataEntity,  
        
        OptionSetToColor,

        UntypedToText,
        UntypedToBoolean,
        UntypedToNumber,
        UntypedToDecimal,
        UntypedToDate,
        UntypedToTime,
        UntypedToDateTime,
        UntypedToColor,
        UntypedToGUID,

        TextToGUID,
        GUIDToText,
        BlobToText,
        ImageToText,
        MediaToText,
        PenImageToText,

        CurrencyToNumber,
        NumberToCurrency,
        CurrencyToText,
        TextToCurrency,
        CurrencyToBoolean,
        BooleanToCurrency,
        PrimitiveToSingleColumnRecord,
    }
}
