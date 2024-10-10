// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class CapabilityTests
    {
        [Fact]
        public void CapabilityTest()
        {
            RecordType rt = new TestRecordType("myTable", RecordType.Empty().Add("logic", FormulaType.String, "display"), null);
            
            Assert.NotNull(rt._type.AssociatedDataSources);
            Assert.NotEmpty(rt._type.AssociatedDataSources);

            IExternalTabularDataSource externalDataSource = rt._type.AssociatedDataSources.First();

            Assert.True(externalDataSource.IsDelegatable);

            IDelegationMetadata delegationMetadata = externalDataSource.DelegationMetadata;

            bool eq = delegationMetadata.FilterDelegationMetadata.IsBinaryOpInDelegationSupportedByColumn(BinaryOp.Equal, DPath.Root.Append(new DName("logic")));
            bool neq = delegationMetadata.FilterDelegationMetadata.IsBinaryOpInDelegationSupportedByColumn(BinaryOp.NotEqual, DPath.Root.Append(new DName("logic")));
            bool eq2 = delegationMetadata.FilterDelegationMetadata.IsBinaryOpInDelegationSupportedByColumn(BinaryOp.Equal, DPath.Root.Append(new DName("logic2")));
            bool neq2 = delegationMetadata.FilterDelegationMetadata.IsBinaryOpInDelegationSupportedByColumn(BinaryOp.NotEqual, DPath.Root.Append(new DName("logic2")));

            Assert.True(eq);
            Assert.False(neq);
            Assert.False(eq2);
            Assert.False(neq2);
        }
    }

    public class TestRecordType : RecordType
    {
        private readonly RecordType _recordType;
        private readonly List<string> _allowedFilters;

        public TestRecordType(string tableName, RecordType recordType, List<string> allowedFilters)
            : base(GetDisplayNameProvider(recordType), GetDelegationInfo(tableName, recordType))
        {
            _recordType = recordType;
            _allowedFilters = allowedFilters;
        }   

        public override bool TryGetFieldType(string fieldName, out FormulaType type)
        {
            return _recordType.TryGetFieldType(fieldName, out type);
        }

        private static DisplayNameProvider GetDisplayNameProvider(RecordType recordType)
        {
            return DisplayNameProvider.New(recordType.FieldNames.Select(f => new KeyValuePair<DName, DName>(new DName(f), new DName(f))));
        }

        private static TableDelegationInfo GetDelegationInfo(string tableName, RecordType recordType)
        {
            return new TestDelegationInfo(recordType)
            {
                TableName = tableName
            };
        }

        public override bool Equals(object other)
        {
            if (other == null || other is not TestRecordType other2)
            {
                return false;
            }

            return _recordType == other2._recordType;
        }

#pragma warning disable CA1065 // Exceptions should not be raised in this type of method.
        public override int GetHashCode() => throw new NotImplementedException();
#pragma warning restore CA1065
    }

    public class TestDelegationInfo : TableDelegationInfo
    {
        private readonly RecordType _recordType;

        public TestDelegationInfo(RecordType recordType)
            : base()
        {
            _recordType = recordType;
        }

        public override bool IsDelegable => true;

        public override ColumnCapabilitiesDefinition GetColumnCapability(string fieldName)
        {
            if (_recordType.TryGetFieldType(fieldName, out FormulaType ft))
            {
                return new ColumnCapabilitiesDefinition()
                {
                    FilterFunctions = new List<string>() { "eq" }
                };
            }

            return null;
        }
    }
}
