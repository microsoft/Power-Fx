// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.PowerFx.Core.Binding.BindInfo;
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
        private DelegationCapability _capability;
        private readonly DType _schema;

        private readonly FilterOpMetadata _filterDelegationMetadata;

        public TestDelegationMetadata(DelegationCapability capability = default, DType schema = default, Core.Functions.Delegation.DelegationMetadata.FilterOpMetadata filterDelegationMetadata = default)
        {
            _capability = capability;
            _schema = schema ?? EntityRecordType._type;
            _filterDelegationMetadata = filterDelegationMetadata;
        }

        public static RecordType EntityRecordType => RecordType.Empty()
                                                         .Add("logicStr2", FormulaType.String, "MyStr2")
                                                         .Add("logicDate2", FormulaType.DateTime, "MyDate2");

        public DType Schema => _schema;

        public DelegationCapability TableAttributes => _capability;

        public DelegationCapability TableCapabilities => _capability;

        public SortOpMetadata SortDelegationMetadata => throw new NotImplementedException();

        public FilterOpMetadata FilterDelegationMetadata => _filterDelegationMetadata;

        public GroupOpMetadata GroupDelegationMetadata => throw new NotImplementedException();

        public Dictionary<DPath, DPath> ODataPathReplacementMap => throw new NotImplementedException();
    }

    internal class TestExternalEntityScope : IExternalEntityScope
    {
        private readonly SymbolTable _symbol;

        public TestExternalEntityScope(SymbolTable symbol)
        {
            _symbol = symbol;
        }

        public bool TryGetNamedEnum(DName identName, out DType enumType) => throw new NotImplementedException();

        public bool TryGetCdsDataSourceWithLogicalName(string datasetName, string expandInfoIdentity, out IExternalCdsDataSource dataSource) => throw new NotImplementedException();

        public IExternalTabularDataSource GetTabularDataSource(string identName) => throw new NotImplementedException();

        public bool TryGetEntity<T>(DName currentEntityEntityName, out T externalEntity)
            where T : class, IExternalEntity
        {
            externalEntity = default;
            if (_symbol.TryGetVariable(currentEntityEntityName, out NameLookupInfo lookupInfo, out _))
            {
                externalEntity = lookupInfo.Data as T;
                return true;
            }

            return false;
        }
    }

    internal class DelegatableSymbolTable : SymbolTable
    {
        private readonly IExternalEntityScope _externalEntityScope;

        public DelegatableSymbolTable()
            : base()
        {
            _externalEntityScope = new TestExternalEntityScope(this);
        }

        internal override IExternalEntityScope InternalEntityScope => _externalEntityScope;
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

        public virtual bool IsSelectable => throw new NotImplementedException();

        public virtual bool IsDelegatable => throw new NotImplementedException();

        public bool RequiresAsync => throw new NotImplementedException();

        public IExternalDataEntityMetadataProvider DataEntityMetadataProvider => ExternalDataEntityMetadataProvider;

        public virtual DataSourceKind Kind => throw new NotImplementedException();

        public IExternalTableMetadata TableMetadata => throw new NotImplementedException();

        public virtual IDelegationMetadata DelegationMetadata => throw new NotImplementedException();

        public DName EntityName => new DName(Name);

        public DType Type { get; }

        public bool IsPageable => false;

        public virtual TabularDataQueryOptions QueryOptions => throw new NotImplementedException();

        public bool IsConvertingDisplayNameMapping => false;

        public BidirectionalDictionary<string, string> DisplayNameMapping => new BidirectionalDictionary<string, string>();

        public BidirectionalDictionary<string, string> PreviousDisplayNameMapping => null;

        IDelegationMetadata IExternalDataSource.DelegationMetadata => DelegationMetadata;

        public bool CanIncludeExpand(IExpandInfo expandToAdd)
        {
            throw new NotImplementedException();
        }

        public bool CanIncludeExpand(IExpandInfo parentExpandInfo, IExpandInfo expandToAdd)
        {
            throw new NotImplementedException();
        }

        public virtual bool CanIncludeSelect(string selectColumnName)
        {
            throw new NotImplementedException();
        }

        public bool CanIncludeSelect(IExpandInfo expandInfo, string selectColumnName)
        {
            throw new NotImplementedException();
        }

        public virtual IReadOnlyList<string> GetKeyColumns()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetKeyColumns(IExpandInfo expandInfo)
        {
            throw new NotImplementedException();
        }
    }

    internal class TestDelegableDataSource : TestDataSource
    {
        private readonly TabularDataQueryOptions _queryOptions;
        private readonly IDelegationMetadata _delegationMetadata;

        internal TestDelegableDataSource(string name, DType schema, IDelegationMetadata delegationMetadata)
            : base(name, schema)
        {
            _queryOptions = new TabularDataQueryOptions(this);
            _delegationMetadata = delegationMetadata;
        }

        public override bool IsSelectable => true;

        public override bool IsDelegatable => true;

        public override bool CanIncludeSelect(string selectColumnName)
        {
            return true;
        }

        public override TabularDataQueryOptions QueryOptions => new TabularDataQueryOptions(this);

        public override IDelegationMetadata DelegationMetadata => _delegationMetadata;

        public override DataSourceKind Kind => DataSourceKind.Connected;

        public override IReadOnlyList<string> GetKeyColumns()
        {
            return new List<string>();
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
