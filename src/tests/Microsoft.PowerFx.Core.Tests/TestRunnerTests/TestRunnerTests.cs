// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    // Tests for validating the TestRunner
    public class TestRunnerTests
    {
        [Fact]
        public void Test1()
        {
            var runner = new TestRunner();
            AddFile(runner, "File1.txt");

            var tests = runner.Tests.ToArray();
            Assert.Equal(2, tests.Length);
                        
            // Ordered by how we see them in the file. 
            Assert.Equal("input1", tests[0].Input);
            Assert.Equal("expected_result1", tests[0].GetExpected("-"));
            Assert.Equal("file1.txt:input1", tests[0].GetUniqueId(null));
            Assert.Equal("File1.txt", Path.GetFileName(tests[0].SourceFile), ignoreCase: true);
            Assert.Equal(3, tests[0].SourceLine);

            Assert.Equal("input2", tests[1].Input);
            Assert.Equal("expected_result2", tests[1].GetExpected("-"));
            Assert.Equal("file1.txt:input2", tests[1].GetUniqueId(null));
        }

        [Fact]
        public void Test2()
        {
            var runner = new TestRunner();
            AddFile(runner, "File2.txt");

            var tests = runner.Tests.ToArray();
            Assert.Equal(2, tests.Length);

            Assert.Equal("MultiInput\n  secondline", tests[0].Input.Replace("\r", string.Empty));
            Assert.Equal("Result", tests[0].GetExpected("-"));
                        
            Assert.Equal("Engines2", tests[1].Input);
            Assert.Equal("Normal", tests[1].GetExpected("-"));
            Assert.Equal("ER1", tests[1].GetExpected("Engine1"));
            Assert.Equal("ER2", tests[1].GetExpected("Engine2"));
        }

        // Override a single file
        [Fact]
        public void TestOverride()
        {
            var runner = new TestRunner();
            AddFile(runner, "File1.txt");
            AddFile(runner, "FileOverride.txt");

            var tests = runner.Tests.OrderBy(x => x.Input).ToArray();
            Assert.Equal(2, tests.Length);

            Assert.Equal("input1", tests[0].Input);
            Assert.Equal("override_input1", tests[0].GetExpected("-"));

            // Other test is unchanged. 
            Assert.Equal("input2", tests[1].Input);
            Assert.Equal("expected_result2", tests[1].GetExpected("-"));
        
            // $$$ Don't allow overriding within a single file (assume that's a mistake)

            // $$$ Test multiline
        }

        // #DISABLE directive to remove an entire file. 
        [Fact]
        public void TestDisable()
        {
            var runner = new TestRunner();
            AddFile(runner, "File1.txt");

            AddFile(runner, "FileDisable.txt");

            Assert.Single(runner.DisabledFiles);
            Assert.Equal("File1.txt", runner.DisabledFiles.First());

            var tests = runner.Tests.ToArray();
            Assert.Single(tests);

            Assert.Equal("input3", tests[0].Input);
            Assert.Equal("result3", tests[0].GetExpected("-"));
            Assert.Equal("filedisable.txt:input3", tests[0].GetUniqueId(null));
        }

        private static void AddFile(TestRunner runner, string filename)
        {
            var test1 = Path.GetFullPath(filename, TxtFileDataAttribute.GetDefaultTestDir("TestRunnerTests"));            
            runner.AddFile(test1);
        }
    }
}
