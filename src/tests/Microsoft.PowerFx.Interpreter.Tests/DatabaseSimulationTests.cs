using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Entities.Delegation;
using Microsoft.PowerFx.Core.Entities.QueryOptions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.UtilityDataStructures;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class DatabaseSimulationTests
    {
        [Fact]
        public void DatabaseSimulation_Test()
        {
            // var expr = "Patch(Table, First(Filter(Table, MyStr=\"Str3\")), {MyDate: \"2022-11-14 7:22:06 pm\"})";
            var expr = "Patch(Table, First(Filter(Table, MyStr=\"Str3\")), {MyDate: DateTimeValue(\"2022-11-14 7:22:06 pm\") })";

            var databaseTable = DatabaseTable.CreateTestTable();
            var symbols = new SymbolTable();

            symbols.AddVariable("Table", DatabaseTable.TestTableType);
            symbols.EnableMutationFunctions();

            var engine = new RecalcEngine();
            var runtimeConfig = ReadOnlySymbolValues.New(new Dictionary<string, DatabaseTable>() { { "Table", databaseTable } });

            CheckResult check = engine.Check(expr, symbolTable: symbols, options: new ParserOptions() { AllowsSideEffects = true });
            Assert.True(check.IsSuccess, string.Join("\r\n", check.Errors.Select(ee => ee.Message)));

            IExpressionEvaluator run = check.GetEvaluator();
            FormulaValue result = run.EvalAsync(CancellationToken.None, runtimeConfig).Result;
        }

        internal class DatabaseTable : InMemoryTableValue
        {
            internal static TableType TestTableType => DatabaseRecord.TestRecordType.ToTable();

            internal static DatabaseTable CreateTestTable() =>
                new(
                    IRContext.NotInSource(TestTableType),
                    new List<DValue<RecordValue>>()
                    {
                        DValue<RecordValue>.Of(DatabaseRecord.CreateTestRecord("Str1", new DateTime(2022, 1, 1, 17, 33, 17), 3.14159265358979)),
                        DValue<RecordValue>.Of(DatabaseRecord.CreateTestRecord("Str2", new DateTime(2001, 7, 11, 8, 17, 52), 2.71828182845904)),
                        DValue<RecordValue>.Of(DatabaseRecord.CreateTestRecord("Str3", new DateTime(2019, 6, 28, 0, 45, 15), 1.41421356237309)),
                        DValue<RecordValue>.Of(DatabaseRecord.CreateTestRecord("Str4", new DateTime(2010, 4, 24, 16, 15, 0), 1.61803398874989)),
                        DValue<RecordValue>.Of(DatabaseRecord.CreateTestRecord("Str5", new DateTime(1954, 12, 4, 21, 5, 10), 2.15443469003188))
                    });

            internal DatabaseTable(IRContext irContext, IEnumerable<DValue<RecordValue>> records)
                : base(irContext, records)
            {
            }
        }

        internal class DatabaseRecord : InMemoryRecordValue
        {
            internal static FormulaType TestEntityType => new TestEntityType();

            internal static RecordType TestRecordType => RecordType.Empty()
                .Add("logicStr", FormulaType.String, "MyStr")
                .Add("logicDate", FormulaType.Date, "MyDate")
                .Add("logicNum", FormulaType.Number, "MyNum")
                .Add("logicEnt", TestEntityType, "MyEntity");

            internal static DatabaseRecord CreateTestRecord(string myStr, DateTime myDate, double myNum) =>
                new(
                    IRContext.NotInSource(TestRecordType),
                    new List<NamedValue>()
                    {
                        new NamedValue("MyStr", New(myStr)),
                        new NamedValue("MyDate", New(myDate)),
                        new NamedValue("MyNum", New(myNum)),
                        new NamedValue("MyEntity", new TestEntityValue(IRContext.NotInSource(TestEntityType)))
                    });

            internal DatabaseRecord(IRContext irContext, IEnumerable<NamedValue> fields)
                : base(irContext, fields)
            {
            }

            internal DatabaseRecord(IRContext irContext, IReadOnlyDictionary<string, FormulaValue> fields)
                : base(irContext, fields)
            {
            }

            protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
            {
                if (Environment.StackTrace.Contains("Microsoft.PowerFx.SymbolContext.GetScopeVar"))
                {
                    return base.TryGetField(fieldType, fieldName, out result);
                }

                throw new NotImplementedException("Cannot call TryGetField");
            }
        }

        internal class TestEntityType : FormulaType
        {
            internal TestEntityType()
                : base(DType.CreateExpandType(new TestExpandInfo()))
            {
            }

            public override void Visit(ITypeVisitor vistor)
            {
                throw new NotImplementedException("TestEntityType.Visit");
            }
        }

        internal class TestEntityValue : ValidFormulaValue
        {
            public TestEntityValue(IRContext irContext) 
                : base(irContext)
            {
            }

            public override object ToObject()
            {
                throw new NotImplementedException();
            }

            public override void Visit(IValueVisitor visitor)
            {
                throw new NotImplementedException("TestEntityValue.Visit");
            }
        }

        internal class ExternalDataEntityMetadataProvider : IExternalDataEntityMetadataProvider
        {
            public bool TryGetEntityMetadata(string expandInfoIdentity, out IDataEntityMetadata entityMetadata)
            {
                throw new NotImplementedException();
            }
        }

        internal class TestDelegationMetadata : IDelegationMetadata
        {
            public DType Schema => new TestEntityType()._type;

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

            internal TestDataSource()
            {
                ExternalDataEntityMetadataProvider = new ExternalDataEntityMetadataProvider();
            }

            public string Name => throw new NotImplementedException();

            public bool IsSelectable => throw new NotImplementedException();

            public bool IsDelegatable => throw new NotImplementedException();

            public bool RequiresAsync => throw new NotImplementedException();

            public IExternalDataEntityMetadataProvider DataEntityMetadataProvider => ExternalDataEntityMetadataProvider;

            public DataSourceKind Kind => throw new NotImplementedException();

            public IExternalTableMetadata TableMetadata => throw new NotImplementedException();

            public DelegationMetadata DelegationMetadata => throw new NotImplementedException();

            public DName EntityName => throw new NotImplementedException();

            public DType Type => throw new NotImplementedException();

            public bool IsPageable => throw new NotImplementedException();

            public TabularDataQueryOptions QueryOptions => throw new NotImplementedException();

            public bool IsConvertingDisplayNameMapping => throw new NotImplementedException();

            public BidirectionalDictionary<string, string> DisplayNameMapping => throw new NotImplementedException();

            public BidirectionalDictionary<string, string> PreviousDisplayNameMapping => throw new NotImplementedException();

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

            internal  TestExpandInfo()
            {
                DataSource = new TestDataSource();
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
    }
}
