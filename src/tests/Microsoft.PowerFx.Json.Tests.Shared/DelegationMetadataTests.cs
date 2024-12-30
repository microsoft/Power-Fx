// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class DelegationMetadataTests
    {
        [Fact]
        public void DelegationMetadata_Simple()
        {
            RecordType rt = RecordType.Empty().Add("a", FormulaType.String);
            DPath aPath = DPath.Root.Append(new DName("a"));

            Dictionary<DPath, DelegationCapability> columnCapabilities = new Dictionary<DPath, DelegationCapability>()
            {
                { aPath, DelegationCapability.Equal }
            };

            FilterOpMetadata fom = new FilterOpMetadata(rt.ToTable()._type, new Dictionary<DPath, DelegationCapability>(), columnCapabilities, DelegationCapability.None, null);
            IDelegationMetadata dm = new DelegationMetadata(rt._type, new List<OperationCapabilityMetadata>() { fom });

            Assert.True(dm.FilterDelegationMetadata.IsDelegationSupportedByColumn(aPath, DelegationCapability.Equal));
            Assert.False(dm.FilterDelegationMetadata.IsDelegationSupportedByColumn(aPath, DelegationCapability.Join));

            IDelegationMetadata dm2 = new DelegationMetadata(rt.ToTable(), dm, new DelegationMetadata(RecordType.Empty()._type, new List<OperationCapabilityMetadata>()), new List<string>(), new Dictionary<string, string>());

            Assert.True(dm2.FilterDelegationMetadata.IsDelegationSupportedByColumn(aPath, DelegationCapability.Equal));
            Assert.False(dm2.FilterDelegationMetadata.IsDelegationSupportedByColumn(aPath, DelegationCapability.Join));
        }

        [Fact]
        public void DelegationMetadata_WithRightColumns()
        {
            RecordType rt = RecordType.Empty().Add("a", FormulaType.String);
            RecordType rt2 = RecordType.Empty().Add("b", FormulaType.String);
            DPath aPath = DPath.Root.Append(new DName("a"));
            DPath bPath = DPath.Root.Append(new DName("b"));
            DPath cPath = DPath.Root.Append(new DName("c"));

            Dictionary<DPath, DelegationCapability> columnCapabilities = new Dictionary<DPath, DelegationCapability>()
            {
                { aPath, DelegationCapability.Equal }
            };

            Dictionary<DPath, DelegationCapability> columnCapabilities2 = new Dictionary<DPath, DelegationCapability>()
            {
                { bPath, DelegationCapability.Equal },
                { cPath, DelegationCapability.Join }
            };

            // left
            FilterOpMetadata fom = new FilterOpMetadata(rt._type, new Dictionary<DPath, DelegationCapability>(), columnCapabilities, DelegationCapability.None, null);
            IDelegationMetadata dm = new DelegationMetadata(rt._type, new List<OperationCapabilityMetadata>() { fom });

            // right
            FilterOpMetadata fom2 = new FilterOpMetadata(rt2._type, new Dictionary<DPath, DelegationCapability>(), columnCapabilities2, DelegationCapability.None, null);
            IDelegationMetadata dm2 = new DelegationMetadata(rt2._type, new List<OperationCapabilityMetadata>() { fom2 });

            // composed            
            IDelegationMetadata dm3 = new DelegationMetadata(rt.ToTable(), dm, dm2, new List<string>() { "b" }, new Dictionary<string, string>());

            Assert.True(dm3.FilterDelegationMetadata.IsDelegationSupportedByColumn(aPath, DelegationCapability.Equal));
            Assert.False(dm3.FilterDelegationMetadata.IsDelegationSupportedByColumn(aPath, DelegationCapability.Join));
            Assert.True(dm3.FilterDelegationMetadata.IsDelegationSupportedByColumn(bPath, DelegationCapability.Equal));
            Assert.False(dm3.FilterDelegationMetadata.IsDelegationSupportedByColumn(bPath, DelegationCapability.Join));

            Assert.False(dm3.FilterDelegationMetadata.IsDelegationSupportedByColumn(cPath, DelegationCapability.Equal));
            Assert.False(dm3.FilterDelegationMetadata.IsDelegationSupportedByColumn(cPath, DelegationCapability.Join));
            
            IDelegationMetadata dm4 = new DelegationMetadata(rt.ToTable(), dm, dm2, new List<string>() { "b", "c" }, new Dictionary<string, string>());

            Assert.False(dm4.FilterDelegationMetadata.IsDelegationSupportedByColumn(cPath, DelegationCapability.Equal));
            Assert.True(dm4.FilterDelegationMetadata.IsDelegationSupportedByColumn(cPath, DelegationCapability.Join));
        }

        [Fact]
        public void DelegationMetadata_WithRenames()
        {
            RecordType rt = RecordType.Empty().Add("a", FormulaType.String).Add("b", FormulaType.String).Add("d", FormulaType.String);
            DPath aPath = DPath.Root.Append(new DName("a"));
            DPath bPath = DPath.Root.Append(new DName("b"));
            DPath cPath = DPath.Root.Append(new DName("c"));
            DPath dPath = DPath.Root.Append(new DName("d"));

            Dictionary<DPath, DelegationCapability> columnCapabilities = new Dictionary<DPath, DelegationCapability>()
            {
                { aPath, DelegationCapability.Equal },
                { bPath, DelegationCapability.Join },
            };

            FilterOpMetadata fom = new FilterOpMetadata(rt._type, new Dictionary<DPath, DelegationCapability>(), columnCapabilities, DelegationCapability.Add, null);
            IDelegationMetadata dm = new DelegationMetadata(rt._type, new List<OperationCapabilityMetadata>() { fom });
            
            // rename column a to c
            IDelegationMetadata dm2 = new DelegationMetadata(rt.ToTable(), dm, null, null, new Dictionary<string, string>() { { "c", "a" } });

            Assert.False(dm2.FilterDelegationMetadata.IsDelegationSupportedByColumn(aPath, DelegationCapability.Equal));
            Assert.True(dm2.FilterDelegationMetadata.IsDelegationSupportedByColumn(cPath, DelegationCapability.Equal));
            Assert.True(dm2.FilterDelegationMetadata.IsDelegationSupportedByColumn(bPath, DelegationCapability.Join));

            Assert.False(dm2.FilterDelegationMetadata.IsDelegationSupportedByColumn(aPath, DelegationCapability.Add));
            Assert.True(dm2.FilterDelegationMetadata.IsDelegationSupportedByColumn(dPath, DelegationCapability.Add));            
        }
    }
}
