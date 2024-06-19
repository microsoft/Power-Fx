// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Connectors;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Xunit;
using static Microsoft.PowerFx.Core.Tests.LazyTypeTests;

namespace Microsoft.PowerFx.Connector.Tests
{
    public class SafeDTypeToStringTests
    {
        private readonly TestLazyRecordType _lazyRecord1;
        private readonly TestLazyRecordType _lazyRecord2;
        private readonly TableType _lazyTable1;
        private readonly TableType _lazyTable2;

        public SafeDTypeToStringTests()
        {
            _lazyRecord1 = new TestLazyRecordType("Lazy1", new List<string>() { "Foo", "Bar", "Baz" }, LazyGetField1);
            _lazyRecord2 = new TestLazyRecordType("Lazy2", new List<string>() { "Qux", "Nested" }, LazyGetField2);
            _lazyTable1 = new TestLazyRecordType("Lazy3", new List<string>() { "Foo", "Bar", "Baz" }, LazyGetField1).ToTable();
            _lazyTable2 = new TestLazyRecordType("Lazy4", new List<string>() { "Qux", "Nested" }, LazyGetField2).ToTable();
        }

        private bool LazyGetField1(string name, out FormulaType type)
        {
            type = name switch
            {
                "Foo" => FormulaType.Number,
                "Bar" => FormulaType.String,
                "Baz" => FormulaType.Boolean,
                _ => FormulaType.Blank,
            };

            return type != FormulaType.Blank;
        }

        private bool LazyGetField2(string name, out FormulaType type)
        {
            type = name switch
            {
                "Qux" => FormulaType.Number,
                "Nested" => _lazyRecord1,
                _ => FormulaType.Blank,
            };

            return type != FormulaType.Blank;
        }

        private static DType AttachmentTableType => DType.CreateAttachmentType(DType.CreateTable(new TypedName(DType.String, new DName("DisplayName"))));

        private static DType AttachmentRecordType => DType.CreateAttachmentType(DType.CreateRecord(new TypedName(DType.Number, new DName("Wrapped"))));

        [Fact]
        public void DTypeRepresentation()
        {
            Assert.Equal("r!", _lazyRecord1._type.SafeDTypeToString());
            Assert.Equal("r*", _lazyTable1._type.SafeDTypeToString());
        }

        [Fact]
        public void DTypeToStringRepresentation()
        {
            Assert.Equal("?", DType.Unknown.SafeDTypeToString());
            Assert.Equal("e", DType.Error.SafeDTypeToString());
            Assert.Equal("x", DType.Invalid.SafeDTypeToString());
            Assert.Equal("b", DType.Boolean.SafeDTypeToString());
            Assert.Equal("n", DType.Number.SafeDTypeToString());
            Assert.Equal("s", DType.String.SafeDTypeToString());
            Assert.Equal("h", DType.Hyperlink.SafeDTypeToString());
            Assert.Equal("d", DType.DateTime.SafeDTypeToString());
            Assert.Equal("c", DType.Color.SafeDTypeToString());
            Assert.Equal("$", DType.Currency.SafeDTypeToString());
            Assert.Equal("i", DType.Image.SafeDTypeToString());
            Assert.Equal("p", DType.PenImage.SafeDTypeToString());
            Assert.Equal("m", DType.Media.SafeDTypeToString());
            Assert.Equal("g", DType.Guid.SafeDTypeToString());
            Assert.Equal("o", DType.Blob.SafeDTypeToString());
            Assert.Equal("r*", AttachmentTableType.SafeDTypeToString());
            Assert.Equal("r!", AttachmentRecordType.SafeDTypeToString());
            Assert.Equal("T", DType.Time.SafeDTypeToString());
            Assert.Equal("D", DType.Date.SafeDTypeToString());
            Assert.Equal("N", DType.ObjNull.SafeDTypeToString());
            Assert.Equal("P", DType.Polymorphic.SafeDTypeToString());
            Assert.Equal("V", DType.NamedValue.SafeDTypeToString());
            Assert.Equal("X", DType.Deferred.SafeDTypeToString());
            Assert.Equal("-", DType.Void.SafeDTypeToString());
            Assert.Equal("w", DType.Decimal.SafeDTypeToString());
        }

