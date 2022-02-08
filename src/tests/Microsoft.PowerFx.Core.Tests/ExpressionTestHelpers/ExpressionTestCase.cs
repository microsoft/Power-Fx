// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace Microsoft.PowerFx.Core.Tests
{
    // Describe a test case in the file. 
    public class ExpressionTestCase : IXunitSerializable
    {
        private readonly string _engineName = null;

        public ExpressionTestCase()
        {
            _engineName = "-";
        }

        public ExpressionTestCase(string engineName)
        {
            _engineName = engineName;
        }

        // Formula string to run 
        public string Input;

        // Expected Result, indexed by runner name
        private Dictionary<string, string> _expected = new Dictionary<string, string>();

        // Location from source file. 
        public string SourceFile;
        public int SourceLine;
        public string SetupHandlerName;

        public override string ToString()
        {
            return $"{Path.GetFileName(SourceFile)} : {SourceLine.ToString("000")} - {Input} = {GetExpected(_engineName)}";
        }

        public void SetExpected(string expected, string engineName = null)
        {
            if (engineName == null)
            {
                engineName = "-";
            }

            _expected[engineName] = expected;
        }

        public string GetExpected(string engineName)
        {
            if (!_expected.TryGetValue(engineName, out var expected))
            {
                return _expected["-"];
            }

            return expected;
        }

        public void Deserialize(IXunitSerializationInfo info)
        {
            _expected = JsonConvert.DeserializeObject<Dictionary<string, string>>(info.GetValue<string>("expected"));
            Input = info.GetValue<string>("input");
            SourceFile = info.GetValue<string>("sourceFile");
            SourceLine = info.GetValue<int>("sourceLine");
            SetupHandlerName = info.GetValue<string>("setupHandlerName");
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            var expectedJSON = JsonConvert.SerializeObject(_expected);
            info.AddValue("expected", expectedJSON, typeof(string));
            info.AddValue("input", Input, typeof(string));
            info.AddValue("sourceFile", SourceFile, typeof(string));
            info.AddValue("sourceLine", SourceLine, typeof(int));
            info.AddValue("setupHandlerName", SetupHandlerName, typeof(string));
        }
    }
}
