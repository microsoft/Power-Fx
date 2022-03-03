// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace Microsoft.PowerFx.Core.Tests
{
    // Wrap a test case for calling from xunit. 
    public class ExpressionTestCase : TestCase, IXunitSerializable
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

        public ExpressionTestCase(string engineName, TestCase test)
            : this(engineName)
        {
            Input = test.Input;
            _expected = test._expected;
            SourceFile = test.SourceFile;
            SourceLine = test.SourceLine;
            SetupHandlerName = test.SetupHandlerName;
        }

        public override string ToString()
        {
            return $"{Path.GetFileName(SourceFile)} : {SourceLine.ToString("000")} - {Input} = {GetExpected(_engineName)}";
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
