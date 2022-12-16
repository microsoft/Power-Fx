// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.IR
{
    internal enum CoercionKind
    {
        None,

        TextToNumber,
        BooleanToNumber,
        BooleanOptionSetToNumber,
        DateToNumber,
        TimeToNumber,
        DateTimeToNumber,

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
        DateToText,
        TimeToText,
        DateTimeToText,

        NumberToBoolean,
        TextToBoolean,
        BooleanOptionSetToBoolean,

        RecordToRecord, // See field mappings
        TableToTable,
        RecordToTable,

        NumberToDateTime,
        NumberToDate,
        NumberToTime,
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

        UntypedToText,
        UntypedToBoolean,
        UntypedToNumber,
        UntypedToDate,
        UntypedToTime,
        UntypedToDateTime,
    }
}
