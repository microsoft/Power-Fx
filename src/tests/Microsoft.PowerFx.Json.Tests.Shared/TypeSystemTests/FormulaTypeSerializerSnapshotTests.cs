// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Json.Tests
{
    public class FormulaTypeSerializerSnapshotTests
    {
        private const bool RegenerateSnapshots = false;

        /// <summary>
        /// Resolves to the directory in the src folder that corresponds to the current directory, which may
        /// instead include the subpath bin/(Debug|Release).AnyCPU, depending on whether the assembly was
        /// built in debug or release mode.
        /// </summary>       

#if NET8_0
        private static readonly string _baseDirectory = Path.Join(Directory.GetCurrentDirectory(), "TypeSystemTests", "JsonTypeSnapshots");

        private static readonly string _typeSnapshotDirectory = RegenerateSnapshots
            ? _baseDirectory
                .Replace(Path.Join("bin", "Debug", "net8.0"), string.Empty)
                .Replace(Path.Join("bin", "Release", "net8.0"), string.Empty)
                .Replace(Path.Join("bin", "DebugAll", "net8.0"), string.Empty)
                .Replace(Path.Join("bin", "ReleaseAll", "net8.0"), string.Empty)
                .Replace(Path.Join("bin", "Debug70", "net8.0"), string.Empty)
                .Replace(Path.Join("bin", "Release70", "net8.0"), string.Empty)
            : _baseDirectory;
#endif 

#if NET7_0
        private static readonly string _baseDirectory = Path.Join(Directory.GetCurrentDirectory(), "TypeSystemTests", "JsonTypeSnapshots");

        private static readonly string _typeSnapshotDirectory = RegenerateSnapshots 
            ? _baseDirectory
                .Replace(Path.Join("bin", "Debug", "net7.0"), string.Empty)
                .Replace(Path.Join("bin", "Release", "net7.0"), string.Empty)
                .Replace(Path.Join("bin", "DebugAll", "net7.0"), string.Empty)
                .Replace(Path.Join("bin", "ReleaseAll", "net7.0"), string.Empty)
                .Replace(Path.Join("bin", "Debug70", "net7.0"), string.Empty)
                .Replace(Path.Join("bin", "Release70", "net7.0"), string.Empty)
            : _baseDirectory;
#endif 

#if NET462
        private static readonly string _baseDirectory = $@"{Directory.GetCurrentDirectory()}\TypeSystemTests\JsonTypeSnapshots";

        private static readonly string _typeSnapshotDirectory = RegenerateSnapshots
            ? _baseDirectory
                .Replace(@"bin\Debug\net462", string.Empty)
                .Replace(@"bin\Release\net462", string.Empty)
                .Replace(@"bin\DebugAll\net462", string.Empty)
                .Replace(@"bin\ReleaseAll\net462", string.Empty)
                .Replace(@"bin\Debug462\net462", string.Empty)
                .Replace(@"bin\Release462\net462", string.Empty)
            : _baseDirectory;
#endif

        private void CheckTypeSnapshot(FormulaType type, string testId, JsonSerializerOptions options)
        {
            var directory = _typeSnapshotDirectory;            

#if NETCOREAPP3_1_OR_GREATER
            var typeSnapshot = Path.Join(_typeSnapshotDirectory, testId + ".json");
#else
            var typeSnapshot = $@"{_typeSnapshotDirectory}\{testId}.json";
#endif

            var actual = JsonSerializer.Serialize(type, options);

            if (File.Exists(typeSnapshot))
            {
                var expected = File.ReadAllText(typeSnapshot);

                if (RegenerateSnapshots)
                {
#pragma warning disable CS0162 // Unreachable code due to a local switch to regenerate baseline files
                    try
                    {
                        TestUtils.AssertJsonEqual(actual, expected);
                    }
                    catch (Xunit.Sdk.XunitException)
                    {
                        // Only rewrite if the JSON has meaningfully changed
                        File.WriteAllText(typeSnapshot, actual + "\r\n");
                    }
                }
                else
                {
                    TestUtils.AssertJsonEqual(actual, expected);
                    var ft = JsonSerializer.Deserialize<FormulaType>(expected, options);

                    if (type._type.IsRecord)
                    {
                        Assert.True(type._type.IsRecord);
                    }
                    else if (type._type.IsTable)
                    {
                        Assert.True(type._type.IsTable);
                    }
                    else if (type._type.IsError)
                    {
                        // Errors are returned as blank.
                        Assert.Equal(DKind.ObjNull, ft._type.Kind);
                    }
                    else
                    {
                        Assert.Equal(type._type.Kind, ft._type.Kind);
                    }
                }
#pragma warning restore CS0162
            }
            else
            {
                Assert.True(RegenerateSnapshots, "Snapshot regeneration must be explicitly enabled to make new snapshot tests. Target path is: " + typeSnapshot);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(typeSnapshot, actual + "\r\n");
            }
        }

        /// <summary>
        /// The expected value of each test is stored in ./JsonTypeSnapshots,
        /// Named by "simple" + testID.
        /// </summary>
        [Theory]
        [InlineData("PrimitiveNumber", "n")]
        [InlineData("PrimitiveBoolean", "b")]
        [InlineData("PrimitiveString", "s")]
        [InlineData("PrimitiveDate", "D")]
        [InlineData("PrimitiveTime", "T")]
        [InlineData("PrimitiveDateTime", "d")]
        [InlineData("PrimitiveHyperlink", "h")]
        [InlineData("PrimitiveDTNTZ", "Z")]
        [InlineData("PrimitiveGuid", "g")]
        [InlineData("PrimitiveUntyped", "O")]
        [InlineData("PrimitiveBlank", "N")]
        [InlineData("PrimitiveError", "e")]
        [InlineData("Record", "![Foo:n, Bar:s]")]
        [InlineData("RecordNested", "![Foo:n, Bar:![Qux:![Baz:s]]]")]
        [InlineData("RecordTableNested", "*[Foo:n, Bar:![Qux:*[Baz:s]]]")]
        [InlineData("TableSingleColumn", "*[Value:g]")]
        [InlineData("EmptyTable", "*[]")]
        [InlineData("EmptyRecord", "![]")]
        public void TypeSnapshotSimple(string testId, string type)
        {
            CheckTypeSnapshot(FormulaType.Build(TestUtils.DT(type)), "Simple" + testId, new JsonSerializerOptions()
            {
                WriteIndented = true,
                Converters =
                {
                    // Serialize types without accounting for any defined type names
                    new FormulaTypeJsonConverter(new SymbolTable())
                }
            });
        }

        [Fact]
        public void TypeSnapshotRecursive()
        {
            CheckTypeSnapshot(new BindingEngineTests.LazyRecursiveRecordType(), "Recursive", new JsonSerializerOptions()
            {
                WriteIndented = true,
                Converters =
                {
                    // Serialize types without accounting for any defined type names
                    new FormulaTypeJsonConverter(new SymbolTable())
                }
            });
        }

        [Theory]
        [InlineData(
            @"{
                ""Type"": 
                    {
                    ""Name"": ""CustomType"",
                    ""IsTable"": true
                    },
                ""CustomTypeName"": ""logicalName""
            }",
            typeof(TableType))]

        [InlineData(
            @"{
                ""Type"": 
                    {
                    ""Name"": ""CustomType""
                    },
                ""CustomTypeName"": ""logicalName""
            }",
            typeof(RecordType))]
        [InlineData(
            @"{
                ""Type"": 
                    {
                    ""Name"": ""CustomType""
                    },
                ""CustomTypeName"": ""Record""
            }",
            typeof(RecordType))]
        public void TestDataverseDerserialization(string serialized, Type type)
        {
            Func<string, RecordType> logicalNameToRecordType = (x) => x == "logicalName" || x == "Record" ? RecordType.Empty().Add("num", FormulaType.Number) : RecordType.Empty();

            var option = new JsonSerializerOptions();
            var serializer = new FormulaTypeJsonConverter();
            option.Converters.Add(serializer);

            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<FormulaType>(serialized, option));

            option = new JsonSerializerOptions();
            var settings = new FormulaTypeSerializerSettings(logicalNameToRecordType);
            serializer = new FormulaTypeJsonConverter(settings);
            option.Converters.Add(serializer);

            var deserialized = JsonSerializer.Deserialize<FormulaType>(serialized, option);

            Assert.IsAssignableFrom(type, deserialized);

            Assert.Equal("num", ((AggregateType)deserialized).FieldNames.First());
        }

        [Fact]
        public void T1()
        {
            var optionSetDisplayNameProvider = DisplayNameUtility.MakeUnique(new Dictionary<string, string>
            {
                { "1", "One" },
                { "2", "Two" },
                { "0", "Zero" },
                { "4", "Four" },
            });

            var optionSet = new OptionSet("MyOptionSet", optionSetDisplayNameProvider);
            FormulaType ft = optionSet.FormulaType;

            var opts = new JsonSerializerOptions();
            opts.Converters.Add(new FormulaTypeJsonConverter());

            var json = JsonSerializer.Serialize(ft, opts);
        }
    }
}
