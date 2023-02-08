// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.PowerFx.Core.App;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Entities
{
    internal static class DataTypeInfo
    {
        private static readonly DataFormat[] NoValidFormat = Array.Empty<DataFormat>();
        private static readonly DataFormat[] AllowedValuesOnly = new[] { DataFormat.AllowedValues };

        private static readonly IReadOnlyDictionary<DKind, DataFormat[]> _validDataFormatsPerDKind = new Dictionary<DKind, DataFormat[]>
        {
            { DKind.Number, AllowedValuesOnly },
            { DKind.Decimal, AllowedValuesOnly },
            { DKind.String, new[] { DataFormat.AllowedValues, DataFormat.Email, DataFormat.Multiline, DataFormat.Phone } },
            { DKind.Record, new[] { DataFormat.Lookup } },
            { DKind.Table, new[] { DataFormat.Lookup } },
            { DKind.OptionSetValue, new[] { DataFormat.Lookup } }
        };

        public static DataFormat[] GetValidDataFormats(DType dtype)
        {
            if (dtype.IsAttachment)
            {
                return new[] { DataFormat.Attachment };
            }

            return _validDataFormatsPerDKind.TryGetValue(dtype.Kind, out var validFormats) ? validFormats : NoValidFormat;
        }
    }
}
