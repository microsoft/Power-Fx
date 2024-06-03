// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit.Abstractions;

namespace Microsoft.PowerFx.Core.Tests
{
    // Wrap a test case for calling from xunit. 
    public class ExpressionTestCase : TestCase, IXunitSerializable
    {
        private readonly string _engineName = null;

        // Normally null. Set if the test discovery infrastructure needs to send a notice to the test runner. 
        public string FailMessage;

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
            Expected = test.Expected;
            SourceFile = test.SourceFile;
            SourceLine = test.SourceLine;
            SetupHandlerName = test.SetupHandlerName;
            DisableDotNet = test.DisableDotNet;
        }

        public static ExpressionTestCase Fail(string message)
        {
            return new ExpressionTestCase
            {
                FailMessage = message
            };
        }

        public override string ToString()
        {
            string netVersion = RuntimeInformation.FrameworkDescription;
            string symbol = "?";

            if (netVersion.StartsWith(".NET Framework 4.6.") || 
                netVersion.StartsWith(".NET Framework 4.7.") || 
                netVersion.StartsWith(".NET Framework 4.8."))
            {
                symbol = "4";
            }
            else if (netVersion.StartsWith(".NET Core 3.1."))
            {
                symbol = "3";
            }
            else if (netVersion.StartsWith(".NET 5."))
            {
                symbol = "5";
            }
            else if (netVersion.StartsWith(".NET 6."))
            {
                symbol = "6";
            }
            else if (netVersion.StartsWith(".NET 7."))
            {
                symbol = "7";
            }
            else if (netVersion.StartsWith(".NET 8."))
            {
                symbol = "8";
            }
            else if (netVersion.StartsWith(".NET 9."))
            {
                symbol = "9";
            }

            var str = $"{symbol} {Path.GetFileName(SourceFile)} : {SourceLine.ToString("000")} - {Input} = {Expected}";

            if (!string.IsNullOrEmpty(SetupHandlerName))
            {
                str += " - Setup: " + SetupHandlerName;
            }

            return str;
        }

        public void Deserialize(IXunitSerializationInfo info)
        {
            try
            {
                Expected = info.GetValue<string>("expected");
                Input = info.GetValue<string>("input");
                SourceFile = info.GetValue<string>("sourceFile");
                SourceLine = info.GetValue<int>("sourceLine");
                SetupHandlerName = info.GetValue<string>("setupHandlerName");
                FailMessage = info.GetValue<string>("failMessage");
                DisableDotNet = info.GetValue<string>("disableDotNet");
            }
            catch (Exception e)
            {
                FailMessage = $"Failed to deserialized test {e.Message}";
            }
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue("expected", Expected, typeof(string));
            info.AddValue("input", Input, typeof(string));
            info.AddValue("sourceFile", SourceFile, typeof(string));
            info.AddValue("sourceLine", SourceLine, typeof(int));
            info.AddValue("setupHandlerName", SetupHandlerName, typeof(string));
            info.AddValue("failMessage", FailMessage, typeof(string));
            info.AddValue("disableDotNet", DisableDotNet, typeof(string));
        }
    }
}
