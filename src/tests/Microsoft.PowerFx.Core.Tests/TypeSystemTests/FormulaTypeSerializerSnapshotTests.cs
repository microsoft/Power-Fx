// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.IO;
using System.Text.Json;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    public class FormulaTypeSerializerSnapshotTests
    {
        private const bool RegenerateSnapshots = true;

        /// <summary>
        /// Resolves to the directory in the src folder that corresponds to the current directory, which may
        /// instead include the subpath bin/(Debug|Release).AnyCPU, depending on whether the assembly was
        /// built in debug or release mode.
        /// </summary>
        private static readonly string _typeSnapshotDirectory = Path.Join(Directory.GetCurrentDirectory(), "TypeSystemTests", "JsonTypeSnapshots")
            .Replace(Path.Join("bin", "Debug.AnyCPU"), "src")
            .Replace(Path.Join("bin", "Release.AnyCPU"), "src");

        private void CheckTypeSnapshot(FormulaType type, string testId, JsonSerializerOptions options)
        {
            var directory = _typeSnapshotDirectory;
            var typeSnapshot = Path.Join(_typeSnapshotDirectory, testId + ".json");

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
                        File.WriteAllText(typeSnapshot, actual);
                    }
                }
                else
                {
                    TestUtils.AssertJsonEqual(actual, expected);
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

                File.WriteAllText(typeSnapshot, actual);
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
        [InlineData("PrimitiveDate", "d")]
        [InlineData("PrimitiveTime", "t")]
        [InlineData("PrimitiveDateTime", "D")]
        [InlineData("PrimitiveHyperlink", "h")]
        [InlineData("PrimitiveDTNTZ", "Z")]
        [InlineData("PrimitiveGuid", "g")]
        [InlineData("PrimitiveUntyped", "O")]
        [InlineData("PrimitiveBlank", "N")]
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
                Converters =
                {
                    // Serialize types without accounting for any defined type names
                    new FormulaTypeJsonConverter(new DefinedTypeSymbolTable())
                }
            });
        }

        [Fact]
        public void TestRegenerateSignatureHelpIsOff() => Assert.False(RegenerateSnapshots);
    }
}
