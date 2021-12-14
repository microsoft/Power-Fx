// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Texl.Intellisense.SignatureHelp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.PowerFx.Tests.IntellisenseTests
{
    public class SignatureHelpTest : IntellisenseTestBase
    {
        static bool RegenerateSignatureHelp = false;

        /// <summary>
        /// Resolves to the directory in the src folder that corresponds to the current directory, which may
        /// instead include the subpath bin/(Debug|Release).AnyCPU, depending on whether the assembly was
        /// built in debug or release mode
        /// </summary>
        private static string _signatureHelpDirectory = Path.Join(Directory.GetCurrentDirectory(), "IntellisenseTests", "TestSignatures")
            .Replace(Path.Join("bin", "Debug.AnyCPU"), "src")
            .Replace(Path.Join("bin", "Release.AnyCPU"), "src");

        /// <summary>
        /// Reads the current signature help test, located in the TestSignatures directory, deserializes and
        /// asserts and compares its value with <see cref="SignatureHelp"/>, then increments the index,
        /// <see cref="SignatureHelpId"/>.
        /// </summary>
        /// <param name="signatureHelp">
        /// Signature help value to test
        /// </param>
        private void CheckSignatureHelpTest(SignatureHelp signatureHelp, int helpId)
        {
            var directory = _signatureHelpDirectory;
            var signatureHelpPath = Path.Join(_signatureHelpDirectory, helpId + ".json");

            if (File.Exists(signatureHelpPath))
            {
                var expectedSignatureHelp = ReadSignatureHelpFile(signatureHelpPath);
                var actualSignatureHelp = SerializeSignatureHelp(signatureHelp);

                if (RegenerateSignatureHelp)
                {
                    if (!JToken.DeepEquals(actualSignatureHelp, expectedSignatureHelp))
                    {
                        WriteSignatureHelp(signatureHelpPath, signatureHelp);
                    }
                }
                else
                {
                    Assert.True(JToken.DeepEquals(actualSignatureHelp, expectedSignatureHelp));
                }
            }
            else
            {
                Assert.True(RegenerateSignatureHelp, "Snapshot regeneration must be explicitly enabled to make new snapshot tests. Target path is: " + signatureHelpPath);

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                WriteSignatureHelp(signatureHelpPath, signatureHelp);
            }
        }

        private JObject ReadSignatureHelpFile(string signatureHelpPath) => JObject.Parse(File.ReadAllText(signatureHelpPath));
        private JObject SerializeSignatureHelp(SignatureHelp signatureHelp) => JObject.Parse(JsonConvert.SerializeObject(signatureHelp));
        private void WriteSignatureHelp(string path, SignatureHelp signatureHelp) => File.WriteAllText(path, JsonConvert.SerializeObject(signatureHelp, Formatting.Indented));

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
        public void TestSignatureHelp(string expression, int helpId) => CheckSignatureHelpTest(Suggest(expression, new PowerFxConfig()).SignatureHelp, helpId);

        [Fact]
        public void TestRegenerateSignatureHelpIsOff() => Assert.False(RegenerateSignatureHelp);
    }
}
