// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Entities
{
    internal sealed class AllowedValuesMetadata
    {
        private static readonly DName ValueColumnName = new DName("Value");

        public static AllowedValuesMetadata CreateForValue(DType valueType)
        {
            Contracts.Assert(valueType.IsValid);

            return new AllowedValuesMetadata(DType.CreateTable(new TypedName(valueType, ValueColumnName)));
        }

        public AllowedValuesMetadata(DType valuesSchema)
        {
            Contracts.Assert(valuesSchema.IsTable);
            Contracts.Assert(valuesSchema.Contains(ValueColumnName));

            ValuesSchema = valuesSchema;
        }

        /// <summary>
        /// The schema of the table returned from the document function DataSourceInfo(DS, DataSourceInfo.AllowedValues, "columnName").
        /// </summary>
        public DType ValuesSchema { get; }
    }
}
