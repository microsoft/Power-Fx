// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Interpreter.Tests.XUnitExtensions;
using Xunit;
using Xunit.Sdk;
using static Microsoft.PowerFx.Interpreter.Tests.ExpressionEvaluationTests;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class FileExpressionEvaluationTests : PowerFxTest
    {
        private InterpreterRunner _runner;

        [InterpreterTheory]
        [TxtFileData("ExpressionTestCases", "InterpreterExpressionTestCases", nameof(InterpreterRunner))]
        public void InterpreterTestCase(ExpressionTestCase testCase)
        {
            // This is running against embedded resources, so if you're updating the .txt files,
            // make sure they build is actually copying them over. 
            Assert.True(testCase.FailMessage == null, testCase.FailMessage);

            _runner = new InterpreterRunner();

            var (result, msg) = _runner.RunAsync(testCase).Result;

            var prefix = $"Test {Path.GetFileName(testCase.SourceFile)}:{testCase.SourceLine}: ";
            switch (result)
            {
                case TestResult.Pass:
                    break;

                case TestResult.Fail:
                    Assert.True(false, prefix + msg);
                    break;

                case TestResult.Skip:
                    Skip.If(true, prefix + msg);
                    break;
            }
        }        

        [InterpreterTheory] //(Skip="NotReadyYet")]
        [TxtFileData("ExpressionTestCases\\NotYetReady", "InterpreterExpressionTestCases", nameof(InterpreterRunner))]
        public void InterpreterTestCase_NotReadyTests(ExpressionTestCase testCase)
        {
            // This is running against embedded resources, so if you're updating the .txt files,
            // make sure they build is actually copying them over. 
            Assert.True(testCase.FailMessage == null, testCase.FailMessage);

            _runner = new InterpreterRunner();

            var (result, msg) = _runner.RunAsync(testCase).Result;

            var prefix = $"Test {Path.GetFileName(testCase.SourceFile)}:{testCase.SourceLine}: ";            

            switch (result)
            {
                case TestResult.Pass:
                    break;

                case TestResult.Fail:
                    Stats.AddOrUpdate(prefix, (testCase, msg), (x, z) => throw new Exception("Oups"));
                    Assert.True(false, prefix + msg);
                    break;

                case TestResult.Skip:
                    Skip.If(true, prefix + msg);
                    break;
            }
        }

        private enum TestCategory
        {
            DidNotReturnAValue, // contains "did not return a value"
            WrongErrorKind, // Received an error, but expected kind=InvalidArgument and received BadLanguageCode
            ErrorValue, // contains Microsoft.PowerFx.Core.Public.Values.ErrorValue
            InvalidResult, //  Expected: 31. actual: 12
            ThrewException, // contains "Threw exception"
            Unknown,
        }

        private TestCategory GetTestCategory(string str)
        {
            if (str.Contains("did not return a value"))
            {
                return TestCategory.DidNotReturnAValue;
            }
            else if (new Regex(@"Received an error, but expected kind=[a-zA-Z0-9]* and received").IsMatch(str))
            {
                return TestCategory.WrongErrorKind;
            }
            else if (str.Contains("Microsoft.PowerFx.Core.Public.Values.ErrorValue"))
            {
                return TestCategory.ErrorValue;
            }
            else if (str.Contains("Expected: ") && str.Contains(". actual: "))
            {
                return TestCategory.InvalidResult;
            }
            else if (str.Contains("Threw exception"))
            {
                return TestCategory.ThrewException;
            }

            return TestCategory.Unknown;            
        }

        private static readonly ConcurrentDictionary<string, (TestCase TestCase, string Message)> Stats = new ();

        [Fact]
        public void GetStats()
        {
            Thread.Sleep(10000); // Let's things start

            var n = Stats.Count;
            var m = -1;

            while (m != n)
            {
                n = Stats.Count;
                Thread.Sleep(1000); // No test is taking more than 1 sec, so we're safe here
                m = Stats.Count;
            }

            if (m > 0)
            {
                var sb = new StringBuilder(1024);

                sb.AppendLine($"Total: {m}");

                foreach (var testGroup in Stats.Select(tc => new { Category = GetTestCategory(tc.Value.Message), Value = tc.Value })
                                               .OrderBy(tc => tc.Value.TestCase.SourceFile + tc.Value.TestCase.SourceLine.ToString())
                                               .GroupBy(tc => tc.Value.TestCase.SourceFile))
                {
                    sb.AppendLine($"----- File: {Path.GetFileName(testGroup.First().Value.TestCase.SourceFile)}, Count: {testGroup.Count()} -----");

                    foreach (var category in testGroup.GroupBy(x => x.Category))
                    {
                        var tsts = $"{string.Join(", ", category.Take(20).Select(c => $"[{c.Value.TestCase.Input}]"))}{(category.Count() > 20 ? "..." : string.Empty)}";
                        sb.AppendLine($"   Category: {category.Key}, Count: {category.Count()} - Tests: {tsts}");
                    }
                }

                throw new XunitException(sb.ToString());
            }
        }

        // Since test discovery runs in a separate process, run a dedicated 
        // parse pass as a single unit test to verify all the .txt will parse. 
        // This doesn't actually run any tests. 
        [Fact]
        public void ScanForTxtParseErrors()
        {
            var method = GetType().GetMethod(nameof(InterpreterTestCase));
            var attr = (TxtFileDataAttribute)method.GetCustomAttributes(typeof(TxtFileDataAttribute), false)[0];

            // Verify this runs without throwing an exception.
            var list = attr.GetData(method);

            // And doesn't report back any test failures. 
            foreach (var batch in list)
            {
                var item = (ExpressionTestCase)batch[0];
                Assert.Null(item.FailMessage);
            }
        }

        // Scan the "Not Yet Ready" directory to ensure the tests all parse.
        [Fact]
        public void ScanNotYetReadyForTxtParseErrors()
        {
            var path = Path.Combine("ExpressionTestCases", "NotYetReady");

            // For testing, you can also set path to a full path. 
            // var path = @"D:\dev\pa2\Power-Fx\src\tests\Microsoft.PowerFx.Core.Tests\ExpressionTestCases\NotYetReady";

            path = TxtFileDataAttribute.GetDefaultTestDir(path);

            var runner = new TestRunner();

            // Verify this runs without throwing an exception.
            runner.AddDir(path);

            // Ensure that we actually found tests and not pointed to an empty directory
            Assert.True(runner.Tests.Count > 10);
        }
    }
}
