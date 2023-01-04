// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Entities.Delegation;
using Microsoft.PowerFx.Core.Entities.QueryOptions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.UtilityDataStructures;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Tests.Helpers
{
    internal class ExternalDataEntityMetadataProvider : IExternalDataEntityMetadataProvider
    {
        public bool TryGetEntityMetadata(string expandInfoIdentity, out IDataEntityMetadata entityMetadata)
        {
            var st = Environment.StackTrace;

            if (st.Contains("Microsoft.PowerFx.Types.CollectionTableValue`1.Matches"))
            {
                entityMetadata = new DataEntityMetadata();
                return true;
            }

            // Getting Metadata isn't allowed for performance reasons only
            throw new GettingMetadataNotAllowedException();
        }
    }

    internal class TestDelegationMetadata : IDelegationMetadata
    {
        public static RecordType EntityRecordType => RecordType.Empty()
                                                         .Add("logicStr2", FormulaType.String, "MyStr2")
                                                         .Add("logicDate2", FormulaType.DateTime, "MyDate2");

        public DType Schema => EntityRecordType._type;

        public DelegationCapability TableAttributes => throw new NotImplementedException();

        public DelegationCapability TableCapabilities => throw new NotImplementedException();

        public Core.Functions.Delegation.DelegationMetadata.SortOpMetadata SortDelegationMetadata => throw new NotImplementedException();

        public Core.Functions.Delegation.DelegationMetadata.FilterOpMetadata FilterDelegationMetadata => throw new NotImplementedException();

        public Core.Functions.Delegation.DelegationMetadata.GroupOpMetadata GroupDelegationMetadata => throw new NotImplementedException();

        public Dictionary<DPath, DPath> ODataPathReplacementMap => throw new NotImplementedException();
    }

    internal class DataEntityMetadata : IDataEntityMetadata
    {
        public string EntityName => throw new NotImplementedException();

        public DType Schema => throw new NotImplementedException();

        public BidirectionalDictionary<string, string> DisplayNameMapping => throw new NotImplementedException();

        public BidirectionalDictionary<string, string> PreviousDisplayNameMapping => throw new NotImplementedException();

        public bool IsConvertingDisplayNameMapping { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IDelegationMetadata DelegationMetadata => new TestDelegationMetadata();

        public IExternalTableDefinition EntityDefinition => throw new NotImplementedException();

        public string DatasetName => throw new NotImplementedException();

        public bool IsValid => throw new NotImplementedException();

        public string OriginalDataDescriptionJson => throw new NotImplementedException();

        public string InternalRepresentationJson => throw new NotImplementedException();

        public void ActualizeTemplate(string datasetName)
        {
            throw new NotImplementedException();
        }

        public void ActualizeTemplate(string datasetName, string entityName)
        {
            throw new NotImplementedException();
        }

        public void LoadClientSemantics(bool isPrimaryTable = false)
        {
            throw new NotImplementedException();
        }

        public void SetClientSemantics(IExternalTableDefinition tableDefinition)
        {
            throw new NotImplementedException();
        }

        public string ToJsonDefinition()
        {
            throw new NotImplementedException();
        }
    }

    internal class TestDataSource : IExternalDataSource, IExternalTabularDataSource
    {
        internal IExternalDataEntityMetadataProvider ExternalDataEntityMetadataProvider;

        internal TestDataSource(string name, DType schema)
        {
            ExternalDataEntityMetadataProvider = new ExternalDataEntityMetadataProvider();
            Type = DType.AttachDataSourceInfo(schema, this);
            Name = name;
        }

        public string Name { get; }

        public bool IsSelectable => throw new NotImplementedException();

        public bool IsDelegatable => throw new NotImplementedException();

        public bool RequiresAsync => throw new NotImplementedException();

        public IExternalDataEntityMetadataProvider DataEntityMetadataProvider => ExternalDataEntityMetadataProvider;

        public DataSourceKind Kind => throw new NotImplementedException();

        public IExternalTableMetadata TableMetadata => throw new NotImplementedException();

        public DelegationMetadataBase DelegationMetadata => throw new NotImplementedException();

        public DName EntityName => new DName(Name);

        public DType Type { get; }

        public bool IsPageable => false;

        public TabularDataQueryOptions QueryOptions => throw new NotImplementedException();

        public bool IsConvertingDisplayNameMapping => false;

        public BidirectionalDictionary<string, string> DisplayNameMapping => new BidirectionalDictionary<string, string>();

        public BidirectionalDictionary<string, string> PreviousDisplayNameMapping => null;

        IDelegationMetadata IExternalDataSource.DelegationMetadata => throw new NotImplementedException();

        public bool CanIncludeExpand(IExpandInfo expandToAdd)
        {
            throw new NotImplementedException();
        }

        public bool CanIncludeExpand(IExpandInfo parentExpandInfo, IExpandInfo expandToAdd)
        {
            throw new NotImplementedException();
        }

        public bool CanIncludeSelect(string selectColumnName)
        {
            throw new NotImplementedException();
        }

        public bool CanIncludeSelect(IExpandInfo expandInfo, string selectColumnName)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<string> GetKeyColumns()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetKeyColumns(IExpandInfo expandInfo)
        {
            throw new NotImplementedException();
        }
    }

    internal class TestExpandInfo : IExpandInfo
    {
        internal TestDataSource DataSource;

        internal TestExpandInfo()
        {
            DataSource = new TestDataSource("test", DType.EmptyTable);
        }

        public string Identity => "Some Identity";

        public bool IsTable => true;

        public string Name => throw new NotImplementedException();

        public string PolymorphicParent => throw new NotImplementedException();

        public IExternalDataSource ParentDataSource => DataSource;

        public ExpandPath ExpandPath => throw new NotImplementedException();

        public IExpandInfo Clone()
        {
            return new TestExpandInfo();
        }

        public string ToDebugString()
        {
            throw new NotImplementedException();
        }

        public void UpdateEntityInfo(IExternalDataSource dataSource, string relatedEntityPath)
        {
        }
    }

    internal class GettingMetadataNotAllowedException : Exception
    {
        public GettingMetadataNotAllowedException()
        {
        }

        public GettingMetadataNotAllowedException(string message)
            : base(message)
        {
        }

        public GettingMetadataNotAllowedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected GettingMetadataNotAllowedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
