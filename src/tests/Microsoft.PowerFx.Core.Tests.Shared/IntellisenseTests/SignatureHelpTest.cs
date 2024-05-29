// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Intellisense.SignatureHelp;
using Microsoft.PowerFx.Syntax;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.PowerFx.Tests.IntellisenseTests
{
    public class SignatureHelpTest : IntellisenseTestBase
    {       
        private const bool RegenerateSignatureHelp = false;

        /// <summary>
        /// Resolves to the directory in the src folder that corresponds to the current directory, which may
        /// instead include the subpath bin/(Debug|Release).AnyCPU, depending on whether the assembly was
        /// built in debug or release mode.
        /// </summary>
        private static readonly string _baseDirectory = Path.Join(Directory.GetCurrentDirectory(), "IntellisenseTests", "TestSignatures");

        private static readonly string _signatureHelpDirectory = RegenerateSignatureHelp ?
            _baseDirectory
                .Replace(Path.Join("bin", "Debug", "netcoreapp3.1"), string.Empty)
                .Replace(Path.Join("bin", "Release", "netcoreapp3.1"), string.Empty) :
            _baseDirectory;

        /// <summary>
        /// Reads the current signature help test, located in the TestSignatures directory, deserializes and
        /// asserts and compares its value with <see cref="SignatureHelp"/>, then increments the index,
        /// <see cref="SignatureHelpId"/>.
        /// </summary>
        /// <param name="signatureHelp">
        /// Signature help value to test.
        /// </param>
        internal void CheckSignatureHelpTest(SignatureHelp signatureHelp, int helpId)
        {
            var directory = _signatureHelpDirectory;
            var signatureHelpPath = Path.Join(_signatureHelpDirectory, helpId + ".json");

            if (File.Exists(signatureHelpPath))
            {
                var expectedSignatureHelp = ReadSignatureHelpFile(signatureHelpPath);
                var actualSignatureHelp = SerializeSignatureHelp(signatureHelp);

                if (RegenerateSignatureHelp)
                {
                    #pragma warning disable CS0162 // Unreachable code due to a local switch to regenerate baseline files
                    if (!JToken.DeepEquals(actualSignatureHelp, expectedSignatureHelp))
                    {
                        WriteSignatureHelp(signatureHelpPath, signatureHelp);
                    }
                    #pragma warning restore CS0162
                }
                else
                {
                    #pragma warning disable CS0162 // Unreachable code due to a local switch to regenerate baseline files
                    Assert.True(JToken.DeepEquals(actualSignatureHelp, expectedSignatureHelp));
                    #pragma warning restore CS0162
                }
            }
            else
            {
                Assert.True(RegenerateSignatureHelp, "Snapshot regeneration must be explicitly enabled to make new snapshot tests. Target path is: " + signatureHelpPath);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                WriteSignatureHelp(signatureHelpPath, signatureHelp);
            }
        }

        private JObject ReadSignatureHelpFile(string signatureHelpPath) => JObject.Parse(File.ReadAllText(signatureHelpPath));

        private string SerializeSig(SignatureHelp signatureHelp) => JsonConvert.SerializeObject(signatureHelp, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented });

        private JObject SerializeSignatureHelp(SignatureHelp signatureHelp) => JObject.Parse(SerializeSig(signatureHelp));

        private void WriteSignatureHelp(string path, SignatureHelp signatureHelp) => File.WriteAllText(path, SerializeSig(signatureHelp));

        /// <summary>
        /// These use json value comparisons to test the signature help output of
        /// Intellisense.<see cref="Suggest"/>.  The expected value of each test is stored in ./TestSignatures,
        /// which are named according to the order in which they appear below.
        /// </summary>
        /// <param name="expression"></param>
        [Theory]
        [InlineData("ForAll(|", 0)]
        [InlineData("Filter(|", 1)]
        [InlineData("Filter([{Value:\"test\"}],|", 2)]
        [InlineData("Filter([{Value:\"test\"}],Value=\"\",|", 3)]
        [InlineData("Filter([{Column1: 0, Column2: 0, Column3: 0}], 0, Column1, Column2, Column3|", 4)]
        [InlineData("ForAll([0,1,2,3], Value + 1, Value + 1, Value + 1|", 5)]
        [InlineData("ForAll([0,1,2,3], Value + 1, Value +| 1, Value + 1", 6)]
        [InlineData("If(true, If(true, 0, 1|))", 7)]
        [InlineData("If(true, If(true, 0, 1)|)", 8)]
        [InlineData("Filter|", 9)]
        [InlineData("|", 10)]
        [InlineData("Boolean(|", 11)]
        [InlineData("Max(|", 12)]
        [InlineData("Max([1,2],|", 13)]
        [InlineData("Max(1,2,3,4,5,6,|", 14)]
        [InlineData("Left(|", 15)]
        [InlineData("Table(|", 16)]
        [InlineData("Table({Value:1}, {Value: 2},| {Value:3}, {Value:3})", 17)]
        [InlineData("Table({Value:1}, {Value: Sqrt(|1)},{Value:3}, {Value:3})", 18)]
        [InlineData("LastN([1,2],|", 19)]
        public void TestSignatureHelp(string expression, int helpId) => CheckSignatureHelpTest(Suggest(expression, SuggestTests.Default, CultureInfo.InvariantCulture).SignatureHelp, helpId);

        [Fact]
        public void TestRegenerateSignatureHelpIsOff() => Assert.False(RegenerateSignatureHelp);
    }
}
