// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Tests
{
    public static class UDTTestHelper
    {
        // Notable missing types that are inappropriate for UDFs and UDTs: None, Void, Unknown, Deferred, Error
        // DateTimeNoTimeZone is included for Core testing since at least one host uses it.
        public static readonly Dictionary<DName, FormulaType> TestTypesDictionaryWithNoNumberType =
            new Dictionary<DName, FormulaType>()
            {
                { BuiltInTypeNames.Boolean, FormulaType.Boolean },
                { BuiltInTypeNames.Color, FormulaType.Color },
                { BuiltInTypeNames.Date, FormulaType.Date },
                { BuiltInTypeNames.Time, FormulaType.Time },
                { BuiltInTypeNames.DateTime, FormulaType.DateTime },
                { BuiltInTypeNames.DateTimeNoTimeZone_DateTimeTZInd, FormulaType.DateTimeNoTimeZone },
                { BuiltInTypeNames.Guid, FormulaType.Guid },
                { BuiltInTypeNames.Number_Float, FormulaType.Number }, // Float
                { BuiltInTypeNames.Decimal, FormulaType.Decimal },
                { BuiltInTypeNames.String_Text, FormulaType.String }, // Text
                { BuiltInTypeNames.Hyperlink, FormulaType.Hyperlink },
                { BuiltInTypeNames.UntypedObject_Dynamic, FormulaType.UntypedObject }, // Dynamic
            };

        // For historical reasons, we do most of our testing with Number type as Float.
        // Some tests are specifically designed without any Number type present.
        public static readonly Dictionary<DName, FormulaType> TestTypesDictionaryWithNumberTypeIsFloat = 
            new Dictionary<DName, FormulaType>(TestTypesDictionaryWithNoNumberType)
                { { BuiltInTypeNames.Number_Alias, FormulaType.Number } };

        public static readonly ReadOnlySymbolTable TestTypesWithNoNumberType = ReadOnlySymbolTable.NewDefaultTypes(TestTypesDictionaryWithNoNumberType);

        public static readonly ReadOnlySymbolTable TestTypesWithNumberTypeIsFloat = ReadOnlySymbolTable.NewDefaultTypes(TestTypesDictionaryWithNumberTypeIsFloat);
    }
}
