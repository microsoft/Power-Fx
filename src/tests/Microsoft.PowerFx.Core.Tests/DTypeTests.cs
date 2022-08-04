// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    public class DTypeTests
    {
        private DType AttachmentType => DType.CreateAttachmentType(DType.CreateTable(new TypedName(DType.String, new DName("DisplayName"))));

        private IExternalEntity _optionSet;

        private DType OptionSetType
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

        private DType OptionSetValueType => DType.CreateOptionSetValueType(OptionSetType.OptionSetInfo);

        private DType MultiSelectOptionSetType
        {
            get
            {
                var optionSetColumn = new TypedName(DType.OptionSetValue, new DName("Value"));
                return DType.CreateTable(optionSetColumn);
            }
        }

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
            Assert.Equal("A", AttachmentType.ToString());
            Assert.Equal("T", DType.Time.ToString());
            Assert.Equal("D", DType.Date.ToString());
            Assert.Equal("N", DType.ObjNull.ToString());
            Assert.Equal("P", DType.Polymorphic.ToString());
            Assert.Equal("V", DType.NamedValue.ToString());
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
            Assert.Equal(DKind.DateTime, DType.DateTime.Kind);
            Assert.Equal(DKind.Record, DType.EmptyRecord.Kind);
            Assert.Equal(DKind.Table, DType.EmptyTable.Kind);
            Assert.Equal(DKind.Enum, DType.EmptyEnum.Kind);
            Assert.Equal(DKind.LazyTable, AttachmentType.Kind);
            Assert.Equal(DKind.OptionSet, OptionSetType.Kind);
            Assert.Equal(DKind.Table, MultiSelectOptionSetType.Kind);
            Assert.Equal(DKind.Date, DType.Date.Kind);
            Assert.Equal(DKind.Time, DType.Time.Kind);
            Assert.Equal(DKind.Polymorphic, DType.Polymorphic.Kind);
            Assert.Equal(DKind.NamedValue, DType.NamedValue.Kind);
        }

        [Fact]
        public void ErrorIsSupertypeOfAll()
        {
            Assert.True(DType.Error.Accepts(DType.Unknown));
            Assert.True(DType.Error.Accepts(DType.Error));
            Assert.True(DType.Error.Accepts(DType.Boolean));
            Assert.True(DType.Error.Accepts(DType.DateTime));
            Assert.True(DType.Error.Accepts(DType.EmptyRecord));
            Assert.True(DType.Error.Accepts(DType.EmptyTable));
            Assert.True(DType.Error.Accepts(DType.Hyperlink));
            Assert.True(DType.Error.Accepts(DType.Image));
            Assert.True(DType.Error.Accepts(DType.PenImage));
            Assert.True(DType.Error.Accepts(DType.Media));
            Assert.True(DType.Error.Accepts(DType.Blob));
            Assert.True(DType.Error.Accepts(DType.Color));
            Assert.True(DType.Error.Accepts(DType.Currency));
            Assert.True(DType.Error.Accepts(DType.Number));
            Assert.True(DType.Error.Accepts(DType.String));
            Assert.True(DType.Error.Accepts(DType.EmptyEnum));
            Assert.True(DType.Error.Accepts(DType.Date));
            Assert.True(DType.Error.Accepts(DType.Time));
            Assert.True(DType.Error.Accepts(DType.Guid));
            Assert.True(DType.Error.Accepts(AttachmentType));
            Assert.True(DType.Error.Accepts(OptionSetType));
            Assert.True(DType.Error.Accepts(MultiSelectOptionSetType));
            Assert.True(DType.Error.Accepts(DType.Polymorphic));
        }

        [Fact]
        public void UnknownIsSubtypeOfAll()
        {
            Assert.True(DType.Unknown.Accepts(DType.Unknown));
            Assert.True(DType.Error.Accepts(DType.Unknown));
            Assert.True(DType.Number.Accepts(DType.Unknown));
            Assert.True(DType.Boolean.Accepts(DType.Unknown));
            Assert.True(DType.String.Accepts(DType.Unknown));
            Assert.True(DType.Hyperlink.Accepts(DType.Unknown));
            Assert.True(DType.Image.Accepts(DType.Unknown));
            Assert.True(DType.PenImage.Accepts(DType.Unknown));
            Assert.True(DType.Media.Accepts(DType.Unknown));
            Assert.True(DType.Blob.Accepts(DType.Unknown));
            Assert.True(DType.Color.Accepts(DType.Unknown));
            Assert.True(DType.Currency.Accepts(DType.Unknown));
            Assert.True(DType.EmptyRecord.Accepts(DType.Unknown));
            Assert.True(DType.EmptyTable.Accepts(DType.Unknown));
            Assert.True(DType.EmptyEnum.Accepts(DType.Unknown));
            Assert.True(DType.Date.Accepts(DType.Unknown));
            Assert.True(DType.Time.Accepts(DType.Unknown));
            Assert.True(DType.Guid.Accepts(DType.Unknown));
            Assert.True(AttachmentType.Accepts(DType.Unknown));
            Assert.True(OptionSetType.Accepts(DType.Unknown));
            Assert.True(MultiSelectOptionSetType.Accepts(DType.Unknown));
            Assert.True(DType.Polymorphic.Accepts(DType.Unknown));
        }
        
        [Fact]
        public void AttachmentTypeAcceptanceTest()
        {
            var type1 = DType.CreateAttachmentType(DType.CreateAttachmentType(DType.CreateTable(new TypedName(DType.String, new DName("DisplayName")))));
            var type2 = DType.CreateAttachmentType(DType.CreateAttachmentType(DType.CreateTable(new TypedName(DType.String, new DName("DisplayName")))));

            Assert.True(type1.Accepts(type2));

            type1 = DType.CreateAttachmentType(DType.CreateAttachmentType(DType.CreateTable(new TypedName(DType.String, new DName("Name")))));
            Assert.False(type1.Accepts(type2));

            type2 = DType.CreateAttachmentType(DType.CreateAttachmentType(DType.CreateRecord(new TypedName(DType.String, new DName("DisplayName")))));
            Assert.False(type2.Accepts(type1));
        }

        [Fact]
        public void DTypeAcceptanceTest()
        {
            Assert.False(DType.Unknown.Accepts(DType.Number));

            Assert.True(DType.Number.Accepts(DType.Number));
            Assert.True(DType.Number.Accepts(DType.Currency));
            Assert.False(DType.Number.Accepts(DType.DateTime));
            Assert.False(DType.Number.Accepts(DType.Date));
            Assert.False(DType.Number.Accepts(DType.Time));
            Assert.True(DType.Number.Accepts(DType.EmptyEnum));
            Assert.True(DType.TryParse("%n[A:1, B:2]", out DType type) && type.IsEnum && DType.Number.Accepts(type));

            Assert.True(DType.Boolean.Accepts(DType.Boolean));
            Assert.True(DType.Boolean.Accepts(DType.EmptyEnum));
            Assert.True(DType.TryParse("%b[A:true, B:false]", out type) && type.IsEnum && DType.Boolean.Accepts(type));

            Assert.True(DType.String.Accepts(DType.String));
            Assert.True(DType.String.Accepts(DType.Hyperlink));
            Assert.True(DType.String.Accepts(DType.Guid));
            Assert.True(DType.String.Accepts(DType.Image));
            Assert.True(DType.String.Accepts(DType.PenImage));
            Assert.True(DType.String.Accepts(DType.Media));
            Assert.True(DType.String.Accepts(DType.Blob));
            Assert.True(DType.String.Accepts(DType.EmptyEnum));
            Assert.True(DType.TryParse("%s[A:\"a\", B:\"b\"]", out type) && type.IsEnum && DType.String.Accepts(type));

            Assert.True(DType.Hyperlink.Accepts(DType.Hyperlink));
            Assert.True(DType.Hyperlink.Accepts(DType.Image));
            Assert.True(DType.Hyperlink.Accepts(DType.Media));
            Assert.True(DType.Hyperlink.Accepts(DType.Blob));
            Assert.True(DType.Hyperlink.Accepts(DType.EmptyEnum));

            Assert.True(DType.Image.Accepts(DType.Image));
            Assert.True(DType.Image.Accepts(DType.EmptyEnum));

            Assert.True(DType.Media.Accepts(DType.Media));
            Assert.True(DType.Media.Accepts(DType.EmptyEnum));

            Assert.True(DType.Blob.Accepts(DType.Blob));
            Assert.True(DType.Blob.Accepts(DType.EmptyEnum));

            Assert.True(DType.Color.Accepts(DType.Color));
            Assert.True(DType.Color.Accepts(DType.EmptyEnum));

            Assert.True(DType.Currency.Accepts(DType.Currency));
            Assert.True(DType.Currency.Accepts(DType.EmptyEnum));

            Assert.True(DType.DateTime.Accepts(DType.DateTime));
            Assert.True(DType.DateTime.Accepts(DType.Date));
            Assert.True(DType.DateTime.Accepts(DType.Time));
            Assert.True(DType.DateTime.Accepts(DType.EmptyEnum));

            Assert.True(DType.Date.Accepts(DType.Date));
            Assert.True(DType.Date.Accepts(DType.EmptyEnum));

            Assert.True(DType.Time.Accepts(DType.Time));
            Assert.True(DType.Time.Accepts(DType.EmptyEnum));
        }
        
        [Fact]
        public void TestDropAllOfKind()
        {
            DType type1 = TestUtils.DT("*[A:n, B:n, C:s]");

            var fError = false;
            var newType = type1.DropAllOfKind(ref fError, DPath.Root, DKind.Number);
            Assert.False(fError);
            Assert.Equal(TestUtils.DT("*[C:s]"), newType);

            newType = type1.DropAllOfKind(ref fError, DPath.Root, DKind.Control);
            Assert.True(fError);
            Assert.Equal(TestUtils.DT("*[A:n, B:n, C:s]"), newType);

            fError = false;
            newType = DType.Number.DropAllOfKind(ref fError, DPath.Root, DKind.Number);
            Assert.True(fError);
            Assert.Equal(TestUtils.DT("n"), newType);

            DType type5 = type1.Add(new DName("Attachments"), AttachmentType);
            fError = false;
            newType = type1.DropAllMatching(ref fError, DPath.Root, type => type.IsAttachment);
            Assert.Equal(TestUtils.DT("*[A:n, B:n, C:s]"), newType);

            DType type6 = type1.Add(new DName("Polymorphic"), DType.Polymorphic);
            fError = false;
            newType = type1.DropAllOfKind(ref fError, DPath.Root, DKind.Polymorphic);
            Assert.Equal(TestUtils.DT("*[A:n, B:n, C:s]"), newType);
        }

        [Fact]
        public void DTypeAcceptanceTest_Negative()
        {
            Assert.False(DType.Number.Accepts(DType.String));
            Assert.False(DType.Number.Accepts(DType.Hyperlink));
            Assert.False(DType.Number.Accepts(DType.Guid));
            Assert.False(DType.Number.Accepts(DType.Image));
            Assert.False(DType.Number.Accepts(DType.Media));
            Assert.False(DType.Number.Accepts(DType.Blob));
            Assert.False(DType.Number.Accepts(DType.Boolean));
            Assert.False(DType.Number.Accepts(DType.EmptyTable));
            Assert.False(DType.Number.Accepts(DType.EmptyRecord));

            Assert.False(DType.Boolean.Accepts(DType.String));
            Assert.False(DType.Boolean.Accepts(DType.Number));
            Assert.False(DType.Boolean.Accepts(DType.Color));
            Assert.False(DType.Boolean.Accepts(DType.Currency));
            Assert.False(DType.Boolean.Accepts(DType.DateTime));
            Assert.False(DType.Boolean.Accepts(DType.Date));
            Assert.False(DType.Boolean.Accepts(DType.Time));
            Assert.False(DType.Boolean.Accepts(DType.Media));
            Assert.False(DType.Boolean.Accepts(DType.Blob));
            Assert.False(DType.Boolean.Accepts(DType.Hyperlink));
            Assert.False(DType.Boolean.Accepts(DType.Image));
            Assert.False(DType.Boolean.Accepts(DType.EmptyTable));
            Assert.False(DType.Boolean.Accepts(DType.EmptyRecord));
            Assert.False(DType.Boolean.Accepts(DType.Guid));

            Assert.False(DType.String.Accepts(DType.Number));
            Assert.False(DType.String.Accepts(DType.Color));
            Assert.False(DType.String.Accepts(DType.Currency));
            Assert.False(DType.String.Accepts(DType.DateTime));
            Assert.False(DType.String.Accepts(DType.Date));
            Assert.False(DType.String.Accepts(DType.Time));
            Assert.False(DType.String.Accepts(DType.Boolean));
            Assert.False(DType.String.Accepts(DType.EmptyRecord));
            Assert.False(DType.String.Accepts(DType.EmptyTable));

            Assert.False(DType.Image.Accepts(DType.Boolean));
            Assert.False(DType.Image.Accepts(DType.Number));
            Assert.False(DType.Image.Accepts(DType.String));
            Assert.False(DType.Image.Accepts(DType.DateTime));
            Assert.False(DType.Image.Accepts(DType.Date));
            Assert.False(DType.Image.Accepts(DType.Time));
            Assert.False(DType.Image.Accepts(DType.Hyperlink));
            Assert.False(DType.Image.Accepts(DType.Currency));
            Assert.False(DType.Image.Accepts(DType.Media));
            Assert.False(DType.Image.Accepts(DType.Color));
            Assert.False(DType.Image.Accepts(DType.EmptyRecord));
            Assert.False(DType.Image.Accepts(DType.EmptyTable));
            Assert.False(DType.Image.Accepts(DType.Guid));

            Assert.False(DType.PenImage.Accepts(DType.Boolean));
            Assert.False(DType.PenImage.Accepts(DType.Number));
            Assert.False(DType.PenImage.Accepts(DType.String));
            Assert.False(DType.PenImage.Accepts(DType.Image));
            Assert.False(DType.PenImage.Accepts(DType.DateTime));
            Assert.False(DType.PenImage.Accepts(DType.Date));
            Assert.False(DType.PenImage.Accepts(DType.Time));
            Assert.False(DType.PenImage.Accepts(DType.Hyperlink));
            Assert.False(DType.PenImage.Accepts(DType.Currency));
            Assert.False(DType.PenImage.Accepts(DType.Media));
            Assert.False(DType.PenImage.Accepts(DType.Blob));
            Assert.False(DType.PenImage.Accepts(DType.Color));
            Assert.False(DType.PenImage.Accepts(DType.EmptyRecord));
            Assert.False(DType.PenImage.Accepts(DType.EmptyTable));
            Assert.False(DType.PenImage.Accepts(DType.Guid));

            Assert.False(DType.Media.Accepts(DType.Boolean));
            Assert.False(DType.Media.Accepts(DType.Number));
            Assert.False(DType.Media.Accepts(DType.String));
            Assert.False(DType.Media.Accepts(DType.DateTime));
            Assert.False(DType.Media.Accepts(DType.Date));
            Assert.False(DType.Media.Accepts(DType.Time));
            Assert.False(DType.Media.Accepts(DType.Image));
            Assert.False(DType.Media.Accepts(DType.Hyperlink));
            Assert.False(DType.Media.Accepts(DType.Currency));
            Assert.False(DType.Media.Accepts(DType.Color));
            Assert.False(DType.Media.Accepts(DType.EmptyRecord));
            Assert.False(DType.Media.Accepts(DType.EmptyTable));
            Assert.False(DType.Media.Accepts(DType.Guid));

            Assert.False(DType.Blob.Accepts(DType.Boolean));
            Assert.False(DType.Blob.Accepts(DType.Number));
            Assert.False(DType.Blob.Accepts(DType.String));
            Assert.False(DType.Blob.Accepts(DType.DateTime));
            Assert.False(DType.Blob.Accepts(DType.Date));
            Assert.False(DType.Blob.Accepts(DType.Time));
            Assert.False(DType.Blob.Accepts(DType.Image));
            Assert.False(DType.Blob.Accepts(DType.Hyperlink));
            Assert.False(DType.Blob.Accepts(DType.Currency));
            Assert.False(DType.Blob.Accepts(DType.Color));
            Assert.False(DType.Blob.Accepts(DType.EmptyRecord));
            Assert.False(DType.Blob.Accepts(DType.EmptyTable));
            Assert.False(DType.Blob.Accepts(DType.Guid));

            Assert.False(DType.Hyperlink.Accepts(DType.Boolean));
            Assert.False(DType.Hyperlink.Accepts(DType.Number));
            Assert.False(DType.Hyperlink.Accepts(DType.String));
            Assert.False(DType.Hyperlink.Accepts(DType.DateTime));
            Assert.False(DType.Hyperlink.Accepts(DType.Date));
            Assert.False(DType.Hyperlink.Accepts(DType.Time));
            Assert.False(DType.Hyperlink.Accepts(DType.Currency));
            Assert.False(DType.Hyperlink.Accepts(DType.Color));
            Assert.False(DType.Hyperlink.Accepts(DType.EmptyRecord));
            Assert.False(DType.Hyperlink.Accepts(DType.EmptyTable));
            Assert.False(DType.Hyperlink.Accepts(DType.Guid));

            Assert.False(DType.DateTime.Accepts(DType.Boolean));
            Assert.False(DType.DateTime.Accepts(DType.Number));
            Assert.False(DType.DateTime.Accepts(DType.String));
            Assert.False(DType.DateTime.Accepts(DType.Hyperlink));
            Assert.False(DType.DateTime.Accepts(DType.Image));
            Assert.False(DType.DateTime.Accepts(DType.Media));
            Assert.False(DType.DateTime.Accepts(DType.Blob));
            Assert.False(DType.DateTime.Accepts(DType.Currency));
            Assert.False(DType.DateTime.Accepts(DType.Color));
            Assert.False(DType.DateTime.Accepts(DType.EmptyRecord));
            Assert.False(DType.DateTime.Accepts(DType.EmptyTable));
            Assert.False(DType.DateTime.Accepts(DType.Guid));

            Assert.False(DType.Date.Accepts(DType.Boolean));
            Assert.False(DType.Date.Accepts(DType.Number));
            Assert.False(DType.Date.Accepts(DType.String));
            Assert.False(DType.Date.Accepts(DType.Hyperlink));
            Assert.False(DType.Date.Accepts(DType.Image));
            Assert.False(DType.Date.Accepts(DType.Media));
            Assert.False(DType.Date.Accepts(DType.Blob));
            Assert.False(DType.Date.Accepts(DType.Currency));
            Assert.False(DType.Date.Accepts(DType.Color));
            Assert.False(DType.Date.Accepts(DType.EmptyRecord));
            Assert.False(DType.Date.Accepts(DType.EmptyTable));
            Assert.False(DType.Date.Accepts(DType.DateTime));
            Assert.False(DType.Date.Accepts(DType.Time));
            Assert.False(DType.Date.Accepts(DType.Guid));

            Assert.False(DType.Time.Accepts(DType.Boolean));
            Assert.False(DType.Time.Accepts(DType.Number));
            Assert.False(DType.Time.Accepts(DType.String));
            Assert.False(DType.Time.Accepts(DType.Hyperlink));
            Assert.False(DType.Time.Accepts(DType.Image));
            Assert.False(DType.Time.Accepts(DType.Media));
            Assert.False(DType.Time.Accepts(DType.Blob));
            Assert.False(DType.Time.Accepts(DType.Currency));
            Assert.False(DType.Time.Accepts(DType.Color));
            Assert.False(DType.Time.Accepts(DType.EmptyRecord));
            Assert.False(DType.Time.Accepts(DType.EmptyTable));
            Assert.False(DType.Time.Accepts(DType.DateTime));
            Assert.False(DType.Time.Accepts(DType.Date));
            Assert.False(DType.Time.Accepts(DType.Guid));

            Assert.False(DType.Currency.Accepts(DType.Boolean));
            Assert.False(DType.Currency.Accepts(DType.Number));
            Assert.False(DType.Currency.Accepts(DType.String));
            Assert.False(DType.Currency.Accepts(DType.Hyperlink));
            Assert.False(DType.Currency.Accepts(DType.Image));
            Assert.False(DType.Currency.Accepts(DType.Media));
            Assert.False(DType.Currency.Accepts(DType.Blob));
            Assert.False(DType.Currency.Accepts(DType.DateTime));
            Assert.False(DType.Currency.Accepts(DType.Date));
            Assert.False(DType.Currency.Accepts(DType.Color));
            Assert.False(DType.Currency.Accepts(DType.EmptyRecord));
            Assert.False(DType.Currency.Accepts(DType.EmptyTable));
            Assert.False(DType.Currency.Accepts(DType.Guid));

            Assert.False(DType.Color.Accepts(DType.Boolean));
            Assert.False(DType.Color.Accepts(DType.Number));
            Assert.False(DType.Color.Accepts(DType.String));
            Assert.False(DType.Color.Accepts(DType.Hyperlink));
            Assert.False(DType.Color.Accepts(DType.Image));
            Assert.False(DType.Color.Accepts(DType.Media));
            Assert.False(DType.Color.Accepts(DType.Blob));
            Assert.False(DType.Color.Accepts(DType.DateTime));
            Assert.False(DType.Color.Accepts(DType.Date));
            Assert.False(DType.Color.Accepts(DType.Currency));
            Assert.False(DType.Color.Accepts(DType.EmptyRecord));
            Assert.False(DType.Color.Accepts(DType.EmptyTable));
            Assert.False(DType.Color.Accepts(DType.Guid));

            Assert.False(DType.EmptyRecord.Accepts(AttachmentType));
            Assert.False(AttachmentType.Accepts(DType.EmptyRecord));

            Assert.False(DType.EmptyTable.Accepts(AttachmentType));
            Assert.False(AttachmentType.Accepts(DType.EmptyTable));
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
            Assert.True(DType.Guid.ChildCount == 0);
            Assert.True(DType.Polymorphic.ChildCount == 0);
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
            Assert.False(DType.Image.IsAggregate);
            Assert.False(DType.PenImage.IsAggregate);
            Assert.False(DType.Media.IsAggregate);
            Assert.False(DType.Blob.IsAggregate);
            Assert.False(DType.Color.IsAggregate);
            Assert.False(DType.Guid.IsAggregate);
            Assert.False(DType.Polymorphic.IsAggregate);
            Assert.False(AttachmentType.IsAggregate);

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
            Assert.False(AttachmentType.IsPrimitive);

            Assert.False(DType.EmptyRecord.IsPrimitive);
            Assert.False(DType.EmptyTable.IsPrimitive);
        }
        
        [Fact]
        public void AttachmentdataDTypes()
        {
            // Attachment types are neither aggregate nor primitive
            Assert.False(AttachmentType.IsPrimitive);
            Assert.False(AttachmentType.IsAggregate);

            Assert.True(AttachmentType.IsAttachment);
            Assert.NotNull(AttachmentType.AttachmentType);
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

            var type2 = DType.CreateTable(
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
            Assert.True(type1.Accepts(type2));
            Assert.True(type2.Accepts(type1));

            type2 = DType.CreateRecord(type1.GetNames(DPath.Root));
            Assert.False(type1 == type2);
            Assert.True(type1 != type2);
            Assert.False(type1.Equals(type2));
            Assert.Equal("![A:n, B:n, C:n]", type2.ToString());
            Assert.False(type1.Accepts(type2));
            Assert.False(type2.Accepts(type1));

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

            var type5 = DType.EmptyTable.Add(ref fError, DPath.Root,  new DName("D"), DType.String)
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

            Assert.False(type7.Accepts(type6));
            Assert.True(type6.Accepts(type7));

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

            Assert.False(type8.Accepts(type1));
            Assert.True(type1.Accepts(type8));

            Assert.False(DType.String.Accepts(DType.Number));

            // Accepts
            Assert.True(
                !DType.EmptyRecord.Add(ref fError, DPath.Root, new DName("A"), DType.Number)
                    .Accepts(DType.EmptyRecord.Add(ref fError, DPath.Root, new DName("A"), DType.Error)) &&
                !fError);
            Assert.True(
                DType.EmptyRecord.Add(ref fError, DPath.Root, new DName("A"), DType.Number)
                    .Accepts(DType.EmptyRecord.Add(ref fError, DPath.Root, new DName("A"), DType.ObjNull)) &&
                !fError);

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
            Assert.True(DType.TryParse("%n[A:0, B:1, C:2, D:3]", out DType type) && type.IsEnum);
            Assert.True(DType.TryParse("%n[A:0, B:1, C:2, D:3]", out DType type2) && type2.IsEnum);

            Assert.True(type == type2);
            Assert.True(type.Accepts(type2));
            Assert.True(type2.Accepts(type));
            Assert.False(type.Accepts(DType.Number));
            Assert.True(DType.Number.Accepts(type));

            Assert.True(DType.TryParse("%n[A:0]", out type2) && type2.IsEnum);
            Assert.False(type == type2);
            Assert.True(type.Accepts(type2)); // The enum type with more values accepts an enum value from the type with less values.
            Assert.False(type2.Accepts(type)); // The enum type with less values does not accept values from the larger enum.
            Assert.False(type2.Accepts(DType.Number));
            Assert.True(DType.Number.Accepts(type2));

            Assert.True(DType.TryParse("%s[A:\"letter\"]", out type2) && type2.IsEnum);
            Assert.False(type == type2);
            Assert.False(type.Accepts(type2));
            Assert.False(type2.Accepts(type));
            Assert.False(type2.Accepts(DType.String));
            Assert.True(DType.String.Accepts(type2));

            Assert.True(DType.TryParse("%b[A:true, B:false]", out type2) && type2.IsEnum);
            Assert.False(type == type2);
            Assert.False(type.Accepts(type2));
            Assert.False(type2.Accepts(type));
            Assert.False(type2.Accepts(DType.Boolean));
            Assert.True(DType.Boolean.Accepts(type2));

            Assert.True(DType.TryParse("%n[A:12345, B:1, C:2, D:3]", out type2) && type2.IsEnum);
            Assert.False(type == type2);
            Assert.False(type.Accepts(type2));
            Assert.False(type2.Accepts(type));
            Assert.False(type2.Accepts(DType.Number));
            Assert.True(DType.Number.Accepts(type2));

            Assert.True(DType.TryParse("%s['Segoe UI':\"segoe ui\", 'bah humbug':\"bah and then humbug\"]", out type2) && type2.IsEnum);
            Assert.True(DType.String.Accepts(type2));
            Assert.Equal("%s['Segoe UI':\"segoe ui\", 'bah humbug':\"bah and then humbug\"]", type2.ToString());
        }
        
        [Fact]
        public void TestDefaultSchemaDifference()
        {
            var left = DType.CreateEnum(DType.ObjNull, Enumerable.Empty<KeyValuePair<DName, object>>());
            var right = DType.CreateEnum(DType.Number, Enumerable.Empty<KeyValuePair<DName, object>>());

            // Test a failing path
            Assert.False(left.Accepts(right, out KeyValuePair<string, DType> testSchemaDifference, out DType typeSchemaDifferenceType));
            Assert.Equal(testSchemaDifference.Value, DType.Invalid);

            // Test the TreeAccepts path
            left = DType.CreateRecord(Enumerable.Empty<TypedName>());
            Assert.True(left.Accepts(left, out testSchemaDifference, out typeSchemaDifferenceType));
            Assert.Equal(testSchemaDifference.Value, DType.Invalid);

            // Test the most immediate path
            right = DType.ObjNull;
            Assert.True(left.Accepts(right, out testSchemaDifference, out typeSchemaDifferenceType));
            Assert.Equal(testSchemaDifference.Value, DType.Invalid);
        }
                
        [Fact]
        public void TestDTypeSupertype()
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
            var superType = DType.Supertype(type3, type4);
            Assert.Equal("*[A:n, B:s, C:*[D:n]]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);
            superType = DType.Supertype(type4, type3);
            Assert.Equal("*[A:n, B:s, C:*[D:n]]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);

            // Output should be *[A:n,B:s,D:n]
            superType = DType.Supertype(type1, type2);
            Assert.Equal("*[A:n, B:s, D:n]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);
            superType = DType.Supertype(type2, type1);
            Assert.Equal("*[A:n, B:s, D:n]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);

            // Table with null value
            // Output should be *[A:n,B:s,C:b,D:n]
            superType = DType.Supertype(type1, type2s);
            Assert.Equal("*[A:n, B:s, C:b, D:n]", superType.ToString());
            Assert.Equal(4, superType.ChildCount);
            superType = DType.Supertype(type2s, type1);
            Assert.Equal("*[A:n, B:s, C:b, D:n]", superType.ToString());
            Assert.Equal(4, superType.ChildCount);

            // Table with null value
            // Output should be *[A:n,B:s,C:*[D:n,F:d]]
            superType = DType.Supertype(type3, type4s);
            Assert.Equal("*[A:n, B:s, C:*[D:n, F:d]]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);
            superType = DType.Supertype(type4s, type3);
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

            superType = DType.Supertype(type5, type6);
            Assert.Equal("![A:n, B:s, D:n]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);
            superType = DType.Supertype(type6, type5);
            Assert.Equal("![A:n, B:s, D:n]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);

            // Record with null value
            superType = DType.Supertype(type5, type6s);
            Assert.Equal("![A:n, B:s, C:b, D:n]", superType.ToString());
            Assert.Equal(4, superType.ChildCount);
            superType = DType.Supertype(type6s, type5);
            Assert.Equal("![A:n, B:s, C:b, D:n]", superType.ToString());
            Assert.Equal(4, superType.ChildCount);

            superType = DType.Supertype(type7, type8);
            Assert.Equal("![A:n, B:s, C:![D:n]]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);
            superType = DType.Supertype(type8, type7);
            Assert.Equal("![A:n, B:s, C:![D:n]]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);

            // Record with null value
            superType = DType.Supertype(type7, type8s);
            Assert.Equal("![A:n, B:s, C:![D:n, F:d]]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);
            superType = DType.Supertype(type8s, type7);
            Assert.Equal("![A:n, B:s, C:![D:n, F:d]]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);

            superType = DType.Supertype(DType.Number, DType.Number);
            Assert.Equal(0, superType.ChildCount);

            superType = DType.Supertype(DType.Number, DType.String);
            Assert.Equal(DKind.Error, superType.Kind);

            superType = DType.Supertype(DType.Unknown, DType.String);
            Assert.Equal(DKind.String, superType.Kind);

            superType = DType.Supertype(DType.String, DType.Unknown);
            Assert.Equal(DKind.String, superType.Kind);

            superType = DType.Supertype(DType.Unknown, DType.Unknown);
            Assert.Equal(DKind.Unknown, superType.Kind);

            superType = DType.Supertype(DType.Date, DType.Time);
            Assert.Equal(DKind.DateTime, superType.Kind);

            superType = DType.Supertype(DType.DateTime, DType.Time);
            Assert.Equal(DKind.DateTime, superType.Kind);

            superType = DType.Supertype(DType.DateTime, DType.Date);
            Assert.Equal(DKind.DateTime, superType.Kind);

            superType = DType.Supertype(DType.Time, DType.DateTime);
            Assert.Equal(DKind.DateTime, superType.Kind);

            superType = DType.Supertype(DType.Date, DType.DateTime);
            Assert.Equal(DKind.DateTime, superType.Kind);

            superType = DType.Supertype(DType.Image, DType.PenImage);
            Assert.Equal(DKind.Image, superType.Kind);

            superType = DType.Supertype(DType.PenImage, DType.Image);
            Assert.Equal(DKind.Image, superType.Kind);

            superType = DType.Supertype(DType.Image, DType.Media);
            Assert.Equal(DKind.Hyperlink, superType.Kind);

            superType = DType.Supertype(DType.Media, DType.Image);
            Assert.Equal(DKind.Hyperlink, superType.Kind);

            superType = DType.Supertype(DType.Image, DType.Blob);
            Assert.Equal(DKind.Image, superType.Kind);

            superType = DType.Supertype(DType.Blob, DType.Image);
            Assert.Equal(DKind.Image, superType.Kind);

            superType = DType.Supertype(DType.Blob, DType.Media);
            Assert.Equal(DKind.Media, superType.Kind);

            superType = DType.Supertype(DType.Media, DType.Blob);
            Assert.Equal(DKind.Media, superType.Kind);

            superType = DType.Supertype(DType.Image, DType.Hyperlink);
            Assert.Equal(DKind.Hyperlink, superType.Kind);

            superType = DType.Supertype(DType.Media, DType.Hyperlink);
            Assert.Equal(DKind.Hyperlink, superType.Kind);

            superType = DType.Supertype(DType.Blob, DType.Hyperlink);
            Assert.Equal(DKind.Hyperlink, superType.Kind);

            superType = DType.Supertype(DType.Hyperlink, DType.Image);
            Assert.Equal(DKind.Hyperlink, superType.Kind);

            superType = DType.Supertype(DType.Hyperlink, DType.Media);
            Assert.Equal(DKind.Hyperlink, superType.Kind);

            superType = DType.Supertype(DType.Hyperlink, DType.Blob);
            Assert.Equal(DKind.Hyperlink, superType.Kind);

            superType = DType.Supertype(DType.Currency, DType.DateTime);
            Assert.Equal(DKind.Error, superType.Kind);

            superType = DType.Supertype(DType.Currency, DType.Date);
            Assert.Equal(DKind.Error, superType.Kind);

            superType = DType.Supertype(DType.Currency, DType.Time);
            Assert.Equal(DKind.Error, superType.Kind);

            superType = DType.Supertype(DType.DateTime, DType.Currency);
            Assert.Equal(DKind.Error, superType.Kind);

            superType = DType.Supertype(DType.Date, DType.Currency);
            Assert.Equal(DKind.Error, superType.Kind);

            superType = DType.Supertype(DType.Time, DType.Currency);
            Assert.Equal(DKind.Error, superType.Kind);

            superType = DType.Supertype(DType.Guid, DType.String);
            Assert.Equal(DKind.String, superType.Kind);

            superType = DType.Supertype(DType.Guid, DType.Number);
            Assert.Equal(DKind.Error, superType.Kind);

            // ObjNull is compatable with every DType except for Error
            superType = DType.Supertype(DType.Number, DType.ObjNull);
            Assert.Equal(DKind.Number, superType.Kind);

            superType = DType.Supertype(DType.String, DType.ObjNull);
            Assert.Equal(DKind.String, superType.Kind);

            superType = DType.Supertype(DType.Date, DType.ObjNull);
            Assert.Equal(DKind.Date, superType.Kind);

            superType = DType.Supertype(DType.Time, DType.ObjNull);
            Assert.Equal(DKind.Time, superType.Kind);

            superType = DType.Supertype(DType.DateTime, DType.ObjNull);
            Assert.Equal(DKind.DateTime, superType.Kind);

            superType = DType.Supertype(DType.Image, DType.ObjNull);
            Assert.Equal(DKind.Image, superType.Kind);

            superType = DType.Supertype(DType.PenImage, DType.ObjNull);
            Assert.Equal(DKind.PenImage, superType.Kind);

            superType = DType.Supertype(DType.Media, DType.ObjNull);
            Assert.Equal(DKind.Media, superType.Kind);

            superType = DType.Supertype(DType.Blob, DType.ObjNull);
            Assert.Equal(DKind.Blob, superType.Kind);

            superType = DType.Supertype(DType.Hyperlink, DType.ObjNull);
            Assert.Equal(DKind.Hyperlink, superType.Kind);

            superType = DType.Supertype(DType.Currency, DType.ObjNull);
            Assert.Equal(DKind.Currency, superType.Kind);

            superType = DType.Supertype(DType.Unknown, DType.ObjNull);
            Assert.Equal(DKind.Unknown, superType.Kind);

            // ![A:t, B:s]
            var type9 = DType.CreateRecord(
                new TypedName(DType.Time, new DName("A")),
                new TypedName(DType.String, new DName("B")));

            // ![A:d, B:b]
            var type10 = DType.CreateRecord(
                new TypedName(DType.DateTime, new DName("A")),
                new TypedName(DType.Boolean, new DName("B")));

            superType = DType.Supertype(type9, type10);
            Assert.Equal(DKind.Record, superType.Kind);
            Assert.Equal(1, superType.ChildCount); // ![A:n]

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

            superType = DType.Supertype(type11, type12);
            Assert.Equal("![A:n, B:s, C:![]]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);
            superType = DType.Supertype(type12, type11);
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

            superType = DType.Supertype(type13, type14);
            Assert.Equal("![A:n, B:s, C:![F:s]]", superType.ToString());
            Assert.Equal(3, superType.ChildCount);
            superType = DType.Supertype(type14, type13);
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

            superType = DType.Supertype(type15, type16);
            Assert.Equal("![E:s]", superType.ToString());
            superType = DType.Supertype(type16, type15);
            Assert.Equal("![E:s]", superType.ToString());

            // supertype of a record with a table:
            superType = DType.Supertype(type4, type14);
            Assert.Equal(DKind.Error, superType.Kind);
        }
        
        [Fact]
        public void DTypeSpecParsing_SimpleTypes()
        {
            Assert.True(DType.TryParse(DType.Unknown.ToString(), out DType type) && type == DType.Unknown);
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

            // *[Num:n, Bool:b, Str:s, Date:d, Hyper:h, Img:i, Currency:$, Color:c, Unknown:?, Err:e, ONull:N]
            type2 = DType.CreateTable(
                new TypedName(DType.Number, new DName("Num")),
                new TypedName(DType.Boolean, new DName("Bool")),
                new TypedName(DType.String, new DName("Str")),
                new TypedName(DType.DateTime, new DName("Date")),
                new TypedName(DType.Hyperlink, new DName("Hyper")),
                new TypedName(DType.Image, new DName("Img")),
                new TypedName(DType.Currency, new DName("Currency")),
                new TypedName(DType.Color, new DName("Color")),
                new TypedName(DType.Unknown, new DName("Unknown")),
                new TypedName(DType.Error, new DName("Err")),
                new TypedName(DType.ObjNull, new DName("ONull")));
            Assert.True(DType.TryParse("*[Num:n, Bool:b, Str:s, Date:d, Hyper:h, Img:i, Currency:$, Color:c, Unknown:?, Err:e, ONull:N]", out type) && type == type2);

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
        
        [Fact]
        public void TestCoercesTo()
        {
            // Coercion to string
            Assert.True(DType.Guid.CoercesTo(DType.String));
            Assert.True(DType.Boolean.CoercesTo(DType.String));
            Assert.True(DType.Number.CoercesTo(DType.String));
            Assert.True(DType.Currency.CoercesTo(DType.String));
            Assert.False(DType.Color.CoercesTo(DType.String));
            Assert.True(DType.DateTime.CoercesTo(DType.String));
            Assert.True(DType.Date.CoercesTo(DType.String));
            Assert.True(DType.Time.CoercesTo(DType.String));
            Assert.True(DType.String.CoercesTo(DType.String));
            Assert.True(DType.Hyperlink.CoercesTo(DType.String));
            Assert.True(DType.Image.CoercesTo(DType.String));
            Assert.True(DType.PenImage.CoercesTo(DType.String));
            Assert.True(DType.Media.CoercesTo(DType.String));
            Assert.True(DType.Blob.CoercesTo(DType.String));
            Assert.False(DType.EmptyTable.CoercesTo(DType.String));
            Assert.False(DType.EmptyRecord.CoercesTo(DType.String));
            Assert.True(DType.EmptyEnum.CoercesTo(DType.String));
            Assert.False(DType.TryParse("%n[A:2]", out DType type) && type.CoercesTo(DType.String));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.String));
            Assert.True(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.String));
            Assert.True(DType.ObjNull.CoercesTo(DType.String));
            Assert.False(DType.Error.CoercesTo(DType.String));

            // Coercion to number
            Assert.True(DType.Boolean.CoercesTo(DType.Number));
            Assert.True(DType.Number.CoercesTo(DType.Number));
            Assert.True(DType.Currency.CoercesTo(DType.Number));
            Assert.False(DType.Color.CoercesTo(DType.Number));
            Assert.True(DType.DateTime.CoercesTo(DType.Number));
            Assert.True(DType.Date.CoercesTo(DType.Number));
            Assert.True(DType.Time.CoercesTo(DType.Number));
            Assert.True(DType.String.CoercesTo(DType.Number));
            Assert.False(DType.Guid.CoercesTo(DType.Number));
            Assert.False(DType.Hyperlink.CoercesTo(DType.Number));
            Assert.False(DType.Image.CoercesTo(DType.Number));
            Assert.False(DType.PenImage.CoercesTo(DType.Number));
            Assert.False(DType.Media.CoercesTo(DType.Number));
            Assert.False(DType.Blob.CoercesTo(DType.Number));
            Assert.False(DType.EmptyTable.CoercesTo(DType.Number));
            Assert.False(DType.EmptyRecord.CoercesTo(DType.Number));
            Assert.True(DType.EmptyEnum.CoercesTo(DType.Number));
            Assert.True(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.Number));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.Number));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.Number));
            Assert.True(DType.ObjNull.CoercesTo(DType.Number));
            Assert.False(DType.Error.CoercesTo(DType.Number));

            // Coercion to boolean
            Assert.True(DType.Boolean.CoercesTo(DType.Boolean));
            Assert.True(DType.Number.CoercesTo(DType.Boolean));
            Assert.True(DType.Currency.CoercesTo(DType.Boolean));
            Assert.False(DType.Color.CoercesTo(DType.Boolean));
            Assert.False(DType.DateTime.CoercesTo(DType.Boolean));
            Assert.False(DType.Date.CoercesTo(DType.Boolean));
            Assert.False(DType.Time.CoercesTo(DType.Boolean));
            Assert.True(DType.String.CoercesTo(DType.Boolean));
            Assert.False(DType.Hyperlink.CoercesTo(DType.Boolean));
            Assert.False(DType.Image.CoercesTo(DType.Boolean));
            Assert.False(DType.PenImage.CoercesTo(DType.Boolean));
            Assert.False(DType.Media.CoercesTo(DType.Boolean));
            Assert.False(DType.Blob.CoercesTo(DType.Boolean));
            Assert.False(DType.EmptyTable.CoercesTo(DType.Boolean));
            Assert.False(DType.EmptyRecord.CoercesTo(DType.Boolean));
            Assert.False(DType.Guid.CoercesTo(DType.Boolean));
            Assert.True(DType.EmptyEnum.CoercesTo(DType.Boolean));
            Assert.False(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.Boolean));
            Assert.True(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.Boolean));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.Boolean));
            Assert.True(DType.ObjNull.CoercesTo(DType.Boolean));
            Assert.False(DType.Error.CoercesTo(DType.Boolean));

            // Coercion to currency
            Assert.True(DType.Boolean.CoercesTo(DType.Currency));
            Assert.True(DType.Number.CoercesTo(DType.Currency));
            Assert.True(DType.Currency.CoercesTo(DType.Currency));
            Assert.False(DType.Color.CoercesTo(DType.Currency));
            Assert.False(DType.DateTime.CoercesTo(DType.Currency));
            Assert.False(DType.Date.CoercesTo(DType.Currency));
            Assert.False(DType.Time.CoercesTo(DType.Currency));
            Assert.True(DType.String.CoercesTo(DType.Currency));
            Assert.False(DType.Hyperlink.CoercesTo(DType.Currency));
            Assert.False(DType.Image.CoercesTo(DType.Currency));
            Assert.False(DType.PenImage.CoercesTo(DType.Currency));
            Assert.False(DType.Media.CoercesTo(DType.Currency));
            Assert.False(DType.Blob.CoercesTo(DType.Currency));
            Assert.False(DType.EmptyTable.CoercesTo(DType.Currency));
            Assert.False(DType.EmptyRecord.CoercesTo(DType.Currency));
            Assert.True(DType.EmptyEnum.CoercesTo(DType.Currency));
            Assert.False(DType.Guid.CoercesTo(DType.Currency));
            Assert.False(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.Currency));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.Currency));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.Currency));
            Assert.True(DType.TryParse("%$[A:2]", out type) && type.CoercesTo(DType.Currency));
            Assert.True(DType.ObjNull.CoercesTo(DType.Currency));
            Assert.False(DType.Error.CoercesTo(DType.Currency));

            // Coercion to color
            Assert.False(DType.Boolean.CoercesTo(DType.Color));
            Assert.False(DType.Number.CoercesTo(DType.Color));
            Assert.False(DType.Currency.CoercesTo(DType.Color));
            Assert.True(DType.Color.CoercesTo(DType.Color));
            Assert.False(DType.DateTime.CoercesTo(DType.Color));
            Assert.False(DType.Date.CoercesTo(DType.Color));
            Assert.False(DType.Time.CoercesTo(DType.Color));
            Assert.False(DType.String.CoercesTo(DType.Color));
            Assert.False(DType.Hyperlink.CoercesTo(DType.Color));
            Assert.False(DType.Image.CoercesTo(DType.Color));
            Assert.False(DType.PenImage.CoercesTo(DType.Color));
            Assert.False(DType.Media.CoercesTo(DType.Color));
            Assert.False(DType.Blob.CoercesTo(DType.Color));
            Assert.False(DType.EmptyTable.CoercesTo(DType.Color));
            Assert.False(DType.EmptyRecord.CoercesTo(DType.Color));
            Assert.False(DType.Guid.CoercesTo(DType.Color));
            Assert.True(DType.EmptyEnum.CoercesTo(DType.Color));
            Assert.True(DType.TryParse("%c[A:2]", out type) && type.CoercesTo(DType.Color));
            Assert.False(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.Color));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.Color));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.Color));
            Assert.True(DType.ObjNull.CoercesTo(DType.Color));
            Assert.False(DType.Error.CoercesTo(DType.Color));

            // Coercion to dateTime
            Assert.False(DType.Boolean.CoercesTo(DType.DateTime));
            Assert.True(DType.Number.CoercesTo(DType.DateTime));
            Assert.True(DType.Currency.CoercesTo(DType.DateTime));
            Assert.False(DType.Color.CoercesTo(DType.DateTime));
            Assert.True(DType.DateTime.CoercesTo(DType.DateTime));
            Assert.True(DType.Date.CoercesTo(DType.DateTime));
            Assert.True(DType.Time.CoercesTo(DType.DateTime));
            Assert.True(DType.String.CoercesTo(DType.DateTime));
            Assert.False(DType.Hyperlink.CoercesTo(DType.DateTime));
            Assert.False(DType.Image.CoercesTo(DType.DateTime));
            Assert.False(DType.PenImage.CoercesTo(DType.DateTime));
            Assert.False(DType.Media.CoercesTo(DType.DateTime));
            Assert.False(DType.Blob.CoercesTo(DType.DateTime));
            Assert.False(DType.Guid.CoercesTo(DType.DateTime));
            Assert.False(DType.EmptyTable.CoercesTo(DType.DateTime));
            Assert.False(DType.EmptyRecord.CoercesTo(DType.DateTime));
            Assert.True(DType.EmptyEnum.CoercesTo(DType.DateTime));
            Assert.True(DType.TryParse("%d[A:2]", out type) && type.CoercesTo(DType.DateTime));
            Assert.False(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.DateTime));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.DateTime));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.DateTime));
            Assert.True(DType.ObjNull.CoercesTo(DType.DateTime));
            Assert.False(DType.Error.CoercesTo(DType.DateTime));

            // Coercion to image
            Assert.False(DType.Boolean.CoercesTo(DType.Image));
            Assert.False(DType.Number.CoercesTo(DType.Image));
            Assert.False(DType.Currency.CoercesTo(DType.Image));
            Assert.False(DType.Color.CoercesTo(DType.Image));
            Assert.False(DType.DateTime.CoercesTo(DType.Image));
            Assert.False(DType.Date.CoercesTo(DType.Image));
            Assert.False(DType.Time.CoercesTo(DType.Image));
            Assert.True(DType.String.CoercesTo(DType.Image));
            Assert.True(DType.Hyperlink.CoercesTo(DType.Image));
            Assert.False(DType.Guid.CoercesTo(DType.Image));
            Assert.True(DType.Image.CoercesTo(DType.Image));
            Assert.True(DType.PenImage.CoercesTo(DType.Image));
            Assert.False(DType.Media.CoercesTo(DType.Image));
            Assert.True(DType.Blob.CoercesTo(DType.Image));
            Assert.False(DType.EmptyTable.CoercesTo(DType.Image));
            Assert.False(DType.EmptyRecord.CoercesTo(DType.Image));
            Assert.True(DType.EmptyEnum.CoercesTo(DType.Image));
            Assert.False(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.Image));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.Image));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.Image));
            Assert.True(DType.TryParse("%i[A:\"hello.jpg\"]", out type) && type.CoercesTo(DType.Image));
            Assert.True(DType.ObjNull.CoercesTo(DType.Image));
            Assert.False(DType.Error.CoercesTo(DType.Image));

            // Coercion to penimage
            Assert.False(DType.Boolean.CoercesTo(DType.PenImage));
            Assert.False(DType.Number.CoercesTo(DType.PenImage));
            Assert.False(DType.Currency.CoercesTo(DType.PenImage));
            Assert.False(DType.Color.CoercesTo(DType.PenImage));
            Assert.False(DType.DateTime.CoercesTo(DType.PenImage));
            Assert.False(DType.Date.CoercesTo(DType.PenImage));
            Assert.False(DType.Time.CoercesTo(DType.PenImage));
            Assert.False(DType.String.CoercesTo(DType.PenImage));
            Assert.False(DType.Hyperlink.CoercesTo(DType.PenImage));
            Assert.False(DType.Guid.CoercesTo(DType.PenImage));
            Assert.False(DType.Image.CoercesTo(DType.PenImage));
            Assert.True(DType.PenImage.CoercesTo(DType.PenImage));
            Assert.False(DType.Media.CoercesTo(DType.PenImage));
            Assert.False(DType.Blob.CoercesTo(DType.PenImage));
            Assert.False(DType.EmptyTable.CoercesTo(DType.PenImage));
            Assert.False(DType.EmptyRecord.CoercesTo(DType.PenImage));
            Assert.True(DType.EmptyEnum.CoercesTo(DType.PenImage));
            Assert.False(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.PenImage));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.PenImage));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.PenImage));
            Assert.True(DType.ObjNull.CoercesTo(DType.PenImage));
            Assert.False(DType.Error.CoercesTo(DType.PenImage));

            // Coercion to media
            Assert.False(DType.Boolean.CoercesTo(DType.Media));
            Assert.False(DType.Number.CoercesTo(DType.Media));
            Assert.False(DType.Currency.CoercesTo(DType.Media));
            Assert.False(DType.Color.CoercesTo(DType.Media));
            Assert.False(DType.DateTime.CoercesTo(DType.Media));
            Assert.False(DType.Date.CoercesTo(DType.Media));
            Assert.False(DType.Time.CoercesTo(DType.Media));
            Assert.True(DType.String.CoercesTo(DType.Media));
            Assert.True(DType.Hyperlink.CoercesTo(DType.Media));
            Assert.False(DType.Image.CoercesTo(DType.Media));
            Assert.False(DType.PenImage.CoercesTo(DType.Media));
            Assert.True(DType.Media.CoercesTo(DType.Media));
            Assert.True(DType.Blob.CoercesTo(DType.Media));
            Assert.False(DType.EmptyTable.CoercesTo(DType.Media));
            Assert.False(DType.EmptyRecord.CoercesTo(DType.Media));
            Assert.True(DType.EmptyEnum.CoercesTo(DType.Media));
            Assert.False(DType.Guid.CoercesTo(DType.Media));
            Assert.False(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.Media));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.Media));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.Media));
            Assert.True(DType.TryParse("%m[A:\"hello\"]", out type) && type.CoercesTo(DType.Media));
            Assert.True(DType.ObjNull.CoercesTo(DType.Media));
            Assert.False(DType.Error.CoercesTo(DType.Media));

            // Coercion to document
            Assert.False(DType.Boolean.CoercesTo(DType.Blob));
            Assert.False(DType.Number.CoercesTo(DType.Blob));
            Assert.False(DType.Currency.CoercesTo(DType.Blob));
            Assert.False(DType.Color.CoercesTo(DType.Blob));
            Assert.False(DType.DateTime.CoercesTo(DType.Blob));
            Assert.False(DType.Date.CoercesTo(DType.Blob));
            Assert.False(DType.Time.CoercesTo(DType.Blob));
            Assert.True(DType.String.CoercesTo(DType.Blob));
            Assert.True(DType.Hyperlink.CoercesTo(DType.Blob));
            Assert.True(DType.Image.CoercesTo(DType.Blob));
            Assert.True(DType.PenImage.CoercesTo(DType.Blob));
            Assert.True(DType.Media.CoercesTo(DType.Blob));
            Assert.True(DType.Blob.CoercesTo(DType.Blob));
            Assert.False(DType.EmptyTable.CoercesTo(DType.Blob));
            Assert.False(DType.EmptyRecord.CoercesTo(DType.Blob));
            Assert.False(DType.Guid.CoercesTo(DType.Blob));
            Assert.True(DType.EmptyEnum.CoercesTo(DType.Blob));
            Assert.False(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.Blob));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.Blob));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.Blob));
            Assert.True(DType.TryParse("%o[A:\"hello\"]", out type) && type.CoercesTo(DType.Blob));
            Assert.True(DType.ObjNull.CoercesTo(DType.Blob));
            Assert.False(DType.Error.CoercesTo(DType.Blob));

            // Coercion to hyperlink
            Assert.False(DType.Boolean.CoercesTo(DType.Hyperlink));
            Assert.False(DType.Number.CoercesTo(DType.Hyperlink));
            Assert.False(DType.Currency.CoercesTo(DType.Hyperlink));
            Assert.False(DType.Color.CoercesTo(DType.Hyperlink));
            Assert.False(DType.DateTime.CoercesTo(DType.Hyperlink));
            Assert.False(DType.Date.CoercesTo(DType.Hyperlink));
            Assert.False(DType.Time.CoercesTo(DType.Hyperlink));
            Assert.True(DType.String.CoercesTo(DType.Hyperlink));
            Assert.True(DType.Hyperlink.CoercesTo(DType.Hyperlink));
            Assert.True(DType.Image.CoercesTo(DType.Hyperlink));
            Assert.True(DType.PenImage.CoercesTo(DType.Hyperlink));
            Assert.True(DType.Media.CoercesTo(DType.Hyperlink));
            Assert.True(DType.Blob.CoercesTo(DType.Hyperlink));
            Assert.False(DType.EmptyTable.CoercesTo(DType.Hyperlink));
            Assert.False(DType.EmptyRecord.CoercesTo(DType.Hyperlink));
            Assert.True(DType.EmptyEnum.CoercesTo(DType.Hyperlink));
            Assert.False(DType.Guid.CoercesTo(DType.Hyperlink));
            Assert.False(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.Hyperlink));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.Hyperlink));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.Hyperlink));
            Assert.True(DType.TryParse("%h[A:\"hello\"]", out type) && type.CoercesTo(DType.Hyperlink));
            Assert.True(DType.ObjNull.CoercesTo(DType.Hyperlink));
            Assert.False(DType.Error.CoercesTo(DType.Hyperlink));

            // Coercion to table
            Assert.False(DType.Boolean.CoercesTo(DType.EmptyTable));
            Assert.False(DType.Number.CoercesTo(DType.EmptyTable));
            Assert.False(DType.Currency.CoercesTo(DType.EmptyTable));
            Assert.False(DType.Color.CoercesTo(DType.EmptyTable));
            Assert.False(DType.DateTime.CoercesTo(DType.EmptyTable));
            Assert.False(DType.Date.CoercesTo(DType.EmptyTable));
            Assert.False(DType.Time.CoercesTo(DType.EmptyTable));
            Assert.False(DType.String.CoercesTo(DType.EmptyTable));
            Assert.False(DType.Hyperlink.CoercesTo(DType.EmptyTable));
            Assert.False(DType.Guid.CoercesTo(DType.EmptyTable));
            Assert.False(DType.Image.CoercesTo(DType.EmptyTable));
            Assert.False(DType.PenImage.CoercesTo(DType.EmptyTable));
            Assert.False(DType.Media.CoercesTo(DType.EmptyTable));
            Assert.False(DType.Blob.CoercesTo(DType.EmptyTable));
            Assert.True(DType.EmptyTable.CoercesTo(DType.EmptyTable));
            Assert.True(DType.EmptyRecord.CoercesTo(DType.EmptyTable));
            Assert.True(DType.TryParse("![A:n]", out type) && type.CoercesTo(DType.EmptyTable));
            Assert.True(DType.TryParse("![A:n]", out DType type1) && DType.TryParse("*[A:n]", out DType type2) && type1.CoercesTo(type2));
            Assert.True(DType.TryParse("![A:n, B:s]", out type1) && DType.TryParse("*[A:n]", out type2) && type1.CoercesTo(type2));
            Assert.True(DType.TryParse("![A:*[B:s]]", out type1) && DType.TryParse("*[A:*[B:s]]", out type2) && type1.CoercesTo(type2));
            Assert.True(DType.TryParse("![A:*[B:![C:n]]]", out type1) && DType.TryParse("*[A:*[B:![C:n]]]", out type2) && type1.CoercesTo(type2));
            Assert.True(DType.TryParse("![A:*[B:s]]", out type1) && DType.TryParse("*[A:*[B:n]]", out type2) && type1.CoercesTo(type2));
            Assert.True(DType.TryParse("![A:*[B:![C:n]]]", out type1) && DType.TryParse("*[A:*[B:*[C:n]]]", out type2) && type1.CoercesTo(type2));
            Assert.True(DType.TryParse("![A:![B:s]]", out type1) && DType.TryParse("*[A:*[B:s]]", out type2) && type1.CoercesTo(type2));

            Assert.False(DType.TryParse("![A:*[B:s]]", out type1) && DType.TryParse("*[A:n]", out type2) && type1.CoercesTo(type2));
            Assert.False(DType.TryParse("![A:n]", out type1) && DType.TryParse("*[A:n, B:s]", out type2) && type1.CoercesTo(type2));
            Assert.False(DType.TryParse("![A:*[B:*[C:n]]]", out type1) && DType.TryParse("*[A:*[B:![C:n]]]", out type2) && type1.CoercesTo(type2));

            Assert.False(DType.EmptyEnum.CoercesTo(DType.EmptyTable));
            Assert.False(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.EmptyTable));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.EmptyTable));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.EmptyTable));
            Assert.True(DType.ObjNull.CoercesTo(DType.EmptyTable));
            Assert.False(DType.Error.CoercesTo(DType.EmptyTable));

            // Coercion to record
            Assert.False(DType.Boolean.CoercesTo(DType.EmptyRecord));
            Assert.False(DType.Number.CoercesTo(DType.EmptyRecord));
            Assert.False(DType.Currency.CoercesTo(DType.EmptyRecord));
            Assert.False(DType.Color.CoercesTo(DType.EmptyRecord));
            Assert.False(DType.DateTime.CoercesTo(DType.EmptyRecord));
            Assert.False(DType.Date.CoercesTo(DType.EmptyRecord));
            Assert.False(DType.Time.CoercesTo(DType.EmptyRecord));
            Assert.False(DType.String.CoercesTo(DType.EmptyRecord));
            Assert.False(DType.Hyperlink.CoercesTo(DType.EmptyRecord));
            Assert.False(DType.Image.CoercesTo(DType.EmptyRecord));
            Assert.False(DType.PenImage.CoercesTo(DType.EmptyRecord));
            Assert.False(DType.Media.CoercesTo(DType.EmptyRecord));
            Assert.False(DType.Guid.CoercesTo(DType.EmptyRecord));
            Assert.False(DType.Blob.CoercesTo(DType.EmptyRecord));
            Assert.False(DType.EmptyTable.CoercesTo(DType.EmptyRecord));
            Assert.True(DType.EmptyRecord.CoercesTo(DType.EmptyRecord));
            Assert.False(DType.EmptyEnum.CoercesTo(DType.EmptyRecord));
            Assert.False(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.EmptyRecord));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.EmptyRecord));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.EmptyRecord));
            Assert.True(DType.ObjNull.CoercesTo(DType.EmptyRecord));
            Assert.False(DType.Error.CoercesTo(DType.EmptyRecord));

            // Coercion to Date
            Assert.False(DType.Boolean.CoercesTo(DType.Date));
            Assert.True(DType.Number.CoercesTo(DType.Date));
            Assert.True(DType.Currency.CoercesTo(DType.Date));
            Assert.False(DType.Color.CoercesTo(DType.Date));
            Assert.True(DType.DateTime.CoercesTo(DType.Date));
            Assert.True(DType.Date.CoercesTo(DType.Date));
            Assert.True(DType.Time.CoercesTo(DType.Date));
            Assert.True(DType.String.CoercesTo(DType.Date));
            Assert.False(DType.Hyperlink.CoercesTo(DType.Date));
            Assert.False(DType.Image.CoercesTo(DType.Date));
            Assert.False(DType.PenImage.CoercesTo(DType.Date));
            Assert.False(DType.Guid.CoercesTo(DType.Date));
            Assert.False(DType.Media.CoercesTo(DType.Date));
            Assert.False(DType.Blob.CoercesTo(DType.Date));
            Assert.False(DType.EmptyTable.CoercesTo(DType.Date));
            Assert.False(DType.EmptyRecord.CoercesTo(DType.Date));
            Assert.True(DType.EmptyEnum.CoercesTo(DType.Date));
            Assert.True(DType.TryParse("%D[A:2]", out type) && type.CoercesTo(DType.Date));
            Assert.False(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.Date));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.Date));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.Date));
            Assert.True(DType.ObjNull.CoercesTo(DType.Date));
            Assert.False(DType.Error.CoercesTo(DType.Date));

            // Coercion to Time
            Assert.False(DType.Boolean.CoercesTo(DType.Time));
            Assert.True(DType.Number.CoercesTo(DType.Time));
            Assert.True(DType.Currency.CoercesTo(DType.Time));
            Assert.False(DType.Color.CoercesTo(DType.Time));
            Assert.True(DType.DateTime.CoercesTo(DType.Time));
            Assert.True(DType.Date.CoercesTo(DType.Time));
            Assert.True(DType.Time.CoercesTo(DType.Time));
            Assert.True(DType.String.CoercesTo(DType.Time));
            Assert.False(DType.Hyperlink.CoercesTo(DType.Time));
            Assert.False(DType.Image.CoercesTo(DType.Time));
            Assert.False(DType.Guid.CoercesTo(DType.Time));
            Assert.False(DType.PenImage.CoercesTo(DType.Time));
            Assert.False(DType.Media.CoercesTo(DType.Time));
            Assert.False(DType.Blob.CoercesTo(DType.Time));
            Assert.False(DType.EmptyTable.CoercesTo(DType.Time));
            Assert.False(DType.EmptyRecord.CoercesTo(DType.Time));
            Assert.True(DType.EmptyEnum.CoercesTo(DType.Time));
            Assert.True(DType.TryParse("%T[A:2]", out type) && type.CoercesTo(DType.Time));
            Assert.False(DType.TryParse("%n[A:2]", out type) && type.CoercesTo(DType.Time));
            Assert.False(DType.TryParse("%b[A:true]", out type) && type.CoercesTo(DType.Time));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(DType.Time));
            Assert.True(DType.ObjNull.CoercesTo(DType.Time));
            Assert.False(DType.Error.CoercesTo(DType.Time));

            // Coercion to Attachment type
            Assert.False(DType.Boolean.CoercesTo(AttachmentType));
            Assert.False(DType.Number.CoercesTo(AttachmentType));
            Assert.False(DType.Currency.CoercesTo(AttachmentType));
            Assert.False(DType.Color.CoercesTo(AttachmentType));
            Assert.False(DType.Guid.CoercesTo(AttachmentType));
            Assert.False(DType.DateTime.CoercesTo(AttachmentType));
            Assert.False(DType.Date.CoercesTo(AttachmentType));
            Assert.False(DType.Time.CoercesTo(AttachmentType));
            Assert.False(DType.String.CoercesTo(AttachmentType));
            Assert.False(DType.Hyperlink.CoercesTo(AttachmentType));
            Assert.False(DType.Image.CoercesTo(AttachmentType));
            Assert.False(DType.PenImage.CoercesTo(AttachmentType));
            Assert.False(DType.Media.CoercesTo(AttachmentType));
            Assert.False(DType.Blob.CoercesTo(AttachmentType));
            Assert.False(DType.EmptyTable.CoercesTo(AttachmentType));
            Assert.False(DType.EmptyRecord.CoercesTo(AttachmentType));
            Assert.True(AttachmentType.CoercesTo(AttachmentType));
            Assert.False(DType.EmptyEnum.CoercesTo(AttachmentType));
            Assert.False(DType.TryParse("%s[A:\"hello\"]", out type) && type.CoercesTo(AttachmentType));
            Assert.True(DType.ObjNull.CoercesTo(AttachmentType));
            Assert.False(DType.Error.CoercesTo(AttachmentType));

            // Coercion to Error type
            Assert.True(DType.Error.CoercesTo(DType.Error));
        }
        
        [Fact]
        public void DTypeTestOptionSetCoercion()
        {
            Assert.True(OptionSetValueType.Accepts(OptionSetValueType));
            Assert.True(OptionSetType.CoercesTo(OptionSetType));

            Assert.False(OptionSetValueType.CoercesTo(DType.Boolean));
            Assert.True(OptionSetValueType.CoercesTo(DType.String));
        }
        
        private void TestUnion(string t1, string t2, string tResult)
        {
            DType type1 = TestUtils.DT(t1);
            Assert.True(type1.IsValid);
            DType type2 = TestUtils.DT(t2);
            Assert.True(type2.IsValid);
            DType typeResult = TestUtils.DT(tResult);
            Assert.True(typeResult.IsValid);
            Assert.Equal<DType>(typeResult, DType.Union(type1, type2));
        }

        private void TestUnion(DType type1, DType type2, DType typeResult)
        {
            Assert.True(type1.IsValid);
            Assert.True(type2.IsValid);
            Assert.True(typeResult.IsValid);
            Assert.Equal<DType>(typeResult, DType.Union(type1, type2));
        }

        [Fact]
        public void DTypeUnion()
        {
            TestUnion("n", "n", "n");
            TestUnion("n", "$", "n");
            TestUnion("n", "c", "e");
            TestUnion("n", "d", "e");
            TestUnion("$", "n", "n");
            TestUnion("$", "d", "e");
            TestUnion("$", "D", "e");
            TestUnion("$", "T", "e");
            TestUnion("c", "n", "e");
            TestUnion("d", "n", "e");
            TestUnion("d", "$", "e");
            TestUnion("n", "o", "e");
            TestUnion("o", "n", "e");

            TestUnion("b", "b", "b");
            TestUnion("b", "n", "e");
            TestUnion("b", "s", "e");
            TestUnion("b", "$", "e");
            TestUnion("b", "o", "e");
            TestUnion("o", "b", "e");

            TestUnion("p", "$", "e");
            TestUnion("p", "n", "e");
            TestUnion("p", "c", "e");
            TestUnion("p", "b", "e");
            TestUnion("p", "m", "h");
            TestUnion("p", "o", "h");
            TestUnion("o", "p", "h");

            TestUnion("s", "s", "s");
            TestUnion("s", "h", "s");
            TestUnion("s", "i", "s");
            TestUnion("s", "m", "s");
            TestUnion("s", "o", "s");
            TestUnion("o", "s", "s");
            TestUnion("s", "g", "s");
            TestUnion("g", "s", "s");

            TestUnion("h", "m", "h");
            TestUnion("h", "s", "s");
            TestUnion("i", "s", "s");
            TestUnion("i", "h", "h");
            TestUnion("i", "m", "h");
            TestUnion("p", "i", "i");
            TestUnion("p", "h", "h");
            TestUnion("p", "s", "s");

            TestUnion("c", "c", "c");
            TestUnion("$", "$", "$");
            TestUnion("h", "h", "h");
            TestUnion("i", "i", "i");
            TestUnion("p", "p", "p");
            TestUnion("d", "d", "d");
            TestUnion("m", "m", "m");
            TestUnion("o", "o", "o");

            TestUnion("D", "T", "d");
            TestUnion("T", "D", "d");
            TestUnion("d", "T", "d");
            TestUnion("d", "D", "d");
            TestUnion("T", "d", "d");
            TestUnion("D", "d", "d");
            TestUnion("D", "$", "e");
            TestUnion("T", "$", "e");

            TestUnion("*[]", "*[]", "*[]");

            TestUnion("*[A:n]", "*[]", "*[A:n]");
            TestUnion("*[]", "*[A:n]", "*[A:n]");
            TestUnion("*[A:n]", "*[A:$]", "*[A:n]");
            TestUnion("*[A:$]", "*[A:n]", "*[A:n]");

            TestUnion("*[A:n]", "*[B:n]", "*[A:n, B:n]");
            TestUnion("*[A:n]", "*[B:s]", "*[A:n, B:s]");
            TestUnion("*[A:n]", "*[B:b]", "*[A:n, B:b]");

            TestUnion("*[]", "*[A:n, B:b, D:d]", "*[A:n, B:b, D:d]");
            TestUnion("*[A:n, B:b, D:d]", "*[]", "*[A:n, B:b, D:d]");
            TestUnion("*[A:n, B:b, D:d]", "*[A:n, B:b]", "*[A:n, B:b, D:d]");
            TestUnion("*[A:n, B:b, D:d]", "*[X:s, Y:n]", "*[A:n, B:b, D:d, X:s, Y:n]");

            // Tests for Type DataNull, DataNull is compatable with any data type, regardless of order.
            TestUnion("N", "N", "N");
            TestUnion("s", "N", "s");
            TestUnion("b", "N", "b");
            TestUnion("n", "N", "n");
            TestUnion("i", "N", "i");
            TestUnion("N", "i", "i");
            TestUnion("$", "N", "$");
            TestUnion("h", "N", "h");
            TestUnion("o", "N", "o");
            TestUnion("c", "N", "c");
            TestUnion("N", "c", "c");
            TestUnion("p", "N", "p");
            TestUnion("m", "N", "m");
            TestUnion("e", "N", "e");
            TestUnion("*[]", "N", "*[]");
            TestUnion("N", "*[]", "*[]");
            TestUnion("*[A:N]", "*[A:$]", "*[A:$]");
            TestUnion("*[A:b]", "*[A:N]", "*[A:b]");
            TestUnion("*[A:N]", "*[A:b]", "*[A:b]");
            TestUnion("*[A:N]", "*[A:s]", "*[A:s]");
            TestUnion("*[A:e]", "*[A:N]", "*[A:e]");
            TestUnion("*[A:n]", "*[A:N]", "*[A:n]");
            TestUnion("*[A:n, B:b, D:s]", "*[D:N]", "*[A:n, B:b, D:s]");
            TestUnion("*[A:n, B:b, D:*[A:s]]", "*[D:N]", "*[A:n, B:b, D:*[A:s]]");

            // Nested aggregates
            TestUnion("*[A:*[A:![X:n, Y:b]]]", "*[A:*[A:![Z:s]]]", "*[A:*[A:![X:n, Y:b, Z:s]]]");
            TestUnion("![A:n, Nest:*[X:n, Y:n, Z:b]]", "![]", "![A:n, Nest:*[X:n, Y:n, Z:b]]");
            TestUnion("*[A:n, Nest:*[X:n, Y:n, Z:b]]", "*[]", "*[A:n, Nest:*[X:n, Y:n, Z:b]]");
            TestUnion("*[A:n, Nest:*[X:n, Y:c, Z:b]]", "*[X:s, Nest:*[X:$, Y:n, W:s]]", "*[A:n, X:s, Nest:*[X:n, Y:e, Z:b, W:s]]");

            // Unresolvable conflicts
            TestUnion("*[A:n]", "*[A:s]", "*[A:e]");
            TestUnion("*[A:n, B:b, D:s]", "*[A:n, B:s, D:s]", "*[A:n, B:e, D:s]");
            TestUnion("*[A:n]", "![B:n]", "e");

            //Attachment
            var type1 = DType.CreateAttachmentType(DType.CreateAttachmentType(DType.CreateTable(new TypedName(DType.String, new DName("DisplayName")))));
            var type2 = DType.CreateAttachmentType(DType.CreateAttachmentType(DType.CreateTable(new TypedName(DType.String, new DName("Name")))));
            TestUnion(type1, type1, type1);
            TestUnion(type1, type2, DType.Error);
            TestUnion(type2, type2, type2);
            TestUnion(DType.Unknown, type1, type1);
            TestUnion(DType.ObjNull, type1, type1);
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

            typeStr = "*[A:n, B:b, C:$, 'Last=!5':n]";
            type = TestUtils.DT(typeStr);
            Assert.True(type.IsAggregate);
            Assert.Equal(4, type.ChildCount);
            Assert.Equal(typeStr, type.ToString());

            typeStr = "*[A:n, B:b, C:$, 'Last=!5':n, 'X,,,=!#@$%':n]";
            type = TestUtils.DT(typeStr);
            Assert.True(type.IsAggregate);
            Assert.Equal(5, type.ChildCount);
            Assert.Equal(typeStr, type.ToString());

            typeStr = "*[A:n, B:b, 'C() * 3/123 - Infinity':$, 'Last=!5':n, 'X,,,=!#@$%':n]";
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
        [InlineData("$", false)]
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
        [InlineData("*[X:*[A:*[], B:![X:n, Y:b], C:*[D:![E:$], E:*[F:n]]], Y:![Z:b, W:*[A:*[B:![C:*[D:![E:n, F:$, G:b]]]]]]]", false)]
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
        [InlineData("![A:s, B:n, C:$]", "![A:s, B:n, C:$]", "![A:s, B:n, C:$]")]
        [InlineData("![A:n, B:s, C:i]", "![A:s, B:n, C:$]", "![]")]
        [InlineData("![A:s, B:s, C:i]", "![A:s, B:n, C:$]", "![A:s]")]
        [InlineData("*[A:s, B:s, C:i]", "*[A:s, B:n, C:$]", "*[A:s]")]
        [InlineData("*[A:s, B:s, C:i]", "![A:s, B:n, C:$]", "e")]
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
        [InlineData("*[X:*[A:*[], B:![X:n, Y:b], C:*[D:![E:$], E:*[F:n]]], Y:![Z:b, W:*[A:*[B:![C:*[D:![E:n, F:$, G:b]]]]]]]", false)]
        public void TestDTypeContainsUO(string typeAsString, bool containsUO)
        {
            Assert.Equal(containsUO, TestUtils.DT(typeAsString).ContainsKindNested(DPath.Root, DKind.UntypedObject));
        }
    }
}
