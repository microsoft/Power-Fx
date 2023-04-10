// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    public class DTypeTests
    {
        private static DType AttachmentTableType => DType.CreateAttachmentType(DType.CreateTable(new TypedName(DType.String, new DName("DisplayName"))));

        private static DType AttachmentRecordType => DType.CreateAttachmentType(DType.CreateRecord(new TypedName(DType.Number, new DName("Wrapped"))));

        private static IExternalEntity _optionSet;

        private static DType OptionSetType
        {
            get
            {
                if (_optionSet == null)
                {
                    var mapping = new Dictionary<string, string>
                    {
                        { "1", "Active" },
                        { "2", "Inactive" },
                        { "3", "Away" }
                    };

                    _optionSet = new OptionSet("Status", DisplayNameUtility.MakeUnique(mapping));
                }

                return _optionSet.Type;
            }
        }

        internal static DType OptionSetValueType => DType.CreateOptionSetValueType(OptionSetType.OptionSetInfo);

        private static DType _booleanOptionSetType;

        internal class BoolOptionSetInfo : IExternalOptionSet
        {
            public DisplayNameProvider DisplayNameProvider => DisplayNameUtility.MakeUnique(new Dictionary<string, string>
            {
                { "Yes", "Yes" },
                { "No", "No" },
            });

            public IEnumerable<DName> OptionNames => new[] { new DName("No"), new DName("Yes") };

            public DKind BackingKind => DKind.Boolean;

            public bool IsConvertingDisplayNameMapping => false;

            public DName EntityName => new DName("BoolOptionSet");

            public DType Type => DType.CreateOptionSetType(this);

            public OptionSetValueType OptionSetValueType => new OptionSetValueType(this);

            public bool TryGetValue(DName fieldName, out OptionSetValue optionSetValue)
            {
                if (fieldName.Value == "No" || fieldName.Value == "Yes")
                {
                    optionSetValue = new OptionSetValue(fieldName.Value, this.OptionSetValueType, fieldName.Value == "Yes");
                    return true;
                }

                optionSetValue = null;
                return false;
            }
        }

        [Fact]
        public void BoolOptionSetTryGetValueSucceeds()
        {
            var boolOs = new BoolOptionSetInfo();
            Assert.True(boolOs.OptionSetValueType.TryGetValue("Yes", out var optionSetValue));
            Assert.Equal(true, optionSetValue.ExecutionValue);

            Assert.True(boolOs.TryGetValue(new DName("Yes"), out var directValue));
            Assert.Equal(true, directValue.ExecutionValue);
        }

        private static DType BooleanValuedOptionSetType
        {
            get
            {
                if (_booleanOptionSetType == null)
                {
                    _booleanOptionSetType = DType.CreateOptionSetType(new BoolOptionSetInfo());
                }

                return _booleanOptionSetType;
            }
        }

        internal static DType BooleanValuedOptionSetValueType => DType.CreateOptionSetValueType(BooleanValuedOptionSetType.OptionSetInfo);

        private static DType MultiSelectOptionSetType
        {
            get
            {
                var optionSetColumn = new TypedName(DType.OptionSetValue, new DName("Value"));
                return DType.CreateTable(optionSetColumn);
            }
        }

        // NOTE: Deferred type and void type is not included in this list due to their special nature.
        private static readonly DType[] _dTypes = new[]
            {
                DType.Unknown, DType.Error, DType.Number, DType.Boolean, DType.String, DType.Hyperlink, DType.Image,
                DType.PenImage, DType.Media, DType.Blob, DType.Color, DType.Currency, DType.EmptyRecord, DType.EmptyTable,
                DType.EmptyEnum, DType.Date, DType.Time, DType.Guid, DType.Polymorphic, DType.Deferred, AttachmentTableType,
                AttachmentRecordType, OptionSetType, MultiSelectOptionSetType, DType.ObjNull, DType.OptionSet,
                DType.OptionSetValue, DType.View, DType.ViewValue, DType.UntypedObject, DType.Void, DType.Decimal,
            };

        [Fact]
        public void MaxDepth()
        {
            Assert.Equal(0, new DType(DKind.Number).MaxDepth);
            Assert.Equal(0, new DType(DKind.String).MaxDepth);
            Assert.Equal(0, new DType(DKind.Boolean).MaxDepth);
            Assert.Equal(0, new DType(DKind.DateTime).MaxDepth);
            Assert.Equal(0, new DType(DKind.Date).MaxDepth);
            Assert.Equal(0, new DType(DKind.Time).MaxDepth);
            Assert.Equal(0, new DType(DKind.Currency).MaxDepth);
            Assert.Equal(0, new DType(DKind.Decimal).MaxDepth);
            Assert.Equal(0, new DType(DKind.Image).MaxDepth);
            Assert.Equal(0, new DType(DKind.PenImage).MaxDepth);
            Assert.Equal(0, new DType(DKind.Media).MaxDepth);
            Assert.Equal(0, new DType(DKind.Blob).MaxDepth);
            Assert.Equal(0, new DType(DKind.Hyperlink).MaxDepth);
            Assert.Equal(0, new DType(DKind.Color).MaxDepth);
            Assert.Equal(0, new DType(DKind.Guid).MaxDepth);
            Assert.Equal(0, new DType(DKind.Control).MaxDepth);
            Assert.Equal(0, new DType(DKind.DataEntity).MaxDepth);
            Assert.Equal(0, new DType(DKind.Polymorphic).MaxDepth);
            Assert.Equal(1, new DType(DKind.Record).MaxDepth);
            Assert.Equal(1, new DType(DKind.Table).MaxDepth);

            DType.TryParse("*[A:n, B:s, C:b]", out var dType1);
            Assert.Equal(1, dType1.MaxDepth);

            var metaFieldName = "'meta-6de62757-ecb6-4be6-bb85-349b3c7938a9'";
            DType.TryParse("*[" + metaFieldName + ":![A:n, B:s, C:b]", out var dType2);
            Assert.Equal(0, dType2.MaxDepth);

            DType.TryParse("*[A:![A:n]]", out var dType3);
            Assert.Equal(2, dType3.MaxDepth);
            DType.TryParse("*[A:![B:*[C:n]]]", out var dType4);
            Assert.Equal(3, dType4.MaxDepth);
            DType.TryParse("*[X:*[Y:n], A:![B:*[C:n]]]", out var dType5);
            Assert.Equal(3, dType5.MaxDepth);
        }

        [Fact]
        public void DTypeToStringRepresentation()
        {
            Assert.Equal("?", DType.Unknown.ToString());
            Assert.Equal("e", DType.Error.ToString());
            Assert.Equal("x", DType.Invalid.ToString());

            Assert.Equal("b", DType.Boolean.ToString());
            Assert.Equal("n", DType.Number.ToString());
            Assert.Equal("s", DType.String.ToString());

            Assert.Equal("h", DType.Hyperlink.ToString());
            Assert.Equal("d", DType.DateTime.ToString());
            Assert.Equal("c", DType.Color.ToString());
            Assert.Equal("$", DType.Currency.ToString());
            Assert.Equal("i", DType.Image.ToString());
            Assert.Equal("p", DType.PenImage.ToString());
            Assert.Equal("m", DType.Media.ToString());
            Assert.Equal("g", DType.Guid.ToString());
            Assert.Equal("o", DType.Blob.ToString());
            Assert.Equal("r*", AttachmentTableType.ToString());
            Assert.Equal("r!", AttachmentRecordType.ToString());
            Assert.Equal("T", DType.Time.ToString());
            Assert.Equal("D", DType.Date.ToString());
            Assert.Equal("N", DType.ObjNull.ToString());
            Assert.Equal("P", DType.Polymorphic.ToString());
            Assert.Equal("V", DType.NamedValue.ToString());
            Assert.Equal("X", DType.Deferred.ToString());
            Assert.Equal("-", DType.Void.ToString());
            Assert.Equal("w", DType.Decimal.ToString());
        }

        [Fact]
        public void DTypeCorrectDKind()
        {
            Assert.Equal(DKind.Unknown, DType.Unknown.Kind);
            Assert.Equal(DKind.Error, DType.Error.Kind);
            Assert.Equal(DKind.Number, DType.Number.Kind);
            Assert.Equal(DKind.Boolean, DType.Boolean.Kind);
            Assert.Equal(DKind.String, DType.String.Kind);
            Assert.Equal(DKind.Hyperlink, DType.Hyperlink.Kind);
            Assert.Equal(DKind.Image, DType.Image.Kind);
            Assert.Equal(DKind.PenImage, DType.PenImage.Kind);
            Assert.Equal(DKind.Media, DType.Media.Kind);
            Assert.Equal(DKind.Guid, DType.Guid.Kind);
            Assert.Equal(DKind.Blob, DType.Blob.Kind);
            Assert.Equal(DKind.Color, DType.Color.Kind);
            Assert.Equal(DKind.Currency, DType.Currency.Kind);
            Assert.Equal(DKind.Decimal, DType.Decimal.Kind);
            Assert.Equal(DKind.DateTime, DType.DateTime.Kind);
            Assert.Equal(DKind.Record, DType.EmptyRecord.Kind);
            Assert.Equal(DKind.Table, DType.EmptyTable.Kind);
            Assert.Equal(DKind.Enum, DType.EmptyEnum.Kind);
            Assert.Equal(DKind.LazyTable, AttachmentTableType.Kind);
            Assert.Equal(DKind.LazyRecord, AttachmentRecordType.Kind);
            Assert.Equal(DKind.OptionSet, OptionSetType.Kind);
            Assert.Equal(DKind.Table, MultiSelectOptionSetType.Kind);
            Assert.Equal(DKind.Date, DType.Date.Kind);
            Assert.Equal(DKind.Time, DType.Time.Kind);
            Assert.Equal(DKind.Polymorphic, DType.Polymorphic.Kind);
            Assert.Equal(DKind.NamedValue, DType.NamedValue.Kind);
            Assert.Equal(DKind.Deferred, DType.Deferred.Kind);
            Assert.Equal(DKind.Void, DType.Void.Kind);
        }

        [Fact]
        public void ErrorIsSupertypeOfAll()
        {
            foreach (var dType in _dTypes)
            {
                foreach (var usePowerFxV1CompatRules in new[] { false, true })
                {
                    if (dType != DType.Void)
                    {
                        Assert.True(DType.Error.Accepts(dType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatRules));
                    }
                }
            }
        }

        [Fact]
        public void UnknownIsSubtypeOfAll()
        {
            foreach (var dType in _dTypes)
            {
                foreach (var usePowerFxV1CompatRules in new[] { false, true })
                {
                    if (dType != DType.Void)
                    {
                        Assert.True(dType.Accepts(DType.Unknown, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatRules));
                    }
                }
            }
        }

        [Fact]
        public void DeferredIsSubtypeOfAll()
        {
            foreach (var dType in _dTypes)
            {
                foreach (var usePowerFxV1CompatRules in new[] { false, true })
                {
                    // Deferred is subtype of all except unknown and void.
                    if (dType != DType.Unknown && dType != DType.Void)
                    {
                        Assert.True(dType.Accepts(DType.Deferred, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatRules));
                    }
                }
            }
        }

        [Fact]
        public void VoidIsNotSubtypeOfAny()
        {
            foreach (var dType in _dTypes)
            {
                foreach (var usePowerFxV1CompatRules in new[] { false, true })
                {
                    Assert.False(dType.Accepts(DType.Void, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatRules));
                }
            }
        }

        [Fact]
        public void VoidIsNotSupertypeOfAny()
        {
            foreach (var dType in _dTypes)
            {
                foreach (var usePowerFxV1CompatRules in new[] { false, true })
                {
                    Assert.False(DType.Void.Accepts(dType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatRules));
                }
            }
        }

        [Fact]
        public void AttachmentTypeAcceptanceTest()
        {
            foreach (var usePowerFxV1CompatRules in new[] { false, true })
            {
                var type1 = DType.CreateAttachmentType(DType.CreateAttachmentType(DType.CreateTable(new TypedName(DType.String, new DName("DisplayName")))));
                var type2 = DType.CreateAttachmentType(DType.CreateAttachmentType(DType.CreateTable(new TypedName(DType.String, new DName("DisplayName")))));

                Assert.True(type1.Accepts(type2, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatRules));

                type1 = DType.CreateAttachmentType(DType.CreateAttachmentType(DType.CreateTable(new TypedName(DType.String, new DName("Name")))));
                Assert.False(type1.Accepts(type2, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatRules));

                type2 = DType.CreateAttachmentType(DType.CreateAttachmentType(DType.CreateRecord(new TypedName(DType.String, new DName("DisplayName")))));
                Assert.False(type2.Accepts(type1, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatRules));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DTypeAcceptanceTest(bool usePowerFxV1CompatibilityRules)
        {
            Assert.False(DType.Unknown.Accepts(DType.Number, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            Assert.True(DType.Number.Accepts(DType.Number, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.Equal(!usePowerFxV1CompatibilityRules, DType.Number.Accepts(DType.Currency, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Number.Accepts(DType.Decimal, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Number.Accepts(DType.DateTime, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Number.Accepts(DType.Date, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Number.Accepts(DType.Time, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Number.Accepts(DType.EmptyEnum, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.TryParse("%n[A:1, B:2]", out DType type) && type.IsEnum && DType.Number.Accepts(type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            Assert.False(DType.Decimal.Accepts(DType.Number, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Decimal.Accepts(DType.Currency, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Decimal.Accepts(DType.Decimal, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Decimal.Accepts(DType.DateTime, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Decimal.Accepts(DType.Date, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Decimal.Accepts(DType.Time, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Decimal.Accepts(DType.EmptyEnum, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.TryParse("%w[A:1, B:2]", out type) && type.IsEnum && DType.Decimal.Accepts(type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            Assert.True(DType.Boolean.Accepts(DType.Boolean, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Boolean.Accepts(DType.EmptyEnum, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.TryParse("%b[A:true, B:false]", out type) && type.IsEnum && DType.Boolean.Accepts(type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            Assert.True(DType.String.Accepts(DType.String, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.Equal(!usePowerFxV1CompatibilityRules, DType.String.Accepts(DType.Hyperlink, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.Equal(!usePowerFxV1CompatibilityRules, DType.String.Accepts(DType.Guid, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.Equal(!usePowerFxV1CompatibilityRules, DType.String.Accepts(DType.Image, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.Equal(!usePowerFxV1CompatibilityRules, DType.String.Accepts(DType.PenImage, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.Equal(!usePowerFxV1CompatibilityRules, DType.String.Accepts(DType.Media, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.Equal(!usePowerFxV1CompatibilityRules, DType.String.Accepts(DType.Blob, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.String.Accepts(DType.EmptyEnum, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.TryParse("%s[A:\"a\", B:\"b\"]", out type) && type.IsEnum && DType.String.Accepts(type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            Assert.True(DType.Hyperlink.Accepts(DType.Hyperlink, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.Equal(!usePowerFxV1CompatibilityRules, DType.Hyperlink.Accepts(DType.Image, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.Equal(!usePowerFxV1CompatibilityRules, DType.Hyperlink.Accepts(DType.Media, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.Equal(!usePowerFxV1CompatibilityRules, DType.Hyperlink.Accepts(DType.Blob, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Hyperlink.Accepts(DType.EmptyEnum, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            Assert.True(DType.Image.Accepts(DType.Image, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Image.Accepts(DType.EmptyEnum, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            Assert.True(DType.Media.Accepts(DType.Media, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Media.Accepts(DType.EmptyEnum, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            Assert.True(DType.Blob.Accepts(DType.Blob, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Blob.Accepts(DType.EmptyEnum, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            Assert.True(DType.Color.Accepts(DType.Color, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Color.Accepts(DType.EmptyEnum, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            Assert.True(DType.Currency.Accepts(DType.Currency, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Currency.Accepts(DType.EmptyEnum, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            Assert.True(DType.DateTime.Accepts(DType.DateTime, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.Equal(!usePowerFxV1CompatibilityRules, DType.DateTime.Accepts(DType.Date, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.Equal(!usePowerFxV1CompatibilityRules, DType.DateTime.Accepts(DType.Time, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.DateTime.Accepts(DType.EmptyEnum, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            Assert.True(DType.Date.Accepts(DType.Date, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Date.Accepts(DType.EmptyEnum, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            Assert.True(DType.Time.Accepts(DType.Time, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Time.Accepts(DType.EmptyEnum, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
        }

        [Fact]
        public void TestDropAllOfKind()
        {
            DType type1 = TestUtils.DT("*[A:n, B:n, C:s]");

            var fError = false;
            var newType = type1.DropAllOfKind(ref fError, DPath.Root, DKind.Number);
            Assert.False(fError);
            Assert.Equal(TestUtils.DT("*[C:s]"), newType);

            fError = false;
            newType = DType.Number.DropAllOfKind(ref fError, DPath.Root, DKind.Number);
            Assert.True(fError);
            Assert.Equal(TestUtils.DT("n"), newType);

            DType type5 = type1.Add(new DName("Attachments"), AttachmentTableType);
            fError = false;
            newType = type5.DropAllMatching(ref fError, DPath.Root, type => type.IsAttachment);
            Assert.Equal(TestUtils.DT("*[A:n, B:n, C:s]"), newType);

            DType type6 = type1.Add(new DName("Polymorphic"), DType.Polymorphic);
            fError = false;
            newType = type6.DropAllOfKind(ref fError, DPath.Root, DKind.Polymorphic);
            Assert.Equal(TestUtils.DT("*[A:n, B:n, C:s]"), newType);

            DType type7 = type1.Add(new DName("Attachments"), AttachmentRecordType);
            fError = false;
            newType = type7.DropAllMatching(ref fError, DPath.Root, type => type.IsAttachment);
            Assert.Equal(TestUtils.DT("*[A:n, B:n, C:s]"), newType);

            // Safe to call when type isn't present
            fError = false;
            newType = type1.DropAllMatching(ref fError, DPath.Root, type => type.IsAttachment);
            Assert.False(fError);
            Assert.Equal(TestUtils.DT("*[A:n, B:n, C:s]"), newType);
        }

        [Fact]
        public void DTypeAcceptanceTest_Negative()
        {
            foreach (var usePowerFxV1CompatibilityRules in new[] { false, true })
            {
                Assert.False(DType.Number.Accepts(DType.String, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Number.Accepts(DType.Hyperlink, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Number.Accepts(DType.Guid, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Number.Accepts(DType.Image, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Number.Accepts(DType.Media, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Number.Accepts(DType.Blob, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Number.Accepts(DType.Boolean, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Number.Accepts(DType.EmptyTable, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Number.Accepts(DType.EmptyRecord, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Number.Accepts(DType.Decimal, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

                Assert.False(DType.Deferred.Accepts(DType.String, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Deferred.Accepts(DType.Hyperlink, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Deferred.Accepts(DType.Guid, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Deferred.Accepts(DType.Image, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Deferred.Accepts(DType.Media, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Deferred.Accepts(DType.Blob, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Deferred.Accepts(DType.Boolean, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Deferred.Accepts(DType.EmptyTable, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Deferred.Accepts(DType.EmptyRecord, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Deferred.Accepts(DType.Number, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

                Assert.False(DType.Boolean.Accepts(DType.String, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Boolean.Accepts(DType.Number, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Boolean.Accepts(DType.Color, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Boolean.Accepts(DType.Currency, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Boolean.Accepts(DType.Decimal, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Boolean.Accepts(DType.DateTime, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Boolean.Accepts(DType.Date, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Boolean.Accepts(DType.Time, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Boolean.Accepts(DType.Media, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Boolean.Accepts(DType.Blob, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Boolean.Accepts(DType.Hyperlink, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Boolean.Accepts(DType.Image, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Boolean.Accepts(DType.EmptyTable, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Boolean.Accepts(DType.EmptyRecord, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Boolean.Accepts(DType.Guid, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

                Assert.False(DType.String.Accepts(DType.Number, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.String.Accepts(DType.Color, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.String.Accepts(DType.Currency, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.String.Accepts(DType.Decimal, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.String.Accepts(DType.DateTime, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.String.Accepts(DType.Date, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.String.Accepts(DType.Time, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.String.Accepts(DType.Boolean, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.String.Accepts(DType.EmptyRecord, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.String.Accepts(DType.EmptyTable, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

                Assert.False(DType.Image.Accepts(DType.Boolean, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Image.Accepts(DType.Number, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Image.Accepts(DType.String, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Image.Accepts(DType.DateTime, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Image.Accepts(DType.Date, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Image.Accepts(DType.Time, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Image.Accepts(DType.Hyperlink, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Image.Accepts(DType.Currency, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Image.Accepts(DType.Decimal, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Image.Accepts(DType.Media, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Image.Accepts(DType.Color, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Image.Accepts(DType.EmptyRecord, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Image.Accepts(DType.EmptyTable, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Image.Accepts(DType.Guid, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

                Assert.False(DType.PenImage.Accepts(DType.Boolean, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.PenImage.Accepts(DType.Number, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.PenImage.Accepts(DType.String, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.PenImage.Accepts(DType.Image, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.PenImage.Accepts(DType.DateTime, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.PenImage.Accepts(DType.Date, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.PenImage.Accepts(DType.Time, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.PenImage.Accepts(DType.Hyperlink, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.PenImage.Accepts(DType.Currency, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.PenImage.Accepts(DType.Decimal, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.PenImage.Accepts(DType.Media, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.PenImage.Accepts(DType.Blob, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.PenImage.Accepts(DType.Color, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.PenImage.Accepts(DType.EmptyRecord, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.PenImage.Accepts(DType.EmptyTable, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.PenImage.Accepts(DType.Guid, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

                Assert.False(DType.Media.Accepts(DType.Boolean, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Media.Accepts(DType.Number, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Media.Accepts(DType.String, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Media.Accepts(DType.DateTime, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Media.Accepts(DType.Date, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Media.Accepts(DType.Time, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Media.Accepts(DType.Image, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Media.Accepts(DType.Hyperlink, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Media.Accepts(DType.Currency, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Media.Accepts(DType.Decimal, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Media.Accepts(DType.Color, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Media.Accepts(DType.EmptyRecord, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Media.Accepts(DType.EmptyTable, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Media.Accepts(DType.Guid, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

                Assert.False(DType.Blob.Accepts(DType.Boolean, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Blob.Accepts(DType.Number, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Blob.Accepts(DType.String, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Blob.Accepts(DType.DateTime, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Blob.Accepts(DType.Date, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Blob.Accepts(DType.Time, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Blob.Accepts(DType.Image, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Blob.Accepts(DType.Hyperlink, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Blob.Accepts(DType.Currency, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Blob.Accepts(DType.Color, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Blob.Accepts(DType.EmptyRecord, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Blob.Accepts(DType.EmptyTable, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Blob.Accepts(DType.Guid, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

                Assert.False(DType.Hyperlink.Accepts(DType.Boolean, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Hyperlink.Accepts(DType.Number, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Hyperlink.Accepts(DType.String, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Hyperlink.Accepts(DType.DateTime, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Hyperlink.Accepts(DType.Date, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Hyperlink.Accepts(DType.Time, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Hyperlink.Accepts(DType.Currency, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Hyperlink.Accepts(DType.Decimal, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Hyperlink.Accepts(DType.Color, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Hyperlink.Accepts(DType.EmptyRecord, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Hyperlink.Accepts(DType.EmptyTable, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Hyperlink.Accepts(DType.Guid, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

                Assert.False(DType.DateTime.Accepts(DType.Boolean, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.DateTime.Accepts(DType.Number, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.DateTime.Accepts(DType.String, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.DateTime.Accepts(DType.Hyperlink, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.DateTime.Accepts(DType.Image, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.DateTime.Accepts(DType.Media, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.DateTime.Accepts(DType.Blob, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.DateTime.Accepts(DType.Currency, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.DateTime.Accepts(DType.Color, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.DateTime.Accepts(DType.EmptyRecord, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.DateTime.Accepts(DType.EmptyTable, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.DateTime.Accepts(DType.Guid, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

                Assert.False(DType.Date.Accepts(DType.Boolean, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Date.Accepts(DType.Number, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Date.Accepts(DType.String, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Date.Accepts(DType.Hyperlink, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Date.Accepts(DType.Image, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Date.Accepts(DType.Media, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Date.Accepts(DType.Blob, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Date.Accepts(DType.Currency, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Date.Accepts(DType.Decimal, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Date.Accepts(DType.Color, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Date.Accepts(DType.EmptyRecord, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Date.Accepts(DType.EmptyTable, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Date.Accepts(DType.DateTime, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Date.Accepts(DType.Time, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Date.Accepts(DType.Guid, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

                Assert.False(DType.Time.Accepts(DType.Boolean, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Time.Accepts(DType.Number, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Time.Accepts(DType.String, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Time.Accepts(DType.Hyperlink, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Time.Accepts(DType.Image, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Time.Accepts(DType.Media, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Time.Accepts(DType.Blob, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Time.Accepts(DType.Currency, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Time.Accepts(DType.Decimal, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Time.Accepts(DType.Color, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Time.Accepts(DType.EmptyRecord, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Time.Accepts(DType.EmptyTable, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Time.Accepts(DType.DateTime, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Time.Accepts(DType.Date, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Time.Accepts(DType.Guid, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

                Assert.False(DType.Currency.Accepts(DType.Boolean, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Currency.Accepts(DType.Number, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Currency.Accepts(DType.Decimal, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Currency.Accepts(DType.String, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Currency.Accepts(DType.Hyperlink, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Currency.Accepts(DType.Image, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Currency.Accepts(DType.Media, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Currency.Accepts(DType.Blob, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Currency.Accepts(DType.DateTime, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Currency.Accepts(DType.Date, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Currency.Accepts(DType.Color, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Currency.Accepts(DType.EmptyRecord, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Currency.Accepts(DType.EmptyTable, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Currency.Accepts(DType.Guid, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

                Assert.False(DType.Decimal.Accepts(DType.Boolean, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Decimal.Accepts(DType.Number, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Decimal.Accepts(DType.Currency, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Decimal.Accepts(DType.String, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Decimal.Accepts(DType.Hyperlink, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Decimal.Accepts(DType.Image, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Decimal.Accepts(DType.Media, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Decimal.Accepts(DType.Blob, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Decimal.Accepts(DType.DateTime, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Decimal.Accepts(DType.Date, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Decimal.Accepts(DType.Color, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Decimal.Accepts(DType.EmptyRecord, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Decimal.Accepts(DType.EmptyTable, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Decimal.Accepts(DType.Guid, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

                Assert.False(DType.Color.Accepts(DType.Boolean, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Color.Accepts(DType.Number, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Color.Accepts(DType.String, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Color.Accepts(DType.Hyperlink, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Color.Accepts(DType.Image, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Color.Accepts(DType.Media, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Color.Accepts(DType.Blob, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Color.Accepts(DType.DateTime, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Color.Accepts(DType.Date, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Color.Accepts(DType.Currency, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Color.Accepts(DType.Decimal, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Color.Accepts(DType.EmptyRecord, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Color.Accepts(DType.EmptyTable, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(DType.Color.Accepts(DType.Guid, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

                Assert.False(DType.EmptyRecord.Accepts(AttachmentTableType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(AttachmentTableType.Accepts(DType.EmptyRecord, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

                Assert.True(DType.EmptyTable.Accepts(AttachmentTableType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(AttachmentTableType.Accepts(DType.EmptyTable, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

                Assert.True(DType.EmptyRecord.Accepts(AttachmentTableType.ToRecord(), exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

                Assert.True(DType.EmptyRecord.Accepts(AttachmentRecordType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(AttachmentRecordType.Accepts(DType.EmptyRecord, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

                Assert.False(DType.EmptyTable.Accepts(AttachmentRecordType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(AttachmentRecordType.Accepts(DType.EmptyTable, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

                Assert.True(DType.EmptyTable.Accepts(AttachmentRecordType.ToTable(), exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            }
        }

        [Fact]
        public void DefaultDTypeIsInvalid()
        {
            Assert.False(DType.Invalid.IsValid);
        }

        [Fact]
        public void NonHierarchicalDTypes()
        {
            Assert.True(DType.Unknown.ChildCount == 0);
            Assert.True(DType.Deferred.ChildCount == 0);
            Assert.True(DType.Error.ChildCount == 0);
            Assert.True(DType.Number.ChildCount == 0);
            Assert.True(DType.String.ChildCount == 0);
            Assert.True(DType.Boolean.ChildCount == 0);
            Assert.True(DType.DateTime.ChildCount == 0);
            Assert.True(DType.Date.ChildCount == 0);
            Assert.True(DType.Time.ChildCount == 0);
            Assert.True(DType.Hyperlink.ChildCount == 0);
            Assert.True(DType.Color.ChildCount == 0);
            Assert.True(DType.Image.ChildCount == 0);
            Assert.True(DType.PenImage.ChildCount == 0);
            Assert.True(DType.Media.ChildCount == 0);
            Assert.True(DType.Blob.ChildCount == 0);
            Assert.True(DType.Currency.ChildCount == 0);
            Assert.True(DType.Decimal.ChildCount == 0);
            Assert.True(DType.Guid.ChildCount == 0);
            Assert.True(DType.Polymorphic.ChildCount == 0);
            Assert.True(DType.Void.ChildCount == 0);
        }

        [Fact]
        public void AggregateDTypes()
        {
            Assert.True(DType.EmptyRecord.IsAggregate);
            Assert.True(DType.EmptyTable.IsAggregate);
            Assert.True(DType.ObjNull.IsAggregate);

            Assert.False(DType.EmptyEnum.IsAggregate);
            Assert.False(DType.Number.IsAggregate);
            Assert.False(DType.Boolean.IsAggregate);
            Assert.False(DType.String.IsAggregate);
            Assert.False(DType.DateTime.IsAggregate);
            Assert.False(DType.Date.IsAggregate);
            Assert.False(DType.Time.IsAggregate);
            Assert.False(DType.Hyperlink.IsAggregate);
            Assert.False(DType.Currency.IsAggregate);
            Assert.False(DType.Decimal.IsAggregate);
            Assert.False(DType.Image.IsAggregate);
            Assert.False(DType.PenImage.IsAggregate);
            Assert.False(DType.Media.IsAggregate);
            Assert.False(DType.Blob.IsAggregate);
            Assert.False(DType.Color.IsAggregate);
            Assert.False(DType.Guid.IsAggregate);
            Assert.False(DType.Polymorphic.IsAggregate);
            Assert.True(AttachmentTableType.IsAggregate);
            Assert.True(AttachmentRecordType.IsAggregate);

            Assert.True(DType.TryParse("%n[A:1,B:2]", out DType type));
            Assert.False(type.IsAggregate);
        }

        [Fact]
        public void AggregateDTypesWithCollidingFields()
        {
            Assert.False(DType.TryParse("*[X:n, X:s]", out DType type));
            Assert.False(DType.TryParse("*[X:n, X:n]", out type));
            Assert.False(DType.TryParse("*[X:n, X:n, X:n]", out type));
            Assert.False(DType.TryParse("*[X:s, X:n, X:b]", out type));
            Assert.False(DType.TryParse("*[X:*[Y:n, Y:n]]", out type));
            Assert.False(DType.TryParse("*[X:*[Y:n, Y:s]]", out type));

            Assert.True(DType.TryParse("*[X:n, x:s]", out type));
            Assert.True(type.IsTable);
            Assert.Equal(2, type.ChildCount);
        }

        [Fact]
        public void PrimitiveDTypes()
        {
            Assert.True(DType.Number.IsPrimitive);
            Assert.True(DType.Boolean.IsPrimitive);
            Assert.True(DType.String.IsPrimitive);
            Assert.True(DType.DateTime.IsPrimitive);
            Assert.True(DType.Date.IsPrimitive);
            Assert.True(DType.Time.IsPrimitive);
            Assert.True(DType.Hyperlink.IsPrimitive);
            Assert.True(DType.Currency.IsPrimitive);
            Assert.True(DType.Decimal.IsPrimitive);
            Assert.True(DType.Image.IsPrimitive);
            Assert.True(DType.PenImage.IsPrimitive);
            Assert.True(DType.Media.IsPrimitive);
            Assert.True(DType.Blob.IsPrimitive);
            Assert.True(DType.Color.IsPrimitive);
            Assert.True(DType.EmptyEnum.IsPrimitive);
            Assert.True(DType.ObjNull.IsPrimitive);
            Assert.True(DType.Guid.IsPrimitive);

            Assert.True(DType.TryParse("%n[A:1,B:2]", out DType type) && type.IsPrimitive);

            Assert.False(DType.Polymorphic.IsPrimitive);
            Assert.False(AttachmentTableType.IsPrimitive);
            Assert.False(AttachmentRecordType.IsPrimitive);

            Assert.False(DType.EmptyRecord.IsPrimitive);
            Assert.False(DType.EmptyTable.IsPrimitive);
        }

        [Fact]
        public void AttachmentdataDTypes()
        {
            // Attachment types are aggregate (implemented as lazy types)
            Assert.False(AttachmentTableType.IsPrimitive);
            Assert.True(AttachmentTableType.IsAggregate);

            Assert.True(AttachmentTableType.IsAttachment);
            Assert.NotNull(AttachmentTableType.AttachmentType);

            Assert.False(AttachmentRecordType.IsPrimitive);
            Assert.True(AttachmentRecordType.IsAggregate);

            Assert.True(AttachmentRecordType.IsAttachment);
            Assert.NotNull(AttachmentRecordType.AttachmentType);
        }

        [Fact]
        public void TryGetTypePathTest()
        {
            var type = TestUtils.DT("*[A:n, B:*[D:s, E:*[F:b]], C:n]");
            Assert.True(
                type.TryGetType(
                    DPath.Root
                        .Append(new DName("B"))
                        .Append(new DName("E"))
                        .Append(new DName("F")),
                    out var result));

            Assert.Equal(DType.Boolean, result);
        }

        [Fact]
        public void RecordAndTableDTypeTests()
        {
            var fError = false;
            DType typeDefault = DType.Invalid;

            Assert.True(!DType.Number.TryGetType(new DName("A"), out DType type) && type == typeDefault);

            Assert.True(!DType.Number.TryGetType(DPath.Root.Append(new DName("A")), out type) && type == typeDefault);
            fError = false;

            Assert.Equal(DType.EmptyRecord, DType.EmptyRecord.ToRecord());
            Assert.Equal(DType.EmptyRecord, DType.EmptyTable.ToRecord());
            Assert.Equal(DType.EmptyRecord, DType.ObjNull.ToRecord());

            DType.Number.ToRecord(ref fError);
            Assert.True(fError);
            fError = false;

            Assert.Equal(DType.EmptyTable, DType.EmptyTable.ToTable());
            Assert.Equal(DType.EmptyTable, DType.EmptyRecord.ToTable());
            Assert.Equal(DType.EmptyTable, DType.ObjNull.ToTable());

            DType.Number.ToTable(ref fError);
            Assert.True(fError);
            fError = false;

            var type1 = DType.CreateTable(
                new TypedName(DType.Number, new DName("A")),
                new TypedName(DType.Number, new DName("B")),
                new TypedName(DType.Number, new DName("C")));

            Assert.Equal("*[A:n, B:n, C:n]", type1.ToString());
            Assert.Equal("![A:n, B:n, C:n]", type1.ToRecord().ToString());
            Assert.Equal("![A:n, B:n, C:n]", type1.ToRecord().ToRecord().ToString());
            Assert.Equal("![A:n, B:n, C:n]", type1.ToRecord().ToString());
            Assert.True(type1 == type1.ToRecord().ToRecord().ToTable());
            Assert.True(type1.ToRecord() ==
                DType.CreateRecord(
                    new TypedName(DType.Number, new DName("A")),
                    new TypedName(DType.Number, new DName("B")),
                    new TypedName(DType.Number, new DName("C"))));

            Assert.True(type1.ChildCount == 3);
            Assert.True(type1.GetNames(DPath.Root).Count() == 3);
            Assert.True(type1.GetNames(DPath.Root.Append(new DName("A"))).Count() == 0);
            fError = false;
            Assert.True(type1.Kind == DKind.Table);
            Assert.True(type1.IsTable);
            Assert.False(type1.IsRecord);
            Assert.True(type1.Equals(type1));
            Assert.True(type1.GetType(DPath.Root) == type1);
            Assert.True(type1.GetType(new DName("A")) == DType.Number);
            Assert.True(!type1.TryGetType(new DName("D"), out type) && type == typeDefault);
            fError = false;
            Assert.True(type1.TryGetType(new DName("B"), out type) && type == DType.Number);
            Assert.True(type1.TryGetType(DPath.Root.Append(new DName("B")), out type) && type == DType.Number);
            Assert.True(!type1.TryGetType(new DName("D"), out type) && type == typeDefault);
            Assert.True(!type1.TryGetType(DPath.Root.Append(new DName("D")), out type) && type == typeDefault);

            DType type2 = null;
            foreach (var usePFxV1CompatRules in new[] { false, true })
            {
                type2 = DType.CreateTable(
                    new List<TypedName>()
                    {
                        new TypedName(DType.Number, new DName("B")),
                        new TypedName(DType.Number, new DName("A")),
                        new TypedName(DType.Number, new DName("C"))
                    });

                Assert.True(type1 == type2);
                Assert.False(type1 != type2);
                Assert.True(type1.Equals(type2));
                Assert.Equal(type1.ToString(), type2.ToString());
                Assert.True(type1.GetHashCode() == type2.GetHashCode());
                Assert.True(type1.Accepts(type2, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePFxV1CompatRules));
                Assert.True(type2.Accepts(type1, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePFxV1CompatRules));

                type2 = DType.CreateRecord(type1.GetNames(DPath.Root));
                Assert.False(type1 == type2);
                Assert.True(type1 != type2);
                Assert.False(type1.Equals(type2));
                Assert.Equal("![A:n, B:n, C:n]", type2.ToString());
                Assert.False(type1.Accepts(type2, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePFxV1CompatRules));
                Assert.False(type2.Accepts(type1, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePFxV1CompatRules));
            }

            type = type2;
            type2 = DType.CreateRecord(type1.GetNames(DPath.Root).ToArray());
            Assert.True(type2 == type);
            type2 = type2.Add(ref fError, DPath.Root, new DName("B"), DType.String);
            Assert.True(fError);
            Assert.Equal("![A:n, B:s, C:n]", type2.ToString());
            fError = false;

            type2 = DType.EmptyTable
                .Add(ref fError, DPath.Root, new DName("B"), DType.Number)
                .Add(ref fError, DPath.Root, new DName("C"), DType.Number)
                .Add(ref fError, DPath.Root, new DName("D"), DType.Boolean)
                .Add(ref fError, DPath.Root, new DName("A"), DType.Number);
            DType type3 = type1.Add(ref fError, DPath.Root, new DName("D"), DType.Boolean);
            Assert.False(fError);
            Assert.True(type2 == type3);
            Assert.True(type2.GetHashCode() == type3.GetHashCode());

            type2 = DType.CreateTable(type1.GetNames(DPath.Root));
            Assert.True(type1 == type2);
            Assert.True(type1.GetHashCode() == type2.GetHashCode());

            type3 = type2.SetType(ref fError, DPath.Root.Append(new DName("B")), DType.String);
            Assert.False(fError);
            Assert.Equal("*[A:n, B:s, C:n]", type3.ToString());
            Assert.False(type3 == type2);
            Assert.True(type1 == type2);

            type3 = type2.SetType(ref fError, DPath.Root.Append(new DName("B")), DType.Boolean);
            Assert.False(fError);
            Assert.Equal("*[A:n, B:b, C:n]", type3.ToString());
            Assert.False(type3 == type2);
            fError = false;

            type3 = type2.SetType(ref fError, DPath.Root.Append(new DName("B")).Append(new DName("X")), DType.Boolean);
            Assert.True(fError);
            Assert.Equal("*[A:n, B:n, C:n]", type3.ToString());
            Assert.True(type3 == type2);
            fError = false;

            var type4 = type1.Add(ref fError, DPath.Root.Append(new DName("X")), new DName("D"), DType.Number);
            Assert.True(fError);
            Assert.Equal("*[A:n, B:n, C:n]", type4.ToString());
            fError = false;

            var type5 = DType.EmptyTable.Add(ref fError, DPath.Root, new DName("D"), DType.String)
                .Add(ref fError, DPath.Root, new DName("E"), DType.String);
            Assert.False(fError);
            Assert.Equal("*[D:s, E:s]", type5.ToString());

            var type6 = DType.CreateTable(new TypedName(type5, new DName("Y")), new TypedName(DType.Boolean, new DName("Q")));
            Assert.False(fError);
            Assert.Equal("*[Q:b, Y:*[D:s, E:s]]", type6.ToString());
            Assert.True(type6.GetType(DPath.Root.Append(new DName("Q"))) == DType.Boolean);
            type = type6.Add(ref fError, DPath.Root.Append(new DName("Y")), new DName("E"), DType.Error);
            Assert.True(fError); // Y already has a child named E.
            Assert.Equal("*[Q:b, Y:*[D:s, E:e]]", type.ToString());
            fError = false;

            DType type7 = type6
                .Add(ref fError, DPath.Root, new DName("N"), DType.Number)
                .Add(ref fError, DPath.Root, new DName("E"), DType.Error);
            type7 = type7.Add(ref fError, DPath.Root.Append(new DName("Y")), new DName("F"), DType.Boolean)
                .Add(ref fError, DPath.Root.Append(new DName("Y")), new DName("G"), DType.Error);
            Assert.False(fError);
            Assert.Equal("*[E:e, N:n, Q:b, Y:*[D:s, E:s, F:b, G:e]]", type7.ToString());

            foreach (var usePFxV1CompatRules in new[] { false, true })
            {
                Assert.False(type7.Accepts(type6, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePFxV1CompatRules));
                Assert.True(type6.Accepts(type7, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePFxV1CompatRules));
            }

            type7 = type7.DropMulti(ref fError, DPath.Root, new DName("N"), new DName("E"));
            Assert.False(fError);
            type7 = type7
                .Drop(ref fError, DPath.Root.Append(new DName("Y")), new DName("F"))
                .Drop(ref fError, DPath.Root.Append(new DName("Y")), new DName("G"));
            Assert.False(fError);
            Assert.True(type7 == type6);
            type7 = type7.Drop(ref fError, DPath.Root.Append(new DName("Q")), new DName("F"));
            Assert.True(fError);
            fError = false;

            DType type12 = type1.Add(
                ref fError,
                DPath.Root,
                new DName("Y"),
                DType.CreateTable(new TypedName(DType.Boolean, new DName("F")), new TypedName(DType.Error, new DName("E"))));
            Assert.False(fError);

            DType type8 = type1.Add(ref fError, DPath.Root, new DName("X"), type6);
            Assert.False(fError);
            Assert.Equal("*[A:n, B:n, C:n, X:*[Q:b, Y:*[D:s, E:s]]]", type8.ToString());

            foreach (var usePFxV1CompatRules in new[] { false, true })
            {
                Assert.False(type8.Accepts(type1, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePFxV1CompatRules));
                Assert.True(type1.Accepts(type8, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePFxV1CompatRules));

                Assert.False(DType.String.Accepts(DType.Number, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePFxV1CompatRules));

                // Accepts
                Assert.True(
                    !DType.EmptyRecord.Add(ref fError, DPath.Root, new DName("A"), DType.Number)
                        .Accepts(DType.EmptyRecord.Add(ref fError, DPath.Root, new DName("A"), DType.Error), exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePFxV1CompatRules) &&
                    !fError);
                Assert.True(
                    DType.EmptyRecord.Add(ref fError, DPath.Root, new DName("A"), DType.Number)
                        .Accepts(DType.EmptyRecord.Add(ref fError, DPath.Root, new DName("A"), DType.ObjNull), exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePFxV1CompatRules) &&
                    !fError);
            }

            // Testing duplicate names in the construction. Last one should win.
            var type11 = DType.CreateTable(
                new TypedName(DType.Error, new DName("A")),
                new TypedName(DType.Number, new DName("A")));
            Assert.True(type11.GetType(DPath.Root.Append(new DName("A"))) == DType.Number);
            type11 = DType.CreateTable(
                new TypedName(DType.Error, new DName("A")),
                new TypedName(DType.String, new DName("B")),
                new TypedName(DType.Number, new DName("A")));
            Assert.True(type11.GetType(DPath.Root.Append(new DName("A"))) == DType.Number);
            type11 = DType.CreateTable(
                new TypedName(DType.Number, new DName("A")),
                new TypedName(DType.String, new DName("B")),
                new TypedName(DType.Error, new DName("A")));
            Assert.True(type11.GetType(DPath.Root.Append(new DName("A"))) == DType.Error);
        }

        [Fact]
        public void EnumDTypeTests()
        {
            foreach (var usePowerFxV1CompatibilityRules in new[] { false, true })
            {
                Assert.True(DType.TryParse("%n[A:0, B:1, C:2, D:3]", out DType type) && type.IsEnum);
                Assert.True(DType.TryParse("%n[A:0, B:1, C:2, D:3]", out DType type2) && type2.IsEnum);

                Assert.True(type == type2);
                Assert.True(type.Accepts(type2, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.True(type2.Accepts(type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(type.Accepts(DType.Number, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.True(DType.Number.Accepts(type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

                Assert.True(DType.TryParse("%n[A:0]", out type2) && type2.IsEnum);
                Assert.False(type == type2);
                Assert.True(type.Accepts(type2, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules)); // The enum type with more values accepts an enum value from the type with less values.
                Assert.False(type2.Accepts(type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules)); // The enum type with less values does not accept values from the larger enum.
                Assert.False(type2.Accepts(DType.Number, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.True(DType.Number.Accepts(type2, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

                Assert.True(DType.TryParse("%s[A:\"letter\"]", out type2) && type2.IsEnum);
                Assert.False(type == type2);
                Assert.False(type.Accepts(type2, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(type2.Accepts(type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(type2.Accepts(DType.String, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.True(DType.String.Accepts(type2, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

                Assert.True(DType.TryParse("%b[A:true, B:false]", out type2) && type2.IsEnum);
                Assert.False(type == type2);
                Assert.False(type.Accepts(type2, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(type2.Accepts(type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(type2.Accepts(DType.Boolean, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.True(DType.Boolean.Accepts(type2, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

                Assert.True(DType.TryParse("%n[A:12345, B:1, C:2, D:3]", out type2) && type2.IsEnum);
                Assert.False(type == type2);
                Assert.False(type.Accepts(type2, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(type2.Accepts(type, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.False(type2.Accepts(DType.Number, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.True(DType.Number.Accepts(type2, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

                Assert.True(DType.TryParse("%s['Segoe UI':\"segoe ui\", 'bah humbug':\"bah and then humbug\"]", out type2) && type2.IsEnum);
                Assert.True(DType.String.Accepts(type2, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
                Assert.Equal("%s['Segoe UI':\"segoe ui\", 'bah humbug':\"bah and then humbug\"]", type2.ToString());
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestDefaultSchemaDifference(bool usePowerFxV1CompatibilityRules)
        {
            var left = DType.CreateEnum(DType.ObjNull, Enumerable.Empty<KeyValuePair<DName, object>>());
            var right = DType.CreateEnum(DType.Number, Enumerable.Empty<KeyValuePair<DName, object>>());

            // Test a failing path
            Assert.False(
                left.Accepts(
                    right, 
                    out KeyValuePair<string, DType> testSchemaDifference, 
                    out DType typeSchemaDifferenceType,
                    exact: true,
                    useLegacyDateTimeAccepts: false,
                    usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.Equal(testSchemaDifference.Value, DType.Invalid);

            // Test the TreeAccepts path
            left = DType.CreateRecord(Enumerable.Empty<TypedName>());
            Assert.True(
                left.Accepts(
                    left, 
                    out testSchemaDifference, 
                    out typeSchemaDifferenceType,
                    exact: true,
                    useLegacyDateTimeAccepts: false,
                    usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.Equal(testSchemaDifference.Value, DType.Invalid);

            // Test the most immediate path
            right = DType.ObjNull;
            Assert.True(
                left.Accepts(
                    right, 
                    out testSchemaDifference,
                    out typeSchemaDifferenceType,
                    exact: true,
                    useLegacyDateTimeAccepts: false,
                    usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.Equal(testSchemaDifference.Value, DType.Invalid);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestDTypeSupertype(bool usePowerFxV1CompatibilityRules)
        {
            // *[A:n,B:s,C:b,D:n]
            var type1 = DType.CreateTable(
                new TypedName(DType.Number, new DName("A")),
                new TypedName(DType.String, new DName("B")),
                new TypedName(DType.Boolean, new DName("C")),
                new TypedName(DType.Number, new DName("D")));

            // *[A:n,B:s,C:n,D:n]
            var type2 = DType.CreateTable(
                new TypedName(DType.Number, new DName("A")),
                new TypedName(DType.String, new DName("B")),
                new TypedName(DType.Number, new DName("C")),
                new TypedName(DType.Number, new DName("D")));

            // Table with null value
            // *[A:n,B:s,C:N,D:n]
            var type2s = DType.CreateTable(
                new TypedName(DType.Number, new DName("A")),
                new TypedName(DType.String, new DName("B")),
                new TypedName(DType.ObjNull, new DName("C")),
                new TypedName(DType.Number, new DName("D")));

            // *[A:n,B:s,C:*[D:n,F:d]]
            var type3 = DType.CreateTable(
                new TypedName(DType.Number, new DName("A")),
                new TypedName(DType.String, new DName("B")),
                new TypedName(
                    DType.CreateTable(
                    new TypedName(DType.Number, new DName("D")),
                    new TypedName(DType.DateTime, new DName("F"))), new DName("C")));

            // *[A:n,B:s,C:*[D:n,F:s]]
            var type4 = DType.CreateTable(
                new TypedName(DType.Number, new DName("A")),
                new TypedName(DType.String, new DName("B")),
                new TypedName(
                    DType.CreateTable(
                    new TypedName(DType.Number, new DName("D")),
                    new TypedName(DType.String, new DName("F"))), new DName("C")));

            // Table with null value
            // *[A:n,B:s,C:*[D:n,F:N]]
            var type4s = DType.CreateTable(
                new TypedName(DType.Number, new DName("A")),
                new TypedName(DType.String, new DName("B")),
                new TypedName(
                    DType.CreateTable(
                    new TypedName(DType.Number, new DName("D")),
                    new TypedName(DType.ObjNull, new DName("F"))), new DName("C")));

            // Output should be *[A:n,B:s,C:*[D:n]]
            var superType = DType.Supertype(type3, type4, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("*[A:n, B:s, C:*[D:n]]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);
            superType = DType.Supertype(type4, type3, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("*[A:n, B:s, C:*[D:n]]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);

            // Output should be *[A:n,B:s,D:n]
            superType = DType.Supertype(type1, type2, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("*[A:n, B:s, D:n]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);
            superType = DType.Supertype(type2, type1, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("*[A:n, B:s, D:n]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);

            // Table with null value
            // Output should be *[A:n,B:s,C:b,D:n]
            superType = DType.Supertype(type1, type2s, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("*[A:n, B:s, C:b, D:n]", superType.ToString());
            Assert.Equal(4, superType.ChildCount);
            superType = DType.Supertype(type2s, type1, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("*[A:n, B:s, C:b, D:n]", superType.ToString());
            Assert.Equal(4, superType.ChildCount);

            // Table with null value
            // Output should be *[A:n,B:s,C:*[D:n,F:d]]
            superType = DType.Supertype(type3, type4s, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("*[A:n, B:s, C:*[D:n, F:d]]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);
            superType = DType.Supertype(type4s, type3, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("*[A:n, B:s, C:*[D:n, F:d]]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);

            // ![A:n,B:s,C:b,D:n]
            var type5 = DType.CreateRecord(
                new TypedName(DType.Number, new DName("A")),
                new TypedName(DType.String, new DName("B")),
                new TypedName(DType.Boolean, new DName("C")),
                new TypedName(DType.Number, new DName("D")));

            // ![A:n,B:s,C:n,D:n]
            var type6 = DType.CreateRecord(
                new TypedName(DType.Number, new DName("A")),
                new TypedName(DType.String, new DName("B")),
                new TypedName(DType.Number, new DName("C")),
                new TypedName(DType.Number, new DName("D")));

            // Record with null value
            // ![A:n,B:s,C:N,D:n]
            var type6s = DType.CreateRecord(
                new TypedName(DType.Number, new DName("A")),
                new TypedName(DType.String, new DName("B")),
                new TypedName(DType.ObjNull, new DName("C")),
                new TypedName(DType.Number, new DName("D")));

            // ![A:n,B:s,C:![D:n,F:d]]
            var type7 = DType.CreateRecord(
                new TypedName(DType.Number, new DName("A")),
                new TypedName(DType.String, new DName("B")),
                new TypedName(
                    DType.CreateRecord(
                    new TypedName(DType.Number, new DName("D")),
                    new TypedName(DType.DateTime, new DName("F"))), new DName("C")));

            // ![A:n,B:s,C:![D:n,F:s]]
            var type8 = DType.CreateRecord(
                new TypedName(DType.Number, new DName("A")),
                new TypedName(DType.String, new DName("B")),
                new TypedName(
                    DType.CreateRecord(
                    new TypedName(DType.Number, new DName("D")),
                    new TypedName(DType.String, new DName("F"))), new DName("C")));

            // Record with null value
            // ![A:n,B:s,C:![D:n,F:N]]
            var type8s = DType.CreateRecord(
                new TypedName(DType.Number, new DName("A")),
                new TypedName(DType.String, new DName("B")),
                new TypedName(
                    DType.CreateRecord(
                    new TypedName(DType.Number, new DName("D")),
                    new TypedName(DType.ObjNull, new DName("F"))), new DName("C")));

            superType = DType.Supertype(type5, type6, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("![A:n, B:s, D:n]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);
            superType = DType.Supertype(type6, type5, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("![A:n, B:s, D:n]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);

            // Record with null value
            superType = DType.Supertype(type5, type6s, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("![A:n, B:s, C:b, D:n]", superType.ToString());
            Assert.Equal(4, superType.ChildCount);
            superType = DType.Supertype(type6s, type5, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("![A:n, B:s, C:b, D:n]", superType.ToString());
            Assert.Equal(4, superType.ChildCount);

            superType = DType.Supertype(type7, type8, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("![A:n, B:s, C:![D:n]]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);
            superType = DType.Supertype(type8, type7, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("![A:n, B:s, C:![D:n]]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);

            // Record with null value
            superType = DType.Supertype(type7, type8s, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("![A:n, B:s, C:![D:n, F:d]]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);
            superType = DType.Supertype(type8s, type7, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("![A:n, B:s, C:![D:n, F:d]]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);

            superType = DType.Supertype(DType.Number, DType.Number, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(0, superType.ChildCount);

            superType = DType.Supertype(DType.Number, DType.String, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.Error, superType.Kind);

            superType = DType.Supertype(DType.Unknown, DType.String, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.String, superType.Kind);

            superType = DType.Supertype(DType.String, DType.Unknown, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.String, superType.Kind);

            superType = DType.Supertype(DType.Unknown, DType.Unknown, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.Unknown, superType.Kind);

            superType = DType.Supertype(DType.Date, DType.Time, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(usePowerFxV1CompatibilityRules ? DKind.Error : DKind.DateTime, superType.Kind);

            superType = DType.Supertype(DType.DateTime, DType.Time, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(usePowerFxV1CompatibilityRules ? DKind.Error : DKind.DateTime, superType.Kind);

            superType = DType.Supertype(DType.DateTime, DType.Date, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(usePowerFxV1CompatibilityRules ? DKind.Error : DKind.DateTime, superType.Kind);

            superType = DType.Supertype(DType.Time, DType.DateTime, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(usePowerFxV1CompatibilityRules ? DKind.Error : DKind.DateTime, superType.Kind);

            superType = DType.Supertype(DType.Date, DType.DateTime, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(usePowerFxV1CompatibilityRules ? DKind.Error : DKind.DateTime, superType.Kind);

            superType = DType.Supertype(DType.Image, DType.PenImage, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(usePowerFxV1CompatibilityRules ? DKind.Error : DKind.Image, superType.Kind);

            superType = DType.Supertype(DType.PenImage, DType.Image, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(usePowerFxV1CompatibilityRules ? DKind.Error : DKind.Image, superType.Kind);

            superType = DType.Supertype(DType.Image, DType.Media, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(usePowerFxV1CompatibilityRules ? DKind.Error : DKind.Hyperlink, superType.Kind);

            superType = DType.Supertype(DType.Media, DType.Image, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(usePowerFxV1CompatibilityRules ? DKind.Error : DKind.Hyperlink, superType.Kind);

            superType = DType.Supertype(DType.Image, DType.Blob, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(usePowerFxV1CompatibilityRules ? DKind.Error : DKind.Image, superType.Kind);

            superType = DType.Supertype(DType.Blob, DType.Image, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(usePowerFxV1CompatibilityRules ? DKind.Error : DKind.Image, superType.Kind);

            superType = DType.Supertype(DType.Blob, DType.Media, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(usePowerFxV1CompatibilityRules ? DKind.Error : DKind.Media, superType.Kind);

            superType = DType.Supertype(DType.Media, DType.Blob, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(usePowerFxV1CompatibilityRules ? DKind.Error : DKind.Media, superType.Kind);

            superType = DType.Supertype(DType.Image, DType.Hyperlink, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(usePowerFxV1CompatibilityRules ? DKind.Error : DKind.Hyperlink, superType.Kind);

            superType = DType.Supertype(DType.Media, DType.Hyperlink, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(usePowerFxV1CompatibilityRules ? DKind.Error : DKind.Hyperlink, superType.Kind);

            superType = DType.Supertype(DType.Blob, DType.Hyperlink, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(usePowerFxV1CompatibilityRules ? DKind.Error : DKind.Hyperlink, superType.Kind);

            superType = DType.Supertype(DType.Hyperlink, DType.Image, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(usePowerFxV1CompatibilityRules ? DKind.Error : DKind.Hyperlink, superType.Kind);

            superType = DType.Supertype(DType.Hyperlink, DType.Media, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(usePowerFxV1CompatibilityRules ? DKind.Error : DKind.Hyperlink, superType.Kind);

            superType = DType.Supertype(DType.Hyperlink, DType.Blob, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(usePowerFxV1CompatibilityRules ? DKind.Error : DKind.Hyperlink, superType.Kind);

            superType = DType.Supertype(DType.Currency, DType.DateTime, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.Error, superType.Kind);

            superType = DType.Supertype(DType.Currency, DType.Date, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.Error, superType.Kind);

            superType = DType.Supertype(DType.Currency, DType.Time, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.Error, superType.Kind);

            superType = DType.Supertype(DType.DateTime, DType.Currency, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.Error, superType.Kind);

            superType = DType.Supertype(DType.Date, DType.Currency, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.Error, superType.Kind);

            superType = DType.Supertype(DType.Time, DType.Currency, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.Error, superType.Kind);

            superType = DType.Supertype(DType.Decimal, DType.DateTime, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.Error, superType.Kind);

            superType = DType.Supertype(DType.Decimal, DType.Time, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.Error, superType.Kind);

            superType = DType.Supertype(DType.Decimal, DType.Date, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.Error, superType.Kind);

            superType = DType.Supertype(DType.DateTime, DType.Decimal, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.Error, superType.Kind);

            superType = DType.Supertype(DType.Time, DType.Decimal, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.Error, superType.Kind);

            superType = DType.Supertype(DType.Date, DType.Decimal, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.Error, superType.Kind);

            superType = DType.Supertype(DType.Guid, DType.String, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(usePowerFxV1CompatibilityRules ? DKind.Error : DKind.String, superType.Kind);

            superType = DType.Supertype(DType.Guid, DType.Number, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.Error, superType.Kind);

            // ObjNull is compatable with every DType except for Error
            superType = DType.Supertype(DType.Number, DType.ObjNull, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.Number, superType.Kind);

            superType = DType.Supertype(DType.String, DType.ObjNull, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.String, superType.Kind);

            superType = DType.Supertype(DType.Date, DType.ObjNull, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.Date, superType.Kind);

            superType = DType.Supertype(DType.Time, DType.ObjNull, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.Time, superType.Kind);

            superType = DType.Supertype(DType.DateTime, DType.ObjNull, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.DateTime, superType.Kind);

            superType = DType.Supertype(DType.Image, DType.ObjNull, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.Image, superType.Kind);

            superType = DType.Supertype(DType.PenImage, DType.ObjNull, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.PenImage, superType.Kind);

            superType = DType.Supertype(DType.Media, DType.ObjNull, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.Media, superType.Kind);

            superType = DType.Supertype(DType.Blob, DType.ObjNull, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.Blob, superType.Kind);

            superType = DType.Supertype(DType.Hyperlink, DType.ObjNull, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.Hyperlink, superType.Kind);

            superType = DType.Supertype(DType.Currency, DType.ObjNull, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.Currency, superType.Kind);

            superType = DType.Supertype(DType.Decimal, DType.ObjNull, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.Decimal, superType.Kind);

            superType = DType.Supertype(DType.Unknown, DType.ObjNull, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.Unknown, superType.Kind);

            // ![A:t, B:s]
            var type9 = DType.CreateRecord(
                new TypedName(DType.Time, new DName("A")),
                new TypedName(DType.String, new DName("B")));

            // ![A:d, B:b]
            var type10 = DType.CreateRecord(
                new TypedName(DType.DateTime, new DName("A")),
                new TypedName(DType.Boolean, new DName("B")));

            superType = DType.Supertype(type9, type10, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.Record, superType.Kind);
            Assert.Equal(usePowerFxV1CompatibilityRules ? 0 : 1, superType.ChildCount); // ![A:n]

            // ![A:n,B:s,C:![D:n,F:d]]
            var type11 = DType.CreateRecord(
                new TypedName(DType.Number, new DName("A")),
                new TypedName(DType.String, new DName("B")),
                new TypedName(
                    DType.CreateRecord(
                    new TypedName(DType.Number, new DName("D")),
                    new TypedName(DType.DateTime, new DName("F"))), new DName("C")));

            // ![A:n,B:s,C:![D:n,F:s]]
            var type12 = DType.CreateRecord(
                new TypedName(DType.Number, new DName("A")),
                new TypedName(DType.String, new DName("B")),
                new TypedName(
                    DType.CreateRecord(
                    new TypedName(DType.Boolean, new DName("D")),
                    new TypedName(DType.String, new DName("F"))), new DName("C")));

            superType = DType.Supertype(type11, type12, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("![A:n, B:s, C:![]]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);
            superType = DType.Supertype(type12, type11, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("![A:n, B:s, C:![]]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);

            // ![A:n,B:s,C:![D:n,F:o]]
            var type13 = DType.CreateRecord(
                new TypedName(DType.Number, new DName("A")),
                new TypedName(DType.String, new DName("B")),
                new TypedName(
                    DType.CreateRecord(
                    new TypedName(DType.Number, new DName("D")),
                    new TypedName(DType.Unknown, new DName("F"))), new DName("C")));

            // ![A:n,B:s,C:![D:n,F:s]]
            var type14 = DType.CreateRecord(
                new TypedName(DType.Number, new DName("A")),
                new TypedName(DType.String, new DName("B")),
                new TypedName(
                    DType.CreateRecord(
                    new TypedName(DType.Boolean, new DName("D")),
                    new TypedName(DType.String, new DName("F"))), new DName("C")));

            superType = DType.Supertype(type13, type14, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("![A:n, B:s, C:![F:s]]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);
            superType = DType.Supertype(type14, type13, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("![A:n, B:s, C:![F:s]]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);

            // ![A:n,B:s,C:![D:n,F:o]]
            var type15 = DType.CreateRecord(
                new TypedName(DType.Number, new DName("D")),
                new TypedName(DType.String, new DName("E")),
                new TypedName(
                    DType.CreateRecord(
                    new TypedName(DType.Number, new DName("G")),
                    new TypedName(DType.Unknown, new DName("H"))), new DName("F")));

            // ![A:n,B:s,C:![D:n,F:s]]
            var type16 = DType.CreateRecord(new TypedName(DType.String, new DName("E")));

            superType = DType.Supertype(type15, type16, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("![E:s]", superType.ToString());
            superType = DType.Supertype(type16, type15, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("![E:s]", superType.ToString());

            // supertype of a record with a table:
            superType = DType.Supertype(type4, type14, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal(DKind.Error, superType.Kind);

            foreach (var dType in _dTypes)
            {
                // Deferred is subtype of all except unknown and void.
                if (dType != DType.Unknown && dType != DType.Void)
                {
                    superType = DType.Supertype(dType, DType.Deferred, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
                    Assert.Equal(dType.Kind, superType.Kind);
                }
            }
        }

        [Fact]
        public void DTypeSpecParsing_SimpleTypes()
        {
            Assert.True(DType.TryParse(DType.Unknown.ToString(), out DType type) && type == DType.Unknown);
            Assert.True(DType.TryParse(DType.Deferred.ToString(), out type) && type == DType.Deferred);
            Assert.True(DType.TryParse(DType.Void.ToString(), out type) && type == DType.Void);
            Assert.True(DType.TryParse(DType.Error.ToString(), out type) && type == DType.Error);
            Assert.True(DType.TryParse(DType.Number.ToString(), out type) && type == DType.Number);
            Assert.True(DType.TryParse(DType.Boolean.ToString(), out type) && type == DType.Boolean);
            Assert.True(DType.TryParse(DType.String.ToString(), out type) && type == DType.String);
            Assert.True(DType.TryParse(DType.DateTime.ToString(), out type) && type == DType.DateTime);
            Assert.True(DType.TryParse(DType.Date.ToString(), out type) && type == DType.Date);
            Assert.True(DType.TryParse(DType.Time.ToString(), out type) && type == DType.Time);
            Assert.True(DType.TryParse(DType.Hyperlink.ToString(), out type) && type == DType.Hyperlink);
            Assert.True(DType.TryParse(DType.Image.ToString(), out type) && type == DType.Image);
            Assert.True(DType.TryParse(DType.PenImage.ToString(), out type) && type == DType.PenImage);
            Assert.True(DType.TryParse(DType.Media.ToString(), out type) && type == DType.Media);
            Assert.True(DType.TryParse(DType.Blob.ToString(), out type) && type == DType.Blob);
            Assert.True(DType.TryParse(DType.Currency.ToString(), out type) && type == DType.Currency);
            Assert.True(DType.TryParse(DType.Decimal.ToString(), out type) && type == DType.Decimal);
            Assert.True(DType.TryParse(DType.Color.ToString(), out type) && type == DType.Color);
            Assert.True(DType.TryParse(DType.EmptyRecord.ToString(), out type) && type == DType.EmptyRecord);
            Assert.True(DType.TryParse(DType.EmptyTable.ToString(), out type) && type == DType.EmptyTable);
            Assert.True(DType.TryParse(DType.EmptyEnum.ToString(), out type) && type == DType.EmptyEnum);
        }

        [Fact]
        public void DTypeSpecParsing_NestedAggregates()
        {
            // ![A:n,B:s,C:![D:n,E:?]]
            var type2 = DType.CreateRecord(
                new TypedName(DType.Number, new DName("A")),
                new TypedName(DType.String, new DName("B")),
                new TypedName(
                    DType.CreateRecord(
                    new TypedName(DType.Number, new DName("D")),
                    new TypedName(DType.Unknown, new DName("E"))), new DName("C")));
            Assert.True(DType.TryParse("![A:n,B:s,C:![D:n,E:?]]", out DType type) && type == type2);
            Assert.True(DType.TryParse("*[A:n,B:s,C:![D:n,E:?]]", out type) && type == type2.ToTable());

            // *[A:n,B:s,C:*[D:n,E:?]]
            type2 = DType.CreateTable(
                new TypedName(DType.Number, new DName("A")),
                new TypedName(DType.String, new DName("B")),
                new TypedName(
                    DType.CreateTable(
                    new TypedName(DType.Number, new DName("D")),
                    new TypedName(DType.Unknown, new DName("E"))), new DName("C")));
            Assert.True(DType.TryParse("*[A:n,B:s,C:*[D:n,E:?]]", out type) && type == type2);
            Assert.True(DType.TryParse("![A:n,B:s,C:*[D:n,E:?]]", out type) && type == type2.ToRecord());

            // ![A:![B:![C:![]]]]
            type2 = DType.CreateRecord(
                new TypedName(
                    DType.CreateRecord(
                    new TypedName(
                        DType.CreateRecord(
                        new TypedName(
                            DType.CreateRecord(),
                            new DName("C"))), new DName("B"))), new DName("A")));
            Assert.True(DType.TryParse("![A:![B:![C:![]]]]", out type) && type == type2);
            Assert.True(DType.TryParse("*[A:![B:![C:![]]]]", out type) && type == type2.ToTable());

            // *[A:*[B:*[C:*[]]]]
            type2 = DType.CreateTable(
                new TypedName(
                    DType.CreateTable(
                    new TypedName(
                        DType.CreateTable(
                        new TypedName(
                            DType.CreateTable(),
                            new DName("C"))), new DName("B"))), new DName("A")));
            Assert.True(DType.TryParse("*[A:*[B:*[C:*[]]]]", out type) && type == type2);
            Assert.True(DType.TryParse("![A:*[B:*[C:*[]]]]", out type) && type == type2.ToRecord());

            // *[Num:n, Bool:b, Str:s, Date:d, Hyper:h, Img:i, Currency:$, Decimal:w, Color:c, Unknown:?, Err:e, ONull:N]
            type2 = DType.CreateTable(
                new TypedName(DType.Number, new DName("Num")),
                new TypedName(DType.Boolean, new DName("Bool")),
                new TypedName(DType.String, new DName("Str")),
                new TypedName(DType.DateTime, new DName("Date")),
                new TypedName(DType.Hyperlink, new DName("Hyper")),
                new TypedName(DType.Image, new DName("Img")),
                new TypedName(DType.Currency, new DName("Currency")),
                new TypedName(DType.Decimal, new DName("Decimal")),
                new TypedName(DType.Color, new DName("Color")),
                new TypedName(DType.Unknown, new DName("Unknown")),
                new TypedName(DType.Error, new DName("Err")),
                new TypedName(DType.Deferred, new DName("Deferred")),
                new TypedName(DType.ObjNull, new DName("ONull")));
            Assert.True(DType.TryParse("*[Num:n, Bool:b, Str:s, Date:d, Hyper:h, Img:i, Currency:$, Decimal:w, Color:c, Unknown:?, Err:e, Deferred:X, ONull:N]", out type) && type == type2);

            // ![A:n,B:s,C:![D:n,E:%s[R:"red",G:"green",B:"blue"]]]
            type2 = DType.CreateRecord(
                new TypedName(DType.Number, new DName("A")),
                new TypedName(DType.String, new DName("B")),
                new TypedName(
                    DType.CreateRecord(
                    new TypedName(DType.Number, new DName("D")),
                    new TypedName(
                        DType.CreateEnum(
                        DType.String,
                        new KeyValuePair<DName, object>(new DName("R"), "red"),
                        new KeyValuePair<DName, object>(new DName("G"), "green"),
                        new KeyValuePair<DName, object>(new DName("B"), "blue")),
                        new DName("E"))), new DName("C")));
            Assert.True(DType.TryParse("![A:n,B:s,C:![D:n,E:%s[R:\"red\",G:\"green\",B:\"blue\"]]]", out type) && type == type2);
            Assert.True(DType.TryParse("*[A:n,B:s,C:![D:n,E:%s[R:\"red\",G:\"green\",B:\"blue\"]]]", out type) && type == type2.ToTable());
        }

        [Fact]
        public void DTypeSpecParsing_Enums()
        {
            var type2 = DType.CreateEnum(
                DType.Number,
                new KeyValuePair<DName, object>(new DName("Red"), 1),
                new KeyValuePair<DName, object>(new DName("Green"), 2),
                new KeyValuePair<DName, object>(new DName("Blue"), 3));
            Assert.True(DType.TryParse("%n[Red:1, Green:2, Blue:3]", out DType type) && type == type2);

            type2 = DType.CreateEnum(
                DType.String,
                new KeyValuePair<DName, object>(new DName("Red"), "red"),
                new KeyValuePair<DName, object>(new DName("Green"), "green"),
                new KeyValuePair<DName, object>(new DName("Blue"), "blue"));
            Assert.True(DType.TryParse("%s[Red:\"red\", Green:\"green\", Blue:\"blue\"]", out type) && type == type2);

            type2 = DType.CreateEnum(
                DType.String,
                new KeyValuePair<DName, object>(new DName("Red"), "light red"),
                new KeyValuePair<DName, object>(new DName("Green"), "lime green"),
                new KeyValuePair<DName, object>(new DName("Blue"), "blue but not cyan"));
            Assert.True(DType.TryParse("%s[Red:\"light red\", Green:\"lime green\", Blue:\"blue but not cyan\"]", out type) && type == type2);

            type2 = DType.CreateEnum(
                DType.Boolean,
                new KeyValuePair<DName, object>(new DName("Yes"), true),
                new KeyValuePair<DName, object>(new DName("Oui"), true),
                new KeyValuePair<DName, object>(new DName("Da"), true),
                new KeyValuePair<DName, object>(new DName("Ja"), true),
                new KeyValuePair<DName, object>(new DName("No"), false),
                new KeyValuePair<DName, object>(new DName("Non"), false),
                new KeyValuePair<DName, object>(new DName("Nu"), false),
                new KeyValuePair<DName, object>(new DName("Nein"), false));
            Assert.True(DType.TryParse("%b[Yes:true, No:false, Oui:true, Non:false, Ja:true, Nein:false, Da:true, Nu:false]", out type) && type == type2);

            type2 = DType.CreateEnum(DType.ObjNull);
            Assert.True(DType.TryParse("%N[]", out type) && type == type2);
        }

        [Fact]
        public void DTypeTestEnumsParseAndPrettyprint()
        {
            var type2 = DType.CreateEnum(
                DType.Number,
                new KeyValuePair<DName, object>(new DName("Red"), -123),
                new KeyValuePair<DName, object>(new DName("Green"), -234),
                new KeyValuePair<DName, object>(new DName("Blue"), -345));
            Assert.True(DType.TryParse("%n[Red:-123, Green:-234, Blue:-345]", out DType type) && type == type2);

            Assert.Equal("%n[Blue:-345, Green:-234, Red:-123]", type2.ToString());

            // Verify that AppendTo and ToString() produce the same result
            var sb = new StringBuilder();
            type2.AppendTo(sb);
            Assert.Equal(sb.ToString(), type2.ToString());

            DType type3 = TestUtils.DT("*[Color:%n[Blue:123, Green:-234, Red:-345]]");
            Assert.Equal("*[Color:%n[Blue:123, Green:-234, Red:-345]]", type3.ToString());

            // Verify that AppendTo and ToString() produce the same result
            sb = new StringBuilder();
            type3.AppendTo(sb);
            Assert.Equal(sb.ToString(), type3.ToString());

            // Test roundtripping
            Assert.True(DType.TryParse(type3.ToString(), out DType type4) && type3 == type4);
        }

        [Fact]
        public void DTypeSpecParsing_FieldsWithBlanks()
        {
            var result = DType.TryParse("*['foo bar':s]", out DType type);
            Assert.True(result);
            Assert.True(type.IsTable);
            Assert.True(type.GetNames(DPath.Root).Count() == 1);
            Assert.True(type.GetNames(DPath.Root).First().Name.Value == "foo bar");

            Assert.True(DType.TryParse("*['foo bar':s, 'hello world from AppMagic':n, something:!['App Magic':b, TheMagic:c]]", out type) && type.IsTable);
            Assert.True(DType.TryParse("![Hello:s, 'hello world':n, something:!['App Magic':b, TheMagic:c]]", out type) && type.IsRecord);
            Assert.True(DType.TryParse("*['some strange \"identifiers\"':s, 'with \"nested double\" quotes':s]", out type) && type.IsTable);
            Assert.True(DType.TryParse("*['\"more strange identifiers\"':s, 'with \"nested double quotes\"':s]", out type) && type.IsTable);

            // Enums should support these too...
            Assert.True(DType.TryParse("%n['foo bar':1, 'hello world from beyond':2]", out type) && type.IsEnum);
            Assert.True(DType.TryParse("%s['foo bar':\"foo bar car\", 'hello world from beyond':\"and then goodbye\"]", out type) && type.IsEnum);

            // Round-tripping...
            Assert.True(DType.TryParse("*['foo bar':s, 'hello world':n, from:n, 'beyond':b]", out type) && type.IsTable);
            Assert.Equal("*[beyond:b, 'foo bar':s, from:n, 'hello world':n]", type.ToString());
        }

        [Fact]
        public void DTypeSpecParsing_FieldsWithSpecialChars()
        {
            var result = DType.TryParse(
                "*[" +
                "'''':s," +
                "'single''apostrophe':s," +
                "'single\"quotes':n," +
                "\"single\"\"quotes_offsetbyquote\":n," +
                "'\"bothquotes\"':s," +
                "'\\backslash':s," +
                "'.period':s," +
                "'!bang':n," +
                "'*asterisk':s," +
                "'space exists':s," +
                "'$currency':s," +
                "'OpenParen(':s," +
                "'single''''quotes':s," +
                "'!@#$%^&*()_+-=:;''''\"{}\\|<>?/.,~`':s]", out DType type);
            Assert.True(result);
            Assert.True(type.IsTable);

            var names = type.GetNames(DPath.Root);
            Assert.True(names.Count() == 14);
            Assert.Contains(names, tn => tn.Name == new DName("'"));
            Assert.Contains(names, tn => tn.Name == new DName("single'apostrophe"));
            Assert.Contains(names, tn => tn.Name == new DName("single\"quotes"));
            Assert.Contains(names, tn => tn.Name == new DName("\"single\"\"quotes_offsetbyquote\""));
            Assert.Contains(names, tn => tn.Name == new DName("\"bothquotes\""));
            Assert.Contains(names, tn => tn.Name == new DName("single''quotes"));
            Assert.Contains(names, tn => tn.Name == new DName("!@#$%^&*()_+-=:;''\"{}\\|<>?/.,~`"));
        }

        [Fact]
        public void DTypeSpecParsing_Negative()
        {
            Assert.False(DType.TryParse("TypeTypeType", out DType type));
            Assert.False(DType.TryParse("nnn", out type));
            Assert.False(DType.TryParse("Number", out type));
            Assert.False(DType.TryParse("*[A:Number]", out type));
            Assert.False(DType.TryParse("**[]", out type));
            Assert.False(DType.TryParse("*[?]", out type));
            Assert.False(DType.TryParse("*[n]", out type));
            Assert.False(DType.TryParse("*[s]", out type));
            Assert.False(DType.TryParse("*[b]", out type));
            Assert.False(DType.TryParse("*[[]]", out type));
            Assert.False(DType.TryParse("![?]", out type));
            Assert.False(DType.TryParse("![n]", out type));
            Assert.False(DType.TryParse("![s]", out type));
            Assert.False(DType.TryParse("![b]", out type));
            Assert.False(DType.TryParse("![o]", out type));
            Assert.False(DType.TryParse("![[]]", out type));
            Assert.False(DType.TryParse("!!!!", out type));
            Assert.False(DType.TryParse("****", out type));
            Assert.False(DType.TryParse("   ", out type));
            Assert.False(DType.TryParse(":", out type));
            Assert.False(DType.TryParse("[", out type));
            Assert.False(DType.TryParse("]", out type));
            Assert.False(DType.TryParse("![", out type));
            Assert.False(DType.TryParse("![A:n", out type));
            Assert.False(DType.TryParse("!A:n", out type));
            Assert.False(DType.TryParse(",", out type));
            Assert.False(DType.TryParse(",,,,,", out type));
            Assert.False(DType.TryParse("%%%%", out type));
            Assert.False(DType.TryParse("%[]", out type));
            Assert.False(DType.TryParse("%[A:1]", out type));
            Assert.False(DType.TryParse("%n[A:n, B:s]", out type));
            Assert.False(DType.TryParse("%*[A:n][A:2]", out type));
            Assert.False(DType.TryParse("%![A:n][A:2]", out type));
            Assert.False(DType.TryParse("*[''':n]", out type));
            Assert.False(DType.TryParse("*[\"\"\":n]", out type));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestCoercesTo(bool usePowerFxV1CompatibilityRules)
        {
            // Coercion to string
            Assert.True(DType.Guid.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Boolean.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Number.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Currency.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Decimal.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Color.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.DateTime.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Date.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Time.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.String.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Hyperlink.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Image.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.PenImage.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Media.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Blob.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyTable.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyRecord.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.EmptyEnum.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%n[A:2]", out DType type) && type.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.ObjNull.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Error.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Deferred.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Void.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            // Coercion to GUID
            Assert.False(DType.Boolean.CoercesTo(DType.Guid, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Number.CoercesTo(DType.Guid, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Currency.CoercesTo(DType.Guid, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Decimal.CoercesTo(DType.Guid, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Color.CoercesTo(DType.Guid, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.DateTime.CoercesTo(DType.Guid, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Date.CoercesTo(DType.Guid, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Time.CoercesTo(DType.Guid, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.Equal(usePowerFxV1CompatibilityRules, DType.String.CoercesTo(DType.Guid, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Hyperlink.CoercesTo(DType.Guid, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Image.CoercesTo(DType.Guid, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.PenImage.CoercesTo(DType.Guid, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Media.CoercesTo(DType.Guid, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Blob.CoercesTo(DType.Guid, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyTable.CoercesTo(DType.Guid, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyRecord.CoercesTo(DType.Guid, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Guid.CoercesTo(DType.Guid, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.EmptyEnum.CoercesTo(DType.Guid, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.Guid, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.Guid, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.Guid, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.TryParse("%g[A:\"hello\"]", out type) && type.CoercesTo(DType.Guid, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.ObjNull.CoercesTo(DType.Guid, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Error.CoercesTo(DType.Guid, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Deferred.CoercesTo(DType.Guid, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Void.CoercesTo(DType.Guid, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            // Coercion to number
            Assert.True(DType.Boolean.CoercesTo(DType.Number, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Number.CoercesTo(DType.Number, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Currency.CoercesTo(DType.Number, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Decimal.CoercesTo(DType.Number, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Color.CoercesTo(DType.Number, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.DateTime.CoercesTo(DType.Number, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Date.CoercesTo(DType.Number, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Time.CoercesTo(DType.Number, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.String.CoercesTo(DType.Number, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Guid.CoercesTo(DType.Number, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Hyperlink.CoercesTo(DType.Number, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Image.CoercesTo(DType.Number, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.PenImage.CoercesTo(DType.Number, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Media.CoercesTo(DType.Number, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Blob.CoercesTo(DType.Number, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyTable.CoercesTo(DType.Number, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyRecord.CoercesTo(DType.Number, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.EmptyEnum.CoercesTo(DType.Number, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.Number, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.Number, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.Number, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.ObjNull.CoercesTo(DType.Number, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Error.CoercesTo(DType.Number, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Deferred.CoercesTo(DType.Number, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Void.CoercesTo(DType.Number, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            // Coercion to boolean
            Assert.True(DType.Boolean.CoercesTo(DType.Boolean, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Number.CoercesTo(DType.Boolean, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Currency.CoercesTo(DType.Boolean, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Decimal.CoercesTo(DType.Boolean, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Color.CoercesTo(DType.Boolean, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.DateTime.CoercesTo(DType.Boolean, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Date.CoercesTo(DType.Boolean, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Time.CoercesTo(DType.Boolean, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.String.CoercesTo(DType.Boolean, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Hyperlink.CoercesTo(DType.Boolean, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Image.CoercesTo(DType.Boolean, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.PenImage.CoercesTo(DType.Boolean, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Media.CoercesTo(DType.Boolean, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Blob.CoercesTo(DType.Boolean, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyTable.CoercesTo(DType.Boolean, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyRecord.CoercesTo(DType.Boolean, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Guid.CoercesTo(DType.Boolean, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.EmptyEnum.CoercesTo(DType.Boolean, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.Boolean, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.Boolean, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.Boolean, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.ObjNull.CoercesTo(DType.Boolean, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Error.CoercesTo(DType.Boolean, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Deferred.CoercesTo(DType.Boolean, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Void.CoercesTo(DType.Boolean, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            // Coercion to currency
            Assert.True(DType.Boolean.CoercesTo(DType.Currency, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Number.CoercesTo(DType.Currency, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Currency.CoercesTo(DType.Currency, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Decimal.CoercesTo(DType.Currency, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Color.CoercesTo(DType.Currency, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.DateTime.CoercesTo(DType.Currency, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Date.CoercesTo(DType.Currency, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Time.CoercesTo(DType.Currency, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.String.CoercesTo(DType.Currency, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Hyperlink.CoercesTo(DType.Currency, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Image.CoercesTo(DType.Currency, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.PenImage.CoercesTo(DType.Currency, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Media.CoercesTo(DType.Currency, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Blob.CoercesTo(DType.Currency, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyTable.CoercesTo(DType.Currency, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyRecord.CoercesTo(DType.Currency, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.EmptyEnum.CoercesTo(DType.Currency, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Guid.CoercesTo(DType.Currency, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.Currency, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.Currency, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.Currency, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.TryParse("%$[A:2]", out type) && type.CoercesTo(DType.Currency, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.ObjNull.CoercesTo(DType.Currency, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Error.CoercesTo(DType.Currency, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Deferred.CoercesTo(DType.Currency, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Void.CoercesTo(DType.Currency, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            // Coercion to Decimal
            Assert.True(DType.Boolean.CoercesTo(DType.Decimal, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Number.CoercesTo(DType.Decimal, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Color.CoercesTo(DType.Decimal, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.DateTime.CoercesTo(DType.Decimal, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Date.CoercesTo(DType.Decimal, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Time.CoercesTo(DType.Decimal, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.String.CoercesTo(DType.Decimal, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Hyperlink.CoercesTo(DType.Decimal, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Image.CoercesTo(DType.Decimal, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.PenImage.CoercesTo(DType.Decimal, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Media.CoercesTo(DType.Decimal, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Blob.CoercesTo(DType.Decimal, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyTable.CoercesTo(DType.Decimal, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyRecord.CoercesTo(DType.Decimal, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.EmptyEnum.CoercesTo(DType.Decimal, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Guid.CoercesTo(DType.Decimal, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.Decimal, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.Decimal, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.Decimal, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.TryParse("%w[A:2]", out type) && type.CoercesTo(DType.Decimal, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.ObjNull.CoercesTo(DType.Decimal, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Error.CoercesTo(DType.Decimal, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Deferred.CoercesTo(DType.Decimal, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            // Coercion to color
            Assert.False(DType.Boolean.CoercesTo(DType.Color, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Number.CoercesTo(DType.Color, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Currency.CoercesTo(DType.Color, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Decimal.CoercesTo(DType.Color, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Color.CoercesTo(DType.Color, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.DateTime.CoercesTo(DType.Color, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Date.CoercesTo(DType.Color, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Time.CoercesTo(DType.Color, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.String.CoercesTo(DType.Color, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Hyperlink.CoercesTo(DType.Color, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Image.CoercesTo(DType.Color, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.PenImage.CoercesTo(DType.Color, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Media.CoercesTo(DType.Color, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Blob.CoercesTo(DType.Color, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyTable.CoercesTo(DType.Color, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyRecord.CoercesTo(DType.Color, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Guid.CoercesTo(DType.Color, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.EmptyEnum.CoercesTo(DType.Color, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.TryParse("%c[A:2]", out type) && type.CoercesTo(DType.Color, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.Color, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.Color, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.Color, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.ObjNull.CoercesTo(DType.Color, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Error.CoercesTo(DType.Color, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Deferred.CoercesTo(DType.Color, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Void.CoercesTo(DType.Color, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            // Coercion to dateTime
            Assert.False(DType.Boolean.CoercesTo(DType.DateTime, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Number.CoercesTo(DType.DateTime, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.Equal(!usePowerFxV1CompatibilityRules, DType.Currency.CoercesTo(DType.DateTime, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Decimal.CoercesTo(DType.DateTime, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Color.CoercesTo(DType.DateTime, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.DateTime.CoercesTo(DType.DateTime, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Date.CoercesTo(DType.DateTime, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Time.CoercesTo(DType.DateTime, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.String.CoercesTo(DType.DateTime, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Hyperlink.CoercesTo(DType.DateTime, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Image.CoercesTo(DType.DateTime, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.PenImage.CoercesTo(DType.DateTime, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Media.CoercesTo(DType.DateTime, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Blob.CoercesTo(DType.DateTime, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Guid.CoercesTo(DType.DateTime, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyTable.CoercesTo(DType.DateTime, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyRecord.CoercesTo(DType.DateTime, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.EmptyEnum.CoercesTo(DType.DateTime, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.TryParse("%d[A:2]", out type) && type.CoercesTo(DType.DateTime, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.DateTime, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.DateTime, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.DateTime, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.ObjNull.CoercesTo(DType.DateTime, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Error.CoercesTo(DType.DateTime, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Deferred.CoercesTo(DType.DateTime, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Void.CoercesTo(DType.DateTime, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            // Coercion to image
            Assert.False(DType.Boolean.CoercesTo(DType.Image, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Number.CoercesTo(DType.Image, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Currency.CoercesTo(DType.Image, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Decimal.CoercesTo(DType.Image, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Color.CoercesTo(DType.Image, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.DateTime.CoercesTo(DType.Image, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Date.CoercesTo(DType.Image, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Time.CoercesTo(DType.Image, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.String.CoercesTo(DType.Image, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Hyperlink.CoercesTo(DType.Image, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Guid.CoercesTo(DType.Image, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Image.CoercesTo(DType.Image, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.PenImage.CoercesTo(DType.Image, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Media.CoercesTo(DType.Image, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Blob.CoercesTo(DType.Image, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyTable.CoercesTo(DType.Image, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyRecord.CoercesTo(DType.Image, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.EmptyEnum.CoercesTo(DType.Image, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.Image, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.Image, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.Image, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.TryParse("%i[A:\"hello.jpg\"]", out type) && type.CoercesTo(DType.Image, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.ObjNull.CoercesTo(DType.Image, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Error.CoercesTo(DType.Image, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Deferred.CoercesTo(DType.Image, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Void.CoercesTo(DType.Image, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            // Coercion to penimage
            Assert.False(DType.Boolean.CoercesTo(DType.PenImage, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Number.CoercesTo(DType.PenImage, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Currency.CoercesTo(DType.PenImage, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Decimal.CoercesTo(DType.PenImage, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Color.CoercesTo(DType.PenImage, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.DateTime.CoercesTo(DType.PenImage, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Date.CoercesTo(DType.PenImage, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Time.CoercesTo(DType.PenImage, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.String.CoercesTo(DType.PenImage, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Hyperlink.CoercesTo(DType.PenImage, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Guid.CoercesTo(DType.PenImage, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Image.CoercesTo(DType.PenImage, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.PenImage.CoercesTo(DType.PenImage, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Media.CoercesTo(DType.PenImage, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Blob.CoercesTo(DType.PenImage, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyTable.CoercesTo(DType.PenImage, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyRecord.CoercesTo(DType.PenImage, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.EmptyEnum.CoercesTo(DType.PenImage, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.PenImage, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.PenImage, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.PenImage, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.ObjNull.CoercesTo(DType.PenImage, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Error.CoercesTo(DType.PenImage, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Deferred.CoercesTo(DType.PenImage, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Void.CoercesTo(DType.PenImage, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            // Coercion to media
            Assert.False(DType.Boolean.CoercesTo(DType.Media, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Number.CoercesTo(DType.Media, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Currency.CoercesTo(DType.Media, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Decimal.CoercesTo(DType.Media, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Color.CoercesTo(DType.Media, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.DateTime.CoercesTo(DType.Media, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Date.CoercesTo(DType.Media, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Time.CoercesTo(DType.Media, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.String.CoercesTo(DType.Media, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Hyperlink.CoercesTo(DType.Media, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Image.CoercesTo(DType.Media, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.PenImage.CoercesTo(DType.Media, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Media.CoercesTo(DType.Media, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Blob.CoercesTo(DType.Media, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyTable.CoercesTo(DType.Media, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyRecord.CoercesTo(DType.Media, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.EmptyEnum.CoercesTo(DType.Media, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Guid.CoercesTo(DType.Media, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.Media, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.Media, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.Media, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.TryParse("%m[A:\"hello\"]", out type) && type.CoercesTo(DType.Media, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.ObjNull.CoercesTo(DType.Media, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Error.CoercesTo(DType.Media, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Deferred.CoercesTo(DType.Media, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Void.CoercesTo(DType.Media, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            // Coercion to document
            Assert.False(DType.Boolean.CoercesTo(DType.Blob, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Number.CoercesTo(DType.Blob, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Currency.CoercesTo(DType.Blob, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Decimal.CoercesTo(DType.Blob, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Color.CoercesTo(DType.Blob, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.DateTime.CoercesTo(DType.Blob, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Date.CoercesTo(DType.Blob, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Time.CoercesTo(DType.Blob, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.String.CoercesTo(DType.Blob, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Hyperlink.CoercesTo(DType.Blob, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Image.CoercesTo(DType.Blob, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.PenImage.CoercesTo(DType.Blob, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Media.CoercesTo(DType.Blob, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Blob.CoercesTo(DType.Blob, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyTable.CoercesTo(DType.Blob, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyRecord.CoercesTo(DType.Blob, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Guid.CoercesTo(DType.Blob, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.EmptyEnum.CoercesTo(DType.Blob, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.Blob, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.Blob, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.Blob, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.TryParse("%o[A:\"hello\"]", out type) && type.CoercesTo(DType.Blob, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.ObjNull.CoercesTo(DType.Blob, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Error.CoercesTo(DType.Blob, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Deferred.CoercesTo(DType.Blob, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Void.CoercesTo(DType.Blob, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            // Coercion to hyperlink
            Assert.False(DType.Boolean.CoercesTo(DType.Hyperlink, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Number.CoercesTo(DType.Hyperlink, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Currency.CoercesTo(DType.Hyperlink, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Decimal.CoercesTo(DType.Hyperlink, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Color.CoercesTo(DType.Hyperlink, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.DateTime.CoercesTo(DType.Hyperlink, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Date.CoercesTo(DType.Hyperlink, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Time.CoercesTo(DType.Hyperlink, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.String.CoercesTo(DType.Hyperlink, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Hyperlink.CoercesTo(DType.Hyperlink, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Image.CoercesTo(DType.Hyperlink, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.PenImage.CoercesTo(DType.Hyperlink, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Media.CoercesTo(DType.Hyperlink, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Blob.CoercesTo(DType.Hyperlink, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyTable.CoercesTo(DType.Hyperlink, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyRecord.CoercesTo(DType.Hyperlink, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.EmptyEnum.CoercesTo(DType.Hyperlink, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Guid.CoercesTo(DType.Hyperlink, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.Hyperlink, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.Hyperlink, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.Hyperlink, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.TryParse("%h[A:\"hello\"]", out type) && type.CoercesTo(DType.Hyperlink, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.ObjNull.CoercesTo(DType.Hyperlink, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Error.CoercesTo(DType.Hyperlink, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Deferred.CoercesTo(DType.Hyperlink, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Void.CoercesTo(DType.Hyperlink, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            // Coercion to table
            Assert.False(DType.Boolean.CoercesTo(DType.EmptyTable, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Number.CoercesTo(DType.EmptyTable, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Currency.CoercesTo(DType.EmptyTable, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Decimal.CoercesTo(DType.EmptyTable, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Color.CoercesTo(DType.EmptyTable, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.DateTime.CoercesTo(DType.EmptyTable, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Date.CoercesTo(DType.EmptyTable, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Time.CoercesTo(DType.EmptyTable, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.String.CoercesTo(DType.EmptyTable, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Hyperlink.CoercesTo(DType.EmptyTable, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Guid.CoercesTo(DType.EmptyTable, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Image.CoercesTo(DType.EmptyTable, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.PenImage.CoercesTo(DType.EmptyTable, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Media.CoercesTo(DType.EmptyTable, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Blob.CoercesTo(DType.EmptyTable, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.EmptyTable.CoercesTo(DType.EmptyTable, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.EmptyRecord.CoercesTo(DType.EmptyTable, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.TryParse("![A:n]", out type) && type.CoercesTo(DType.EmptyTable, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.TryParse("![A:n]", out DType type1) && DType.TryParse("*[A:n]", out DType type2) && type1.CoercesTo(type2, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.TryParse("![A:n, B:s]", out type1) && DType.TryParse("*[A:n]", out type2) && type1.CoercesTo(type2, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.TryParse("![A:*[B:s]]", out type1) && DType.TryParse("*[A:*[B:s]]", out type2) && type1.CoercesTo(type2, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.TryParse("![A:*[B:![C:n]]]", out type1) && DType.TryParse("*[A:*[B:![C:n]]]", out type2) && type1.CoercesTo(type2, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.TryParse("![A:*[B:s]]", out type1) && DType.TryParse("*[A:*[B:n]]", out type2) && type1.CoercesTo(type2, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.TryParse("![A:*[B:![C:n]]]", out type1) && DType.TryParse("*[A:*[B:*[C:n]]]", out type2) && type1.CoercesTo(type2, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.TryParse("![A:![B:s]]", out type1) && DType.TryParse("*[A:*[B:s]]", out type2) && type1.CoercesTo(type2, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            Assert.False(DType.TryParse("![A:*[B:s]]", out type1) && DType.TryParse("*[A:n]", out type2) && type1.CoercesTo(type2, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.Equal(usePowerFxV1CompatibilityRules, DType.TryParse("![A:n]", out type1) && DType.TryParse("*[A:n, B:s]", out type2) && type1.CoercesTo(type2, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("![A:*[B:*[C:n]]]", out type1) && DType.TryParse("*[A:*[B:![C:n]]]", out type2) && type1.CoercesTo(type2, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            Assert.False(DType.EmptyEnum.CoercesTo(DType.EmptyTable, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.EmptyTable, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.EmptyTable, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.EmptyTable, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.ObjNull.CoercesTo(DType.EmptyTable, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Error.CoercesTo(DType.EmptyTable, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Deferred.CoercesTo(DType.EmptyTable, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Void.CoercesTo(DType.EmptyTable, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            // Coercion to record
            Assert.False(DType.Boolean.CoercesTo(DType.EmptyRecord, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Number.CoercesTo(DType.EmptyRecord, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Currency.CoercesTo(DType.EmptyRecord, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Decimal.CoercesTo(DType.EmptyRecord, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Color.CoercesTo(DType.EmptyRecord, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.DateTime.CoercesTo(DType.EmptyRecord, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Date.CoercesTo(DType.EmptyRecord, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Time.CoercesTo(DType.EmptyRecord, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.String.CoercesTo(DType.EmptyRecord, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Hyperlink.CoercesTo(DType.EmptyRecord, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Image.CoercesTo(DType.EmptyRecord, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.PenImage.CoercesTo(DType.EmptyRecord, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Media.CoercesTo(DType.EmptyRecord, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Guid.CoercesTo(DType.EmptyRecord, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Blob.CoercesTo(DType.EmptyRecord, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyTable.CoercesTo(DType.EmptyRecord, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.EmptyRecord.CoercesTo(DType.EmptyRecord, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyEnum.CoercesTo(DType.EmptyRecord, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.EmptyRecord, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.EmptyRecord, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.EmptyRecord, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.ObjNull.CoercesTo(DType.EmptyRecord, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Error.CoercesTo(DType.EmptyRecord, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Deferred.CoercesTo(DType.EmptyRecord, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Void.CoercesTo(DType.EmptyRecord, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            // Coercion to Date
            Assert.False(DType.Boolean.CoercesTo(DType.Date, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Number.CoercesTo(DType.Date, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.Equal(!usePowerFxV1CompatibilityRules, DType.Currency.CoercesTo(DType.Date, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Decimal.CoercesTo(DType.Date, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Color.CoercesTo(DType.Date, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.DateTime.CoercesTo(DType.Date, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Date.CoercesTo(DType.Date, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Time.CoercesTo(DType.Date, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.String.CoercesTo(DType.Date, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Hyperlink.CoercesTo(DType.Date, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Image.CoercesTo(DType.Date, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.PenImage.CoercesTo(DType.Date, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Guid.CoercesTo(DType.Date, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Media.CoercesTo(DType.Date, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Blob.CoercesTo(DType.Date, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyTable.CoercesTo(DType.Date, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyRecord.CoercesTo(DType.Date, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.EmptyEnum.CoercesTo(DType.Date, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.TryParse("%D[A:2]", out type) && type.CoercesTo(DType.Date, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.Date, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.Date, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.Date, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.ObjNull.CoercesTo(DType.Date, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Error.CoercesTo(DType.Date, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Deferred.CoercesTo(DType.Date, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Void.CoercesTo(DType.Date, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            // Coercion to Time
            Assert.False(DType.Boolean.CoercesTo(DType.Time, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Number.CoercesTo(DType.Time, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.Equal(!usePowerFxV1CompatibilityRules, DType.Currency.CoercesTo(DType.Time, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Decimal.CoercesTo(DType.Time, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Color.CoercesTo(DType.Time, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.DateTime.CoercesTo(DType.Time, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Date.CoercesTo(DType.Time, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Time.CoercesTo(DType.Time, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.String.CoercesTo(DType.Time, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Hyperlink.CoercesTo(DType.Time, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Image.CoercesTo(DType.Time, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Guid.CoercesTo(DType.Time, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.PenImage.CoercesTo(DType.Time, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Media.CoercesTo(DType.Time, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Blob.CoercesTo(DType.Time, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyTable.CoercesTo(DType.Time, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyRecord.CoercesTo(DType.Time, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.EmptyEnum.CoercesTo(DType.Time, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.TryParse("%T[A:2]", out type) && type.CoercesTo(DType.Time, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.Time, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.Time, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.Time, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.ObjNull.CoercesTo(DType.Time, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Error.CoercesTo(DType.Time, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Deferred.CoercesTo(DType.Time, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Void.CoercesTo(DType.Time, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            // Coercion to Attachment Table type
            Assert.False(DType.Boolean.CoercesTo(AttachmentTableType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Number.CoercesTo(AttachmentTableType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Currency.CoercesTo(AttachmentTableType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Decimal.CoercesTo(AttachmentTableType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Color.CoercesTo(AttachmentTableType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Guid.CoercesTo(AttachmentTableType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.DateTime.CoercesTo(AttachmentTableType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Date.CoercesTo(AttachmentTableType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Time.CoercesTo(AttachmentTableType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.String.CoercesTo(AttachmentTableType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Hyperlink.CoercesTo(AttachmentTableType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Image.CoercesTo(AttachmentTableType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.PenImage.CoercesTo(AttachmentTableType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Media.CoercesTo(AttachmentTableType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Blob.CoercesTo(AttachmentTableType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.Equal(usePowerFxV1CompatibilityRules, DType.EmptyTable.CoercesTo(AttachmentTableType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.Equal(usePowerFxV1CompatibilityRules, DType.EmptyRecord.CoercesTo(AttachmentTableType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(AttachmentTableType.CoercesTo(AttachmentTableType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyEnum.CoercesTo(AttachmentTableType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(AttachmentTableType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.ObjNull.CoercesTo(AttachmentTableType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Error.CoercesTo(AttachmentTableType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Deferred.CoercesTo(AttachmentTableType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Void.CoercesTo(AttachmentTableType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            // Coercion to Attachment Record type
            Assert.False(DType.Boolean.CoercesTo(AttachmentRecordType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Number.CoercesTo(AttachmentRecordType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Currency.CoercesTo(AttachmentRecordType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Decimal.CoercesTo(AttachmentRecordType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Color.CoercesTo(AttachmentRecordType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Guid.CoercesTo(AttachmentRecordType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.DateTime.CoercesTo(AttachmentRecordType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Date.CoercesTo(AttachmentRecordType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Time.CoercesTo(AttachmentRecordType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.String.CoercesTo(AttachmentRecordType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Hyperlink.CoercesTo(AttachmentRecordType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Image.CoercesTo(AttachmentRecordType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.PenImage.CoercesTo(AttachmentRecordType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Media.CoercesTo(AttachmentRecordType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Blob.CoercesTo(AttachmentRecordType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyTable.CoercesTo(AttachmentRecordType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.Equal(usePowerFxV1CompatibilityRules, DType.EmptyRecord.CoercesTo(AttachmentRecordType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(AttachmentRecordType.CoercesTo(AttachmentRecordType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.EmptyEnum.CoercesTo(AttachmentRecordType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(AttachmentRecordType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.ObjNull.CoercesTo(AttachmentRecordType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Error.CoercesTo(AttachmentRecordType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.True(DType.Deferred.CoercesTo(AttachmentRecordType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
            Assert.False(DType.Void.CoercesTo(AttachmentRecordType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));

            // Coercion to Error type
            Assert.True(DType.Error.CoercesTo(DType.Error, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
        }

        [Fact]
        public void DTypeTestOptionSetCoercion()
        {
            foreach (var usePFxV1CompatRules in new[] { false, true })
            {
                Assert.True(OptionSetValueType.Accepts(OptionSetValueType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePFxV1CompatRules));
                Assert.True(OptionSetType.CoercesTo(OptionSetType, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePFxV1CompatRules));

                Assert.False(OptionSetValueType.CoercesTo(DType.Boolean, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePFxV1CompatRules));
                Assert.True(OptionSetValueType.CoercesTo(DType.String, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePFxV1CompatRules));

                Assert.True(BooleanValuedOptionSetValueType.CoercesTo(DType.Boolean, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: usePFxV1CompatRules));
            }
        }

        private void TestUnion(string t1, string t2, string tResult, bool usePowerFxV1CompatibilityRules)
        {
            DType type1 = TestUtils.DT(t1);
            Assert.True(type1.IsValid);
            DType type2 = TestUtils.DT(t2);
            Assert.True(type2.IsValid);
            DType typeResult = TestUtils.DT(tResult);
            Assert.True(typeResult.IsValid);
            Assert.Equal<DType>(typeResult, DType.Union(type1, type2, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
        }

        private void TestUnion(DType type1, DType type2, DType typeResult, bool usePowerFxV1CompatibilityRules)
        {
            Assert.True(type1.IsValid);
            Assert.True(type2.IsValid);
            Assert.True(typeResult.IsValid);
            Assert.Equal<DType>(typeResult, DType.Union(type1, type2, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules));
        }

        [Fact]
        public void DTypeUnion_LegacyPowerFxV1CompatibilityRulesDisabled()
        {
            TestUnion("n", "n", "n", false);
            TestUnion("n", "w", "e", false);
            TestUnion("n", "c", "e", false);
            TestUnion("n", "d", "e", false);
            TestUnion("w", "n", "e", false);
            TestUnion("w", "d", "e", false);
            TestUnion("w", "D", "e", false);
            TestUnion("w", "T", "e", false);
            TestUnion("c", "n", "e", false);
            TestUnion("d", "n", "e", false);
            TestUnion("d", "w", "e", false);
            TestUnion("n", "o", "e", false);
            TestUnion("o", "n", "e", false);

            TestUnion("b", "b", "b", false);
            TestUnion("b", "n", "e", false);
            TestUnion("b", "s", "e", false);
            TestUnion("b", "w", "e", false);
            TestUnion("b", "o", "e", false);
            TestUnion("o", "b", "e", false);

            TestUnion("p", "w", "e", false);
            TestUnion("p", "n", "e", false);
            TestUnion("p", "c", "e", false);
            TestUnion("p", "b", "e", false);
            TestUnion("p", "m", "h", false);
            TestUnion("p", "o", "h", false);
            TestUnion("o", "p", "h", false);

            TestUnion("s", "s", "s", false);
            TestUnion("s", "h", "s", false);
            TestUnion("s", "i", "s", false);
            TestUnion("s", "m", "s", false);
            TestUnion("s", "o", "s", false);
            TestUnion("o", "s", "s", false);
            TestUnion("s", "g", "s", false);
            TestUnion("g", "s", "s", false);

            TestUnion("h", "m", "h", false);
            TestUnion("h", "s", "s", false);
            TestUnion("i", "s", "s", false);
            TestUnion("i", "h", "h", false);
            TestUnion("i", "m", "h", false);
            TestUnion("p", "i", "i", false);
            TestUnion("p", "h", "h", false);
            TestUnion("p", "s", "s", false);

            TestUnion("c", "c", "c", false);
            TestUnion("w", "w", "w", false);
            TestUnion("h", "h", "h", false);
            TestUnion("i", "i", "i", false);
            TestUnion("p", "p", "p", false);
            TestUnion("d", "d", "d", false);
            TestUnion("m", "m", "m", false);
            TestUnion("o", "o", "o", false);

            TestUnion("D", "T", "d", false);
            TestUnion("T", "D", "d", false);
            TestUnion("d", "T", "d", false);
            TestUnion("d", "D", "d", false);
            TestUnion("T", "d", "d", false);
            TestUnion("D", "d", "d", false);
            TestUnion("D", "w", "e", false);
            TestUnion("T", "w", "e", false);

            TestUnion("*[]", "*[]", "*[]", false);

            TestUnion("*[A:n]", "*[]", "*[A:n]", false);
            TestUnion("*[]", "*[A:n]", "*[A:n]", false);
            TestUnion("*[A:n]", "*[A:w]", "*[A:e]", false);
            TestUnion("*[A:w]", "*[A:n]", "*[A:e]", false);

            TestUnion("*[A:n]", "*[B:n]", "*[A:n, B:n]", false);
            TestUnion("*[A:n]", "*[B:s]", "*[A:n, B:s]", false);
            TestUnion("*[A:n]", "*[B:b]", "*[A:n, B:b]", false);
            TestUnion("*[A:n]", "*[B:w]", "*[A:n, B:w]", false);
            TestUnion("*[A:n]", "X", "*[A:n]", false);

            TestUnion("*[]", "*[A:n, B:b, D:d]", "*[A:n, B:b, D:d]", false);
            TestUnion("*[A:n, B:b, D:d]", "*[]", "*[A:n, B:b, D:d]", false);
            TestUnion("*[A:n, B:b, D:d]", "*[A:n, B:b]", "*[A:n, B:b, D:d]", false);
            TestUnion("*[A:n, B:b, D:d]", "*[X:s, Y:n]", "*[A:n, B:b, D:d, X:s, Y:n]", false);
            TestUnion("*[A:n, B:b, D:d]", "X", "*[A:n, B:b, D:d]", false);

            // Tests for Type DataNull, DataNull is compatable with any data type, regardless of order.
            TestUnion("N", "N", "N", false);
            TestUnion("s", "N", "s", false);
            TestUnion("b", "N", "b", false);
            TestUnion("n", "N", "n", false);
            TestUnion("i", "N", "i", false);
            TestUnion("N", "i", "i", false);
            TestUnion("w", "N", "w", false);
            TestUnion("h", "N", "h", false);
            TestUnion("o", "N", "o", false);
            TestUnion("c", "N", "c", false);
            TestUnion("N", "c", "c", false);
            TestUnion("p", "N", "p", false);
            TestUnion("m", "N", "m", false);
            TestUnion("e", "N", "e", false);
            TestUnion("*[]", "N", "*[]", false);
            TestUnion("N", "*[]", "*[]", false);
            TestUnion("*[A:N]", "*[A:w]", "*[A:w]", false);
            TestUnion("*[A:b]", "*[A:N]", "*[A:b]", false);
            TestUnion("*[A:N]", "*[A:b]", "*[A:b]", false);
            TestUnion("*[A:N]", "*[A:s]", "*[A:s]", false);
            TestUnion("*[A:e]", "*[A:N]", "*[A:e]", false);
            TestUnion("*[A:n]", "*[A:N]", "*[A:n]", false);
            TestUnion("*[A:n, B:b, D:s]", "*[D:N]", "*[A:n, B:b, D:s]", false);
            TestUnion("*[A:n, B:b, D:*[A:s]]", "*[D:N]", "*[A:n, B:b, D:*[A:s]]", false);

            // Nested aggregates
            TestUnion("*[A:*[A:![X:n, Y:b]]]", "*[A:*[A:![Z:s]]]", "*[A:*[A:![X:n, Y:b, Z:s]]]", false);
            TestUnion("![A:n, Nest:*[X:n, Y:n, Z:b]]", "![]", "![A:n, Nest:*[X:n, Y:n, Z:b]]", false);
            TestUnion("*[A:n, Nest:*[X:n, Y:n, Z:b]]", "*[]", "*[A:n, Nest:*[X:n, Y:n, Z:b]]", false);
            TestUnion("*[A:n, Nest:*[X:n, Y:c, Z:b]]", "*[X:s, Nest:*[X:w, Y:n, W:s]]", "*[A:n, X:s, Nest:*[X:e, Y:e, Z:b, W:s]]", false);
            TestUnion("*[A:n, Nest:*[X:n, Y:c, Z:b]]", "X", "*[A:n, Nest:*[X:n, Y:c, Z:b]]", false);

            // Unresolvable conflicts
            TestUnion("*[A:n]", "*[A:s]", "*[A:e]", false);
            TestUnion("*[A:n, B:b, D:s]", "*[A:n, B:s, D:s]", "*[A:n, B:e, D:s]", false);
            TestUnion("*[A:n]", "![B:n]", "e", false);

            //Attachment
            var type1 = DType.CreateAttachmentType(DType.CreateAttachmentType(DType.CreateTable(new TypedName(DType.String, new DName("DisplayName")))));
            var type2 = DType.CreateAttachmentType(DType.CreateAttachmentType(DType.CreateTable(new TypedName(DType.String, new DName("Name")))));
            TestUnion(type1, type1, type1, false);
            TestUnion(type1, type2, TestUtils.DT("*[DisplayName:s, Name:s]"), false);
            TestUnion(type2, type2, type2, false);
            TestUnion(DType.Unknown, type1, type1.LazyTypeProvider.GetExpandedType(type1.IsTable), false);
            TestUnion(DType.ObjNull, type1, type1.LazyTypeProvider.GetExpandedType(type1.IsTable), false);

            var typeEncodings = "ebnshdipmgo$wcDTlLZPQqVOXw";
            foreach (var type in typeEncodings)
            {
                TestUnion(type.ToString(), "X", type.ToString(), false);
                TestUnion(type.ToString(), "-", "e", false);
            }
        }

        [Theory]

        // First is number
        [InlineData("n", "n", "n")]
        [InlineData("n", "$", "n")]
        [InlineData("n", "w", "n")]
        [InlineData("n", "c", "e")]
        [InlineData("n", "d", "n")]
        [InlineData("n", "D", "n")]
        [InlineData("n", "T", "n")]
        [InlineData("n", "s", "n")]
        [InlineData("n", "g", "e")]
        [InlineData("n", "b", "n")]
        [InlineData("n", "i", "e")]
        [InlineData("n", "o", "e")]
        [InlineData("n", "m", "e")]
        [InlineData("n", "h", "e")]

        // First is decimal
        [InlineData("w", "n", "w")]
        [InlineData("w", "$", "w")]
        [InlineData("w", "w", "w")]
        [InlineData("w", "c", "e")]
        [InlineData("w", "d", "w")]
        [InlineData("w", "D", "w")]
        [InlineData("w", "T", "w")]
        [InlineData("w", "s", "w")]
        [InlineData("w", "g", "e")]
        [InlineData("w", "b", "w")]
        [InlineData("w", "i", "e")]
        [InlineData("w", "o", "e")]
        [InlineData("w", "m", "e")]
        [InlineData("w", "h", "e")]

        // First is currency
        [InlineData("$", "n", "$")]
        [InlineData("$", "$", "$")]
        [InlineData("$", "w", "$")]
        [InlineData("$", "c", "e")]
        [InlineData("$", "d", "e")]
        [InlineData("$", "D", "e")]
        [InlineData("$", "T", "e")]
        [InlineData("$", "s", "$")]
        [InlineData("$", "g", "e")]
        [InlineData("$", "b", "$")]
        [InlineData("$", "i", "e")]
        [InlineData("$", "o", "e")]
        [InlineData("$", "m", "e")]
        [InlineData("$", "h", "e")]

        // First is boolean
        [InlineData("b", "n", "b")]
        [InlineData("b", "$", "b")]
        [InlineData("b", "w", "b")]
        [InlineData("b", "c", "e")]
        [InlineData("b", "d", "e")]
        [InlineData("b", "D", "e")]
        [InlineData("b", "T", "e")]
        [InlineData("b", "s", "b")]
        [InlineData("b", "g", "e")]
        [InlineData("b", "b", "b")]
        [InlineData("b", "i", "e")]
        [InlineData("b", "o", "e")]
        [InlineData("b", "m", "e")]
        [InlineData("b", "h", "e")]

        // First is string
        [InlineData("s", "n", "s")]
        [InlineData("s", "$", "s")]
        [InlineData("s", "w", "s")]
        [InlineData("s", "c", "e")]
        [InlineData("s", "d", "s")]
        [InlineData("s", "D", "s")]
        [InlineData("s", "T", "s")]
        [InlineData("s", "s", "s")]
        [InlineData("s", "g", "s")]
        [InlineData("s", "b", "s")]
        [InlineData("s", "i", "s")]
        [InlineData("s", "o", "s")]
        [InlineData("s", "m", "s")]
        [InlineData("s", "h", "s")]

        // First is guid
        [InlineData("g", "n", "e")]
        [InlineData("g", "$", "e")]
        [InlineData("g", "w", "e")]
        [InlineData("g", "c", "e")]
        [InlineData("g", "d", "e")]
        [InlineData("g", "D", "e")]
        [InlineData("g", "T", "e")]
        [InlineData("g", "s", "g")]
        [InlineData("g", "g", "g")]
        [InlineData("g", "b", "e")]
        [InlineData("g", "i", "e")]
        [InlineData("g", "o", "e")]
        [InlineData("g", "m", "e")]
        [InlineData("g", "h", "e")]

        // First is color
        [InlineData("c", "n", "e")]
        [InlineData("c", "$", "e")]
        [InlineData("c", "w", "e")]
        [InlineData("c", "c", "c")]
        [InlineData("c", "d", "e")]
        [InlineData("c", "D", "e")]
        [InlineData("c", "T", "e")]
        [InlineData("c", "s", "e")]
        [InlineData("c", "g", "e")]
        [InlineData("c", "b", "e")]
        [InlineData("c", "i", "e")]
        [InlineData("c", "o", "e")]
        [InlineData("c", "m", "e")]
        [InlineData("c", "h", "e")]

        // First is media
        [InlineData("m", "n", "e")]
        [InlineData("m", "$", "e")]
        [InlineData("m", "w", "e")]
        [InlineData("m", "c", "e")]
        [InlineData("m", "d", "e")]
        [InlineData("m", "D", "e")]
        [InlineData("m", "T", "e")]
        [InlineData("m", "s", "m")]
        [InlineData("m", "g", "e")]
        [InlineData("m", "b", "e")]
        [InlineData("m", "i", "e")]
        [InlineData("m", "o", "m")]
        [InlineData("m", "m", "m")]
        [InlineData("m", "h", "m")]

        // First is image
        [InlineData("i", "n", "e")]
        [InlineData("i", "$", "e")]
        [InlineData("i", "w", "e")]
        [InlineData("i", "c", "e")]
        [InlineData("i", "d", "e")]
        [InlineData("i", "D", "e")]
        [InlineData("i", "T", "e")]
        [InlineData("i", "s", "i")]
        [InlineData("i", "g", "e")]
        [InlineData("i", "b", "e")]
        [InlineData("i", "i", "i")]
        [InlineData("i", "o", "i")]
        [InlineData("i", "m", "e")]
        [InlineData("i", "h", "i")]

        // First is blob
        [InlineData("o", "n", "e")]
        [InlineData("o", "$", "e")]
        [InlineData("o", "w", "e")]
        [InlineData("o", "c", "e")]
        [InlineData("o", "d", "e")]
        [InlineData("o", "D", "e")]
        [InlineData("o", "T", "e")]
        [InlineData("o", "s", "o")]
        [InlineData("o", "g", "e")]
        [InlineData("o", "b", "e")]
        [InlineData("o", "i", "o")]
        [InlineData("o", "o", "o")]
        [InlineData("o", "m", "o")]
        [InlineData("o", "h", "o")]

        // First is hyperlink
        [InlineData("h", "n", "e")]
        [InlineData("h", "$", "e")]
        [InlineData("h", "w", "e")]
        [InlineData("h", "c", "e")]
        [InlineData("h", "d", "e")]
        [InlineData("h", "D", "e")]
        [InlineData("h", "T", "e")]
        [InlineData("h", "s", "h")]
        [InlineData("h", "g", "e")]
        [InlineData("h", "b", "e")]
        [InlineData("h", "i", "h")]
        [InlineData("h", "o", "h")]
        [InlineData("h", "m", "h")]
        [InlineData("h", "h", "h")]
        public void DTypeUnion_PowerFxV1CompatRules_Primitives(string type1, string type2, string typeResult)
        {
            TestUnion(type1, type2, typeResult, true);
        }

        [Theory]
        [InlineData("*[]", "*[]", "*[]")]

        [InlineData("*[A:n]", "*[]", "*[A:n]")]
        [InlineData("*[]", "*[A:n]", "*[A:n]")]
        [InlineData("*[A:n]", "*[A:$]", "*[A:n]")]
        [InlineData("*[A:$]", "*[A:n]", "*[A:$]")]
        [InlineData("*[A:n]", "*[A:w]", "*[A:n]")]
        [InlineData("*[A:w]", "*[A:n]", "*[A:w]")]

        [InlineData("*[A:n]", "*[B:n]", "*[A:n, B:n]")]
        [InlineData("*[A:n]", "*[B:s]", "*[A:n, B:s]")]
        [InlineData("*[A:n]", "*[B:b]", "*[A:n, B:b]")]
        [InlineData("*[A:n]", "*[B:w]", "*[A:n, B:w]")]
        [InlineData("*[A:n]", "X", "*[A:n]")]

        [InlineData("*[]", "*[A:n, B:b, D:d]", "*[A:n, B:b, D:d]")]
        [InlineData("*[A:n, B:b, D:d]", "*[]", "*[A:n, B:b, D:d]")]
        [InlineData("*[A:n, B:b, D:d]", "*[A:n, B:b]", "*[A:n, B:b, D:d]")]
        [InlineData("*[A:n, B:b, D:d]", "*[X:s, Y:n]", "*[A:n, B:b, D:d, X:s, Y:n]")]
        [InlineData("*[A:n, B:b, D:d]", "X", "*[A:n, B:b, D:d]")]

        [InlineData("*[A:*[A:![X:n, Y:b]]]", "*[A:*[A:![Z:s]]]", "*[A:*[A:![X:n, Y:b, Z:s]]]")]
        [InlineData("![A:n, Nest:*[X:n, Y:n, Z:b]]", "![]", "![A:n, Nest:*[X:n, Y:n, Z:b]]")]
        [InlineData("*[A:n, Nest:*[X:n, Y:n, Z:b]]", "*[]", "*[A:n, Nest:*[X:n, Y:n, Z:b]]")]
        [InlineData(
                "*[A:n, Nest:*[X:n, Y:c, Z:b]]",
                "*[X:s, Nest:*[X:$, Y:n, W:s]]",
                "*[A:n, X:s, Nest:*[X:n, Y:e, Z:b, W:s]]")]
        [InlineData("*[A:n, Nest:*[X:n, Y:c, Z:b]]", "*[X:s, Nest:*[X:w, Y:n, W:s]]", "*[A:n, X:s, Nest:*[X:n, Y:e, Z:b, W:s]]")]
        [InlineData("*[A:n, Nest:*[X:n, Y:c, Z:b]]", "X", "*[A:n, Nest:*[X:n, Y:c, Z:b]]")]
        public void DTypeUnion_PowerFxV1CompatRules_Tables(string type1, string type2, string typeResult)
        {
            TestUnion(type1, type2, typeResult, true);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DTypeUnion_ObjNull_CompatibleWithAll(bool usePFxV1CompatRules)
        {
            var typeEncodings = "ebnshdipmgo$wcDTlLZPQqVOw".ToCharArray()
                .Select(c => c.ToString())
                .Concat(new[] { "![a:n]", "![]", "*[]", "*[a:w,b:b]" });
            foreach (var type in typeEncodings)
            {
                TestUnion(type, "N", type, usePFxV1CompatRules);
                TestUnion("N", type, type, usePFxV1CompatRules);

                var tableType = $"*[A:{type}]";
                var nullTableType = "*[A:N]";

                TestUnion(tableType, nullTableType, tableType, usePFxV1CompatRules);
                TestUnion(nullTableType, tableType, tableType, usePFxV1CompatRules);
            }

            TestUnion("*[A:n, B:b, D:s]", "*[D:N]", "*[A:n, B:b, D:s]", usePFxV1CompatRules);
            TestUnion("*[A:n, B:b, D:*[A:s]]", "*[D:N]", "*[A:n, B:b, D:*[A:s]]", usePFxV1CompatRules);

            TestUnion("N", "X", "X", usePFxV1CompatRules);
            TestUnion("X", "N", "X", usePFxV1CompatRules);

            TestUnion("-", "N", "-", usePFxV1CompatRules);
            TestUnion("N", "-", "-", usePFxV1CompatRules);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DTypeUnion_Unknown_CompatibleWithAll(bool usePFxV1CompatRules)
        {
            var typeEncodings = "ebnshdipmgo$wcDTlLZPQqVOw".ToCharArray()
                .Select(c => c.ToString());
            foreach (var type in typeEncodings)
            {
                TestUnion(type, "?", type, usePFxV1CompatRules);
                TestUnion("?", type, type, usePFxV1CompatRules);
            }

            TestUnion("?", "N", "N", usePFxV1CompatRules);
            TestUnion("N", "?", "N", usePFxV1CompatRules);
        }

        [Theory]
        [InlineData("n", "![A:n]", "e")]
        [InlineData("*[A:n]", "*[A:c]", "*[A:e]")]
        [InlineData("*[A:![B:w]]", "*[A:w]", "*[A:e]")]
        [InlineData("*[A:n]", "![B:n]", "e")]
        public void DTypeUnion_PowerFxV1CompatRules_UnresolvableConflicts(string type1, string type2, string typeResult)
        {
            foreach (var usePFxV1CompatRules in new[] { false, true })
            {
                TestUnion(type1, type2, typeResult, usePFxV1CompatRules);
            }
        }

        [Fact]
        public void DTypeUnion_PowerFxV1CompatRules_Attachmentss()
        {
            foreach (var usePFxV1CompatRules in new[] { false, true })
            {
                var type1 = DType.CreateAttachmentType(DType.CreateAttachmentType(DType.CreateTable(new TypedName(DType.String, new DName("DisplayName")))));
                var type2 = DType.CreateAttachmentType(DType.CreateAttachmentType(DType.CreateTable(new TypedName(DType.String, new DName("Name")))));
                TestUnion(type1, type1, type1, usePFxV1CompatRules);
                TestUnion(type1, type2, TestUtils.DT("*[DisplayName:s, Name:s]"), usePFxV1CompatRules);
                TestUnion(type2, type2, type2, usePFxV1CompatRules);
                TestUnion(DType.Unknown, type1, type1.LazyTypeProvider.GetExpandedType(type1.IsTable), usePFxV1CompatRules);
                TestUnion(DType.ObjNull, type1, type1.LazyTypeProvider.GetExpandedType(type1.IsTable), usePFxV1CompatRules);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DTypeUnion_PowerFxV1CompatRules_VoidNotCompatibleWithAnything(bool usePFxV1CompatRules)
        {
            var typeEncodings = "ebnshdipmgo$wcDTlLZPQqVOXw";
            foreach (var type in typeEncodings)
            {
                TestUnion(type.ToString(), "-", "e", usePFxV1CompatRules);
                TestUnion("-", type.ToString(), "e", usePFxV1CompatRules);
            }
        }

        [Fact]
        public void DTypeAggregateWithFunkyFieldsToString()
        {
            string typeStr;
            DType type;

            typeStr = "*['Last=!5':n]";
            type = TestUtils.DT(typeStr);
            Assert.True(type.IsAggregate);
            Assert.Equal(1, type.ChildCount);
            Assert.Equal(typeStr, type.ToString());

            typeStr = "*[A:n, B:b, C:w, 'Last=!5':n]";
            type = TestUtils.DT(typeStr);
            Assert.True(type.IsAggregate);
            Assert.Equal(4, type.ChildCount);
            Assert.Equal(typeStr, type.ToString());

            typeStr = "*[A:n, B:b, C:w, 'Last=!5':n, 'X,,,=!#@w%':n]";
            type = TestUtils.DT(typeStr);
            Assert.True(type.IsAggregate);
            Assert.Equal(5, type.ChildCount);
            Assert.Equal(typeStr, type.ToString());

            typeStr = "*[A:n, B:b, 'C() * 3/123 - Infinity':w, 'Last=!5':n, 'X,,,=!#@w%':n]";
            type = TestUtils.DT(typeStr);
            Assert.True(type.IsAggregate);
            Assert.Equal(5, type.ChildCount);
            Assert.Equal(typeStr, type.ToString());
        }

        [Fact]
        public void DTypeEnumWithFunkyValuesToString()
        {
            string typeStr;
            DType type;

            typeStr = "%n['Last=!5':10, X:123]";
            type = TestUtils.DT(typeStr);
            Assert.True(type.IsEnum);
            Assert.Equal(typeStr, type.ToString());

            typeStr = "%n['Last=!5':10, 'X Y Z':123]";
            type = TestUtils.DT(typeStr);
            Assert.True(type.IsEnum);
            Assert.Equal(typeStr, type.ToString());

            typeStr = "%n['Last=!5':10, 'X=!Y+Z':123]";
            type = TestUtils.DT(typeStr);
            Assert.True(type.IsEnum);
            Assert.Equal(typeStr, type.ToString());
        }

        [Fact]
        public void DTypeIntersects()
        {
            Assert.True(TestUtils.DT("*[A:n]").Intersects(TestUtils.DT("*[A:n]")));
            Assert.True(TestUtils.DT("![A:n]").Intersects(TestUtils.DT("![A:n]")));
            Assert.True(TestUtils.DT("*[A:n, B:n, C:s]").Intersects(TestUtils.DT("*[X:n, Y:![Z:![W:![Q:*[A:n]]]], B:n]")));
            Assert.True(TestUtils.DT("![A:n, B:n, C:s]").Intersects(TestUtils.DT("![X:n, Y:![Z:![W:![Q:*[A:n]]]], B:n]")));
            Assert.True(TestUtils.DT("*[A:n, B:n, C:s]").Intersects(TestUtils.DT("*[C:s]")));
            Assert.True(TestUtils.DT("![A:n, B:n, C:s]").Intersects(TestUtils.DT("![C:s]")));
            Assert.True(TestUtils.DT("*[C:s]").Intersects(TestUtils.DT("*[A:n, B:b, C:s]")));
            Assert.True(TestUtils.DT("![C:s]").Intersects(TestUtils.DT("![A:n, B:b, C:s]")));
            Assert.True(TestUtils.DT("*[C:*[X:![A:*[B:![C:n]]]], A:n]").Intersects(TestUtils.DT("*[A:s, B:b, C:*[X:![A:*[B:![C:n]]]]]")));
        }

        [Fact]
        public void DTypeIntersects_Negative()
        {
            Assert.False(TestUtils.DT("*[]").Intersects(TestUtils.DT("*[]")));
            Assert.False(TestUtils.DT("![]").Intersects(TestUtils.DT("![]")));
            Assert.False(TestUtils.DT("*[]").Intersects(TestUtils.DT("![]")));
            Assert.False(TestUtils.DT("![]").Intersects(TestUtils.DT("*[]")));
            Assert.False(TestUtils.DT("*[A:n]").Intersects(TestUtils.DT("![A:n]")));
            Assert.False(TestUtils.DT("![A:n]").Intersects(TestUtils.DT("*[A:n]")));
            Assert.False(TestUtils.DT("*[A:n]").Intersects(TestUtils.DT("![A:s]")));
            Assert.False(TestUtils.DT("*[A:n]").Intersects(TestUtils.DT("*[A:![B:n]]")));
            Assert.False(TestUtils.DT("*[]").Intersects(TestUtils.DT("*[A:![B:n]]")));
            Assert.False(TestUtils.DT("![]").Intersects(TestUtils.DT("![A:![B:n]]")));
            Assert.False(TestUtils.DT("*[A:![B:n]]").Intersects(TestUtils.DT("*[]")));
            Assert.False(TestUtils.DT("![A:![B:n]]").Intersects(TestUtils.DT("![]")));
            Assert.False(TestUtils.DT("*[A:n, B:n, C:s]").Intersects(TestUtils.DT("*[X:n, Y:![Z:![W:![Q:*[A:n]]]], Z:n]")));
        }

        [Theory]
        [InlineData("n", false)]
        [InlineData("b", false)]
        [InlineData("s", false)]
        [InlineData("w", false)]
        [InlineData("c", false)]
        [InlineData("p", false)]
        [InlineData("d", false)]
        [InlineData("h", false)]
        [InlineData("i", false)]
        [InlineData("m", false)]
        [InlineData("o", false)]
        [InlineData("e", true)]
        [InlineData("*[]", false)]
        [InlineData("![]", false)]
        [InlineData("*[A:n, B:s]", false)]
        [InlineData("![A:n, B:s]", false)]
        [InlineData("*[X:e]", true)]
        [InlineData("![X:e]", true)]
        [InlineData("*[X:n, Y:e]", true)]
        [InlineData("*[X:n, Y:![Z:e]]", true)]
        [InlineData("*[X:n, Y:![Z:b, W:c]]", false)]
        [InlineData("*[X:n, Y:![Z:b, W:e]]", true)]
        [InlineData("*[X:n, Y:![Z:b, W:*[A:*[B:![C:*[D:![E:n, F:s, G:b]]]]]]]", false)]
        [InlineData("*[X:n, Y:![Z:b, W:*[A:*[B:![C:*[D:![E:n, F:e, G:b]]]]]]]", true)]
        [InlineData("*[X:*[A:*[], B:![X:n, Y:b], C:*[D:![E:e], E:*[F:n]]], Y:![Z:b, W:*[A:*[B:![C:*[D:![E:n, F:e, G:b]]]]]]]", true)]
        [InlineData("*[X:*[A:*[], B:![X:n, Y:b], C:*[D:![E:w], E:*[F:n]]], Y:![Z:b, W:*[A:*[B:![C:*[D:![E:n, F:w, G:b]]]]]]]", false)]
        public void TestDTypeHasErrors(string typeAsString, bool hasErrors)
        {
            Assert.Equal(hasErrors, TestUtils.DT(typeAsString).HasErrors);
        }

        [Theory]
        [InlineData("s", "s", "s")]
        [InlineData("n", "n", "n")]
        [InlineData("s", "n", "e")]
        [InlineData("![]", "![A:s]", "![]")]
        [InlineData("![A:s]", "![A:s]", "![A:s]")]
        [InlineData("![A:s, B:n, C:w]", "![A:s, B:n, C:w]", "![A:s, B:n, C:w]")]
        [InlineData("![A:n, B:s, C:i]", "![A:s, B:n, C:w]", "![]")]
        [InlineData("![A:s, B:s, C:i]", "![A:s, B:n, C:w]", "![A:s]")]
        [InlineData("*[A:s, B:s, C:i]", "*[A:s, B:n, C:w]", "*[A:s]")]
        [InlineData("*[A:s, B:s, C:i]", "![A:s, B:n, C:w]", "e")]
        [InlineData("*[X:n, Y:![Z:b, W:*[A:*[B:![C:*[D:![E:n, F:s, G:b]]]]]]]", "*[X:n, Y:![Z:b, W:*[A:*[B:![C:*[D:![E:n, F:n, G:b]]]]]]]", "*[X:n, Y:![Z:b, W:*[A:*[B:![C:*[D:![E:n, G:b]]]]]]]")]
        [InlineData("*[X:n, Y:![Z:b, W:*[A:*[B:![C:*[D:![E:n, F:s, G:b]]]]]]]", "*[X:n, Y:![Z:b, W:*[A:*[B:![C:*[D:![E:n, F:s, G:b]]]]]]]", "*[X:n, Y:![Z:b, W:*[A:*[B:![C:*[D:![E:n, F:s, G:b]]]]]]]")]
        [InlineData("*[X:n, Y:![Z:b, W:*[A:*[B:![M:n, C:*[D:![E:n, F:s, G:b]]]]]]]", "*[X:n, Y:![Z:b, W:*[A:*[B:![C:*[D:![E:n, F:s, G:b]]]]]]]", "*[X:n, Y:![Z:b, W:*[A:*[B:![C:*[D:![E:n, F:s, G:b]]]]]]]")]
        [InlineData("*[X:s, Y:![Z:b, W:*[A:*[B:![M:n, C:*[D:![E:n, F:s, G:b]]]]]]]", "*[X:n, Y:![Z:b, W:*[A:*[B:![C:*[D:![E:n, F:s, G:b]]]]]]]", "*[Y:![Z:b, W:*[A:*[B:![C:*[D:![E:n, F:s, G:b]]]]]]]")]
        public void TestIntersectionTypes(string type1String, string type2String, string resultString)
        {
            DType type1 = TestUtils.DT(type1String);
            Assert.True(type1.IsValid);
            DType type2 = TestUtils.DT(type2String);
            Assert.True(type2.IsValid);
            DType result = TestUtils.DT(resultString);
            Assert.True(result.IsValid);
            Assert.Equal(result, DType.Intersection(type1, type2));
        }

        [Fact]
        public void TestMultiSelectOptionSet()
        {
            Assert.True(MultiSelectOptionSetType.IsMultiSelectOptionSet());
            Assert.False(OptionSetType.IsMultiSelectOptionSet());
            Assert.False(DType.EmptyTable.IsMultiSelectOptionSet());
            Assert.False(DType.CreateTable(new TypedName(OptionSetType, new DName("Value")), new TypedName(DType.String, new DName("Name"))).IsMultiSelectOptionSet());
        }

        [Theory]
        [InlineData("*[hello:n, hellos:b]", "hello", "hello")]
        [InlineData("*[hello:n, hellos:b]", "he", "hello")]
        [InlineData("*[hello:n, hellos:b]", "hellose", "hellos")]
        [InlineData("*[HELLO:n, hellos:b]", "hello", "HELLO")]
        [InlineData("*[completely:n, different:b]", "differential", "different")]
        public void TestDTypeSimilarName(string typeString, string testName, string expected)
        {
            DType type = TestUtils.DT(typeString);
            type.TryGetSimilarName(new DName(testName), FieldNameKind.Display, out var similar);
            Assert.Equal(expected, similar);
        }

        [Theory]
        [InlineData("n", false)]
        [InlineData("*[]", false)]
        [InlineData("![]", false)]
        [InlineData("*[A:n, B:s]", false)]
        [InlineData("![A:n, B:s]", false)]
        [InlineData("*[X:O]", true)]
        [InlineData("![X:O]", true)]
        [InlineData("*[X:n, Y:O]", true)]
        [InlineData("*[X:n, Y:![Z:O]]", true)]
        [InlineData("*[X:n, Y:![Z:b, W:c]]", false)]
        [InlineData("*[X:n, Y:![Z:b, W:O]]", true)]
        [InlineData("*[X:n, Y:![Z:b, W:*[A:*[B:![C:*[D:![E:n, F:s, G:b]]]]]]]", false)]
        [InlineData("*[X:n, Y:![Z:b, W:*[A:*[B:![C:*[D:![E:n, F:O, G:b]]]]]]]", true)]
        [InlineData("*[X:*[A:*[], B:![X:n, Y:b], C:*[D:![E:O], E:*[F:n]]], Y:![Z:b, W:*[A:*[B:![C:*[D:![E:n, F:O, G:b]]]]]]]", true)]
        [InlineData("*[X:*[A:*[], B:![X:n, Y:b], C:*[D:![E:w], E:*[F:n]]], Y:![Z:b, W:*[A:*[B:![C:*[D:![E:n, F:w, G:b]]]]]]]", false)]
        public void TestDTypeContainsUO(string typeAsString, bool containsUO)
        {
            Assert.Equal(containsUO, TestUtils.DT(typeAsString).ContainsKindNested(DPath.Root, DKind.UntypedObject));
        }
    }
}
