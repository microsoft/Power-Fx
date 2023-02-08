// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Types
{
    // These represent the kinds of values that the document model deals with.
    // These values are persisted. DType.Invalid will get an invalid DKind (0).
    //
    // NOTE: You should NEVER use a DKind in a place where ToString() is explicitly or implicitly called on it!
    // Because we have multiple DKinds map to the same value, DKind.ToString() has undefined behavior. In the
    // case where you need a string representation of a DKind, use DType.GetKindString().
    internal enum DKind
    {
        // This is a sentinel value, for a DType that is not actually valid. This is used
        // for cases where you would normally use the null type, due to DType's history
        // as previously being a struct.
        Invalid = 0,

        // Unknown is an authoring-time type. It is a subtype of all other types.
        // It represents a type that is not yet known, and it is used primarily for analysis
        // purposes (i.e. Top in static analysis).
        // This type does not have a runtime equivalent.
#pragma warning disable SA1300 // Element should begin with upper-case letter
        _Min = Unknown,
        Unknown = 1,

        // Error is an authoring-time type. It is a supertype of all other types.
        // It represents a failure or loss of precision during analysis (i.e. Bottom in
        // static analysis).
        // This type does not have a runtime equivalent.
        Error = 2,

        Record = 3,
        Table = 4,

        _MinPrimitive = Boolean,
        Boolean = 5,
        Number = 6,
        String = 7,
        Date = 8,
        Time = 9,
        DateTime = 10,
        Hyperlink = 11,
        Decimal = 12,
        Image = 13,
        Color = 14,
        Enum = 15,
        Media = 16,
        PenImage = 17,
        Blob = 18,
        LegacyBlob = 19,

        DateTimeNoTimeZone = 20,
        Guid = 21,
        OptionSetValue = 22,
        ViewValue = 23,
        NamedValue = 24,
        _LimPrimitive = Control,

        // Control type.
        Control = 25,

        // Expand Entity type.
        DataEntity = 26,

        ObjNull = 27, // A type representing a null value.

        // Metadata type,
        Metadata = 28, // Type represents column metadata.

        // OptionSet type,
        OptionSet = 30,

        // Polymorphic type,
        Polymorphic = 31,

        // Views
        View = 32,

        // Complex types
        File = 33,
        LargeImage = 34,
        UntypedObject = 35,

        // Lazy implementations of Record/Table, using LazyTypeProvider for field access indirection
        LazyRecord = 36,
        LazyTable = 37,

        Deferred = 38,

        _Lim = 39,
#pragma warning restore SA1300 // Element should begin with upper-case letter
    }
}