        [Fact]
        public void RecordAndTableDTypeTests()
        {
            bool err = false;
            var type1 = DType.CreateTable(
                new TypedName(DType.Number, new DName("A")),
                new TypedName(DType.Number, new DName("B")),
                new TypedName(DType.Number, new DName("C")));

            Assert.Equal("*[field1:n, field2:n, field3:n]", type1.SafeDTypeToString());
            Assert.Equal("![field1:n, field2:n, field3:n]", type1.ToRecord().SafeDTypeToString());
            Assert.Equal("![field1:n, field2:n, field3:n]", type1.ToRecord().ToRecord().SafeDTypeToString());
            Assert.Equal("![field1:n, field2:n, field3:n]", type1.ToRecord().SafeDTypeToString());

            var type2 = DType.CreateTable(
                new List<TypedName>()
                {
                    new TypedName(DType.Number, new DName("B")),
                    new TypedName(DType.Number, new DName("A")),
                    new TypedName(DType.Number, new DName("C"))
                });

            Assert.Equal("*[field1:n, field2:n, field3:n]", type2.SafeDTypeToString());

            type2 = DType.CreateRecord(type1.GetNames(DPath.Root).ToArray());
            type2 = type2.Add(ref err, DPath.Root, new DName("B"), DType.String);
            Assert.Equal("![field1:n, field2:s, field3:n]", type2.SafeDTypeToString());

            type2 = DType.EmptyTable
                .Add(ref err, DPath.Root, new DName("B"), DType.Number)
                .Add(ref err, DPath.Root, new DName("C"), DType.Number)
                .Add(ref err, DPath.Root, new DName("D"), DType.Boolean)
                .Add(ref err, DPath.Root, new DName("A"), DType.Number);

            DType type3 = type1.Add(ref err, DPath.Root, new DName("D"), DType.Boolean);
            type2 = DType.CreateTable(type1.GetNames(DPath.Root));
            type3 = type2.SetType(ref err, DPath.Root.Append(new DName("B")), DType.String);
            Assert.Equal("*[field1:n, field2:s, field3:n]", type3.SafeDTypeToString());

            type3 = type2.SetType(ref err, DPath.Root.Append(new DName("B")), DType.Boolean);
            Assert.Equal("*[field1:n, field2:b, field3:n]", type3.SafeDTypeToString());

            type3 = type2.SetType(ref err, DPath.Root.Append(new DName("B")).Append(new DName("X")), DType.Boolean);
            Assert.Equal("*[field1:n, field2:n, field3:n]", type3.SafeDTypeToString());

            var type4 = type1.Add(ref err, DPath.Root.Append(new DName("X")), new DName("D"), DType.Number);

            Assert.Equal("*[field1:n, field2:n, field3:n]", type4.SafeDTypeToString());

            var type5 = DType.EmptyTable.Add(ref err, DPath.Root, new DName("D"), DType.String)
                .Add(ref err, DPath.Root, new DName("E"), DType.String);

            Assert.Equal("*[field1:s, field2:s]", type5.SafeDTypeToString());

            var type6 = DType.CreateTable(new TypedName(type5, new DName("Y")), new TypedName(DType.Boolean, new DName("Q")));

            Assert.Equal("*[field1:b, field2:*[field1:s, field2:s]]", type6.SafeDTypeToString());

            var type = type6.Add(ref err, DPath.Root.Append(new DName("Y")), new DName("E"), DType.Error);

            Assert.Equal("*[field1:b, field2:*[field1:s, field2:e]]", type.SafeDTypeToString());
            err = false;

            DType type7 = type6
                .Add(ref err, DPath.Root, new DName("N"), DType.Number)
                .Add(ref err, DPath.Root, new DName("E"), DType.Error);
            type7 = type7.Add(ref err, DPath.Root.Append(new DName("Y")), new DName("F"), DType.Boolean)
                .Add(ref err, DPath.Root.Append(new DName("Y")), new DName("G"), DType.Error);

            Assert.Equal("*[field1:e, field2:n, field3:b, field4:*[field1:s, field2:s, field3:b, field4:e]]", type7.SafeDTypeToString());

            type7 = type7.DropMulti(ref err, DPath.Root, new DName("N"), new DName("E"));
            type7 = type7
                .Drop(ref err, DPath.Root.Append(new DName("Y")), new DName("F"))
                .Drop(ref err, DPath.Root.Append(new DName("Y")), new DName("G"));

            type7 = type7.Drop(ref err, DPath.Root.Append(new DName("Q")), new DName("F"));

            DType type12 = type1.Add(ref err, DPath.Root, new DName("Y"), DType.CreateTable(new TypedName(DType.Boolean, new DName("F")), new TypedName(DType.Error, new DName("E"))));
            Assert.Equal("*[field1:n, field2:n, field3:n, field4:*[field1:e, field2:b]]", type12.SafeDTypeToString());

            DType type8 = type1.Add(ref err, DPath.Root, new DName("X"), type6);
            Assert.Equal("*[field1:n, field2:n, field3:n, field4:*[field1:b, field2:*[field1:s, field2:s]]]", type8.SafeDTypeToString());
        }

