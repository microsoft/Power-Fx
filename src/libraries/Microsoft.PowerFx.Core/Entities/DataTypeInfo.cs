// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.PowerFx.Core.App;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Entities
{
    internal static class DataTypeInfo
    {
        private static readonly DataFormat[] NoValidFormat = new DataFormat[0];
        private static readonly DataFormat[] AllowedValuesOnly = new[] { DataFormat.AllowedValues };

        private static readonly IReadOnlyDictionary<DKind, DataFormat[]> _validDataFormatsPerDKind = new Dictionary<DKind, DataFormat[]>
        {
            { DKind.Number, AllowedValuesOnly },
            { DKind.Currency, AllowedValuesOnly },
            { DKind.String, new[] { DataFormat.AllowedValues, DataFormat.Email, DataFormat.Multiline, DataFormat.Phone } },
            { DKind.Record, new[] { DataFormat.Lookup } },
            { DKind.Table, new[] { DataFormat.Lookup } },
            { DKind.Attachment, new[] { DataFormat.Attachment } },
            { DKind.OptionSetValue, new[] { DataFormat.Lookup } }
        };

        public static DataFormat[] GetValidDataFormats(DKind dkind)
        {
            return _validDataFormatsPerDKind.TryGetValue(dkind, out var validFormats) ? validFormats : NoValidFormat;
        }
    }
}
