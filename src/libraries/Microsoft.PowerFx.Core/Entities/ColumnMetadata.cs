// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.PowerFx.Core.App;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Entities
{
    internal enum ColumnVisibility
    {
        Default = 0,
        Hidden = 1,
        Advanced = 2,
        Important = 3,
        Internal = 4,
    }

    internal enum ColumnCreationKind
    {
        UserProvided = 0,
        ServerGenerated = 1,
    }

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

    internal struct ColumnLookupMetadata
    {
        public readonly bool IsSearchable;
        public readonly bool IsSearchRequired;

        public ColumnLookupMetadata(bool isSearchable, bool isSearchRequired)
        {
            IsSearchable = isSearchable;
            IsSearchRequired = isSearchRequired;
        }
    }

    internal struct ColumnAttachmentMetadata
    {
        public readonly string ListFunctionName;
        public readonly string GetFunctionName;
        public readonly string CreateFunctionName;
        public readonly string DeleteFunctionName;

        public ColumnAttachmentMetadata(string listFunctionName, string getFunctionName, string createFunctionName, string deleteFunctionName)
        {
            Contracts.AssertNonEmpty(listFunctionName);
            Contracts.AssertNonEmpty(getFunctionName);
            Contracts.AssertNonEmpty(createFunctionName);
            Contracts.AssertNonEmpty(deleteFunctionName);

            ListFunctionName = listFunctionName;
            GetFunctionName = getFunctionName;
            CreateFunctionName = createFunctionName;
            DeleteFunctionName = deleteFunctionName;
        }
    }

    /// <summary>
    /// Implements logic for describing metadata about a datasource column.
    /// </summary>
    [DebuggerDisplay("Name={Name} ({DisplayName}) Type={Type.ToString()}")]
    internal sealed class ColumnMetadata : IExternalColumnMetadata
    {
        private readonly ColumnCreationKind _kind;
        private readonly ColumnVisibility _visibility;

        public ColumnMetadata(
            string name,
            DType schema,
            DataFormat? dataFormat,
            string displayName,
            bool isReadOnly,
            bool isKey,
            bool isRequired,
            ColumnCreationKind creationKind,
            ColumnVisibility visibility,
            string titleColumnName,
            string subtitleColumnName,
            string thumbnailColumnName,
            ColumnLookupMetadata? lookupMetadata,
            ColumnAttachmentMetadata? attachmentMetadata)
        {
            Contracts.AssertNonEmpty(name);
            Contracts.AssertValid(schema);
            Contracts.AssertOneOfValueTypeOrNull(dataFormat, DataTypeInfo.GetValidDataFormats(schema.Kind));
            Contracts.AssertNonEmpty(displayName);
            Contracts.AssertNonEmptyOrNull(titleColumnName);
            Contracts.AssertNonEmptyOrNull(subtitleColumnName);
            Contracts.AssertNonEmptyOrNull(thumbnailColumnName);
            Contracts.AssertValueOrNull(lookupMetadata);
            Contracts.AssertValueOrNull(attachmentMetadata);

            Name = name;
            Type = schema;
            DataFormat = dataFormat;
            DisplayName = displayName;
            IsReadOnly = isReadOnly;
            IsKey = isKey;
            IsRequired = isRequired;
            _kind = creationKind;
            _visibility = visibility;
            TitleColumnName = titleColumnName;
            SubtitleColumnName = subtitleColumnName;
            ThumbnailColumnName = thumbnailColumnName;
            LookupMetadata = lookupMetadata;
            AttachmentMetadata = attachmentMetadata;

            if (dataFormat == App.DataFormat.AllowedValues)
            {
                AllowedValues = AllowedValuesMetadata.CreateForValue(schema);
            }
        }

        public string Name { get; }
        public DType Type { get; }
        public DataFormat? DataFormat { get; }
        public string DisplayName { get; }
        public bool IsReadOnly { get; }
        public bool IsKey { get; }
        public bool IsRequired { get; }
        public bool IsHidden => _visibility == ColumnVisibility.Hidden || _visibility == ColumnVisibility.Internal;
        public bool IsServerGenerated => _kind == ColumnCreationKind.ServerGenerated;
        public AllowedValuesMetadata AllowedValues { get; }
        public string TitleColumnName { get; }
        public string SubtitleColumnName { get; }
        public string ThumbnailColumnName { get; }
        public ColumnLookupMetadata? LookupMetadata { get; }
        public ColumnAttachmentMetadata? AttachmentMetadata { get; }
    }
}