        [Fact]
        public void EnumDTypeTests()
        {
            Assert.True(DType.TryParse("%n[A:0, B:1, C:2, D:3]", out DType type) && type.IsEnum);
            Assert.True(DType.TryParse("%n[A:0, B:1, C:2, D:3]", out DType type2) && type2.IsEnum);
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
            Assert.Equal("*[field1:n, field2:s, field3:*[field1:n]]", superType.SafeDTypeToString());            
            superType = DType.Supertype(type4, type3, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("*[field1:n, field2:s, field3:*[field1:n]]", superType.SafeDTypeToString());            

            // Output should be *[A:n,B:s,D:n]
            superType = DType.Supertype(type1, type2, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("*[field1:n, field2:s, field3:n]", superType.SafeDTypeToString());            
            superType = DType.Supertype(type2, type1, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("*[field1:n, field2:s, field3:n]", superType.SafeDTypeToString());            

            // Table with null value
            // Output should be *[A:n,B:s,C:b,D:n]
            superType = DType.Supertype(type1, type2s, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("*[field1:n, field2:s, field3:b, field4:n]", superType.SafeDTypeToString());            
            superType = DType.Supertype(type2s, type1, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("*[field1:n, field2:s, field3:b, field4:n]", superType.SafeDTypeToString());            

            // Table with null value
            // Output should be *[A:n,B:s,C:*[D:n,F:d]]
            superType = DType.Supertype(type3, type4s, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("*[field1:n, field2:s, field3:*[field1:n, field2:d]]", superType.SafeDTypeToString());
            Assert.Equal(3, superType.ChildCount);
            superType = DType.Supertype(type4s, type3, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("*[field1:n, field2:s, field3:*[field1:n, field2:d]]", superType.SafeDTypeToString());
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
            Assert.Equal("![field1:n, field2:s, field3:n]", superType.SafeDTypeToString());
            superType = DType.Supertype(type6, type5, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("![field1:n, field2:s, field3:n]", superType.SafeDTypeToString());

            // Record with null value
            superType = DType.Supertype(type5, type6s, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("![field1:n, field2:s, field3:b, field4:n]", superType.SafeDTypeToString());
            superType = DType.Supertype(type6s, type5, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("![field1:n, field2:s, field3:b, field4:n]", superType.SafeDTypeToString());

            superType = DType.Supertype(type7, type8, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("![field1:n, field2:s, field3:![field1:n]]", superType.SafeDTypeToString());
            superType = DType.Supertype(type8, type7, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("![field1:n, field2:s, field3:![field1:n]]", superType.SafeDTypeToString());

            // Record with null value
            superType = DType.Supertype(type7, type8s, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("![field1:n, field2:s, field3:![field1:n, field2:d]]", superType.SafeDTypeToString());
            superType = DType.Supertype(type8s, type7, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("![field1:n, field2:s, field3:![field1:n, field2:d]]", superType.SafeDTypeToString());

            // ![A:t, B:s]
            var type9 = DType.CreateRecord(
                new TypedName(DType.Time, new DName("A")),
                new TypedName(DType.String, new DName("B")));

            // ![A:d, B:b]
            var type10 = DType.CreateRecord(
                new TypedName(DType.DateTime, new DName("A")),
                new TypedName(DType.Boolean, new DName("B")));

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
            Assert.Equal("![field1:n, field2:s, field3:![]]", superType.SafeDTypeToString());

            superType = DType.Supertype(type12, type11, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("![field1:n, field2:s, field3:![]]", superType.SafeDTypeToString());

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
            Assert.Equal("![field1:n, field2:s, field3:![field1:s]]", superType.SafeDTypeToString());

            superType = DType.Supertype(type14, type13, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("![field1:n, field2:s, field3:![field1:s]]", superType.SafeDTypeToString());

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
            Assert.Equal("![field1:s]", superType.SafeDTypeToString());

            superType = DType.Supertype(type16, type15, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: usePowerFxV1CompatibilityRules);
            Assert.Equal("![field1:s]", superType.SafeDTypeToString());
        }

        [Fact]
        public void DTypeTestEnumsParseAndPrettyprint()
        {
            var type2 = DType.CreateEnum(
                DType.Number,
                new KeyValuePair<DName, object>(new DName("Red"), -123),
                new KeyValuePair<DName, object>(new DName("Green"), -234),
                new KeyValuePair<DName, object>(new DName("Blue"), -345));

            // %n[Blue:-345, Green:-234, Red:-123]
            Assert.Equal("%n[enum1:-345, enum2:-234, enum3:-123]", type2.SafeDTypeToString());
        }

        [Fact]
        public void DTypeSpecParsing_FieldsWithBlanks()
        {
            DType.TryParse("*['foo bar':s]", out DType type);
            Assert.Equal("*[field1:s]", type.SafeDTypeToString());

            DType.TryParse("*['foo bar':s, 'hello world from AppMagic':n, something:!['App Magic':b, TheMagic:c]]", out type);
            Assert.Equal("*[field1:s, field2:n, field3:![field1:b, field2:c]]", type.SafeDTypeToString());

            DType.TryParse("![Hello:s, 'hello world':n, something:!['App Magic':b, TheMagic:c]]", out type);
            Assert.Equal("![field1:s, field2:n, field3:![field1:b, field2:c]]", type.SafeDTypeToString());

            DType.TryParse("*['some strange \"identifiers\"':s, 'with \"nested double\" quotes':s]", out type);
            Assert.Equal("*[field1:s, field2:s]", type.SafeDTypeToString());

            DType.TryParse("*['\"more strange identifiers\"':s, 'with \"nested double quotes\"':s]", out type);
            Assert.Equal("*[field1:s, field2:s]", type.SafeDTypeToString());

            DType.TryParse("%n['foo bar':1, 'hello world from beyond':2]", out type);
            Assert.Equal("%n[enum1:1, enum2:2]", type.SafeDTypeToString());

            DType.TryParse("%s['foo bar':\"foo bar car\", 'hello world from beyond':\"and then goodbye\"]", out type);
            Assert.Equal("%s[enum1:\"foo bar car\", enum2:\"and then goodbye\"]", type.SafeDTypeToString());

            DType.TryParse("*['foo bar':s, 'hello world':n, from:n, 'beyond':b]", out type);
            Assert.Equal("*[field1:b, field2:s, field3:n, field4:n]", type.SafeDTypeToString());
        }

        [Fact]
        public void DTypeAggregateWithFunkyFieldsToString()
        {
            string typeStr;

            typeStr = "*['Last=!5':n]";
            DType.TryParse(typeStr, out DType type);
            Assert.Equal("*[field1:n]", type.SafeDTypeToString());

            typeStr = "*[A:n, B:b, C:w, 'Last=!5':n]";
            DType.TryParse(typeStr, out type);
            Assert.Equal("*[field1:n, field2:b, field3:w, field4:n]", type.SafeDTypeToString());

            typeStr = "*[A:n, B:b, C:w, 'Last=!5':n, 'X,,,=!#@w%':n]";
            DType.TryParse(typeStr, out type);
            Assert.Equal("*[field1:n, field2:b, field3:w, field4:n, field5:n]", type.SafeDTypeToString());

            typeStr = "*[A:n, B:b, 'C() * 3/123 - Infinity':w, 'Last=!5':n, 'X,,,=!#@w%':n]";
            DType.TryParse(typeStr, out type);
            Assert.Equal("*[field1:n, field2:b, field3:w, field4:n, field5:n]", type.SafeDTypeToString());
        }

        [Fact]
        public void DTypeEnumWithFunkyValuesToString()
        {
            string typeStr;

            typeStr = "%n['Last=!5':10, X:123]";
            DType.TryParse(typeStr, out DType type);
            Assert.Equal("%n[enum1:10, enum2:123]", type.SafeDTypeToString());

            typeStr = "%n['Last=!5':10, 'X Y Z':123]";
            DType.TryParse(typeStr, out type);
            Assert.Equal("%n[enum1:10, enum2:123]", type.SafeDTypeToString());

            typeStr = "%n['Last=!5':10, 'X=!Y+Z':123]";
            DType.TryParse(typeStr, out type);
            Assert.Equal("%n[enum1:10, enum2:123]", type.SafeDTypeToString());
        }

        [Fact]
        public void DTypeOptionSetValueToString()
        {
            var optionSet = new OptionSet("OptionSet", DisplayNameUtility.MakeUnique(new Dictionary<string, string>()
            {
                    { "option_1", "Option1" },
                    { "option_2", "Option2" }
            }));

            // L{option_1:l, option_2:l}
            DType osType = DType.CreateOptionSetType(optionSet);

            Assert.Equal("L{optionSet1:l, optionSet2:l}", osType.SafeDTypeToString());
        }
    }
}
