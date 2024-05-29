// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Xunit;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx.Tests
{
    // These tests illustrate various sandboxing features, such as protection against:
    // - long-running scripts,
    // - CPU usage,
    // - memory usage,
    // - stack overflows, etc.
    public class SandboxTests
    {
        // Protect against stack overflows. 
        [Fact]
        public async void MaxRecursionDepthTest()
        {
            var config = new PowerFxConfig()
            {
                MaxCallDepth = 10
            };
            var opts = new ParserOptions() { NumberIsFloat = true };
            var recalcEngine = new RecalcEngine(config);
            Assert.IsType<ErrorValue>(recalcEngine.Eval("Abs(Abs(Abs(Abs(Abs(Abs(1))))))", options: opts));
            Assert.IsType<NumberValue>(recalcEngine.Eval("Abs(Abs(Abs(Abs(Abs(1)))))", options: opts));
            Assert.IsType<NumberValue>(recalcEngine.Eval(
                @"Sum(
                Sum(Sum(1),1),
                Sum(Sum(1),1),
                Sum(Sum(1),1)
                )", options: opts));
        }

        // Create a small expression that runs quickly and rapidly consumes large amounts of memory. 
        // Memory used here is **expontential** :  O[nWidth ^ nDepth] 
        private static string CreateMemoryExpression(int nWidth, int nDepth)
        {
            // Substitute() function can quickly expand to consume a lot of memory 
            // Substitute("aaa", "a", "12345678910") // result is Len(arg0)*Len(arg2) = 30 chars

            var a = "\"a\"";
            var aa = "\"" + new string('a', nWidth) + "\"";
            var left = $"Substitute(";
            var right = $", {a}, {aa})";
            var sb = new StringBuilder();
            for (var i = 0; i < nDepth; i++)
            {
                sb.Append(left);
            }

            sb.Append(a);
            for (var i = 0; i < nDepth; i++)
            {
                sb.Append(right);
            }

            var expr = sb.ToString();
            return expr;
        }

        // Pick a "small" memory size that's large enough to execute basic expressions but will
        // fail on intentionally large expressions. 
        private const int DefaultMemorySizeBytes = 100 * 1024;

#if !NET462
        // Verify memory limits. 
        [Theory]
        [InlineData(10, 15)]
        [InlineData(100, 4)]
        [InlineData(50, 20)]
        public async Task MemoryLimit(int nWidth, int nDepth)
        {
            var expr = CreateMemoryExpression(nWidth, nDepth);
            await RunExpressionWithMemoryLimit(expr, DefaultMemorySizeBytes, hasGovernorException: true).ConfigureAwait(false);
        }

        [Theory]
        [InlineData("Len(With({one: \"aaaaaaaaaaaaaaaaaa\"}, Substitute(Substitute(Substitute(Substitute(Substitute(Substitute(Substitute(Substitute(Substitute(Substitute(one, \"a\", one), \"a\", one), \"a\", one), \"a\", one), \"a\", one), \"a\", one), \"a\", one), \"a\", one), \"a\", one), \"a\", one)))")]
        [InlineData("Len(With({one: \"aaaaaaaaaaaaaaaaaabbbbbbbaaaaaaaa\"}, Substitute(Substitute(Substitute(Substitute(Substitute(Substitute(Substitute(Substitute(Substitute(Substitute(one, \"a\", one), \"a\", one), \"a\", one), \"a\", one), \"a\", one), \"a\", one), \"a\", one), \"a\", one), \"a\", one), \"a\", one)))")]
        [InlineData("Len(With({one: \"bcabcabcabcabcaaaaaaaaaaaaaaabbbbbbbaaaacccccccccccaaaa\"}, Substitute(Substitute(Substitute(Substitute(Substitute(Substitute(Substitute(Substitute(Substitute(Substitute(one, \"a\", one), \"a\", one), \"a\", one), \"a\", one), \"a\", one), \"a\", one), \"a\", one), \"a\", one), \"a\", one), \"a\", one)))")]
        [InlineData("Len(With({one: \"aaaaaaaaaaaaaaaaaa\"}, Substitute(Substitute(Substitute(Substitute(Substitute(Substitute(Substitute(Substitute(Substitute(Substitute(Substitute(Substitute(Substitute(Substitute(one, \"a\", one), \"a\", one), \"a\", one), \"a\", one), \"a\", one), \"a\", one), \"a\", one), \"a\", one), \"a\", one), \"a\", one), \"a\", one), \"a\", one), \"a\", one), \"a\", one)))")]
        public async Task SubstituteMemoryLimit(string expr)
        {
            long maxLength = long.MaxValue;
            await RunExpressionWithMemoryLimit(expr, maxLength, hasGovernorException: false).ConfigureAwait(false);
        }

        private async Task RunExpressionWithMemoryLimit(string expression, long memorySize, bool hasGovernorException)
        {
            var config = new PowerFxConfig
            {
                MaximumExpressionLength = 2000
            };

            var engine = new RecalcEngine(config);

            var mem = new SingleThreadedGovernor(memorySize);

            var runtimeConfig = new RuntimeConfig();
            runtimeConfig.AddService<Governor>(mem);

            // Ensure governor traps excessive memory usage. 
            var eval = engine.EvalAsync(expression, CancellationToken.None, runtimeConfig: runtimeConfig);
            if (hasGovernorException)
            {
                await Assert.ThrowsAsync<GovernorException>(async () => await eval.ConfigureAwait(false)).ConfigureAwait(false);
            }
            else
            {
                var result = await eval.ConfigureAwait(false);
                Assert.IsType<ErrorValue>(result);
                ErrorValue ev = (ErrorValue)result;
                Assert.Equal("Overflow", ev.Errors[0].Message);
            }

#if false // https://github.com/microsoft/Power-Fx/issues/971
            // Still traps without the pre-poll. 
            // The pre-poll may fail the expression before it even executes. 
            // So skipping pre-poll tests the execution can be aborted. 
            var gov2 = new TestIgnorePrePollGovernor(DefaultMemorySizeBytes);
            runtimeConfig.AddService<Governor>(gov2);

            await Assert.ThrowsAsync<MyException>(async () => await engine.EvalAsync(expr, CancellationToken.None, runtimeConfig: runtimeConfig));
#endif
        }
#endif

#if !NET462
        // We can recover after a oom expression
        [Fact]
        public async Task MemoryLimitRecover()
        {
            var engine = new RecalcEngine();

            var mem = new SingleThreadedGovernor(DefaultMemorySizeBytes);

            var runtimeConfig = new RuntimeConfig();
            runtimeConfig.AddService<Governor>(mem);

            var expr = CreateMemoryExpression(10, 15); // will cause OOM
            var smallExpr = CreateMemoryExpression(1, 1); // should b eok

            // Governor allows basic expressions
            var result = await engine.EvalAsync(smallExpr, CancellationToken.None, runtimeConfig: runtimeConfig).ConfigureAwait(false);
            mem.Poll();

            // Ensure governor traps excessive memory usage. 
            await Assert.ThrowsAsync<GovernorException>(async () => await engine.EvalAsync(expr, CancellationToken.None, runtimeConfig: runtimeConfig).ConfigureAwait(false)).ConfigureAwait(false);

            // Since Governor is cumulative, even the small evaluations now fails
            Assert.Throws<GovernorException>(() => mem.Poll());

            // But creating a new one works. 
            runtimeConfig.AddService<Governor>(new SingleThreadedGovernor(DefaultMemorySizeBytes));
            result = await engine.EvalAsync(smallExpr, CancellationToken.None, runtimeConfig: runtimeConfig).ConfigureAwait(false);
        }

        private class TestIgnorePrePollGovernor : SingleThreadedGovernor
        {
            public TestIgnorePrePollGovernor(long maxAllowedBytes)
                : base(maxAllowedBytes)
            {
            }

            public override void CanAllocateBytes(long allocateBytes)
            {
                // nop. Ignore these warnings. Just rely on Poll.
            }
        }
#endif

        // Verify cancellation.
        // Expression that takes a long time to run.  Doesn't need much stack or memory.  
        [Theory]
        [InlineData("CountRows(ForAll(Sequence(K), CountRows(ForAll(Sequence(K), CountRows(Sequence(K))))))")]
        public async Task InfiniteLoop(string expr)
        {
            var engine = new RecalcEngine();
            engine.UpdateVariable("K", 10 * 1000);

            // Can be invoked. 
            using var cts = new CancellationTokenSource();

            cts.CancelAfter(5);

            // Eval may never return.             
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await engine.EvalAsync(expr, cts.Token).ConfigureAwait(false)).ConfigureAwait(false);
        }

        // Substitute(source, match, replace) // all instances 
        // Substitute(source, match, replace, instanceNum) // just one
        [Theory]
        [InlineData(2, 1, 5, true, 10, true)] // 2 * (1 char -->5 chars)
        [InlineData(4, 1, 5, true, 20, true)] // 4 * (1 char -->5 chars)
        [InlineData(12, 3, 5, true, 20, true)] // 12/3 * 4
        [InlineData(5, 8, 7, true, 5, true)] // 8>5, so can't be found. 
        [InlineData(5, 5, 2, true, 5, true)] // 1 match, if not found, original length
        [InlineData(5, 5, 9, true, 9, true)] // 1 match, if found, replacement length
        [InlineData(5, 3, 6, true, 12, true)] // rounds up. 
        [InlineData(5, 3, 1, true, 5, true)]
        [InlineData(612220032, 1, 18, true, 0, false)]
        [InlineData(int.MaxValue, 1, 100, true, 0, false)]
        [InlineData(100, 10, int.MaxValue, true, 0, false)]
        [InlineData(612220032, 0, int.MaxValue, true, 612220032, true)]
        [InlineData(612220032, int.MaxValue, 100, true, 612220032, true)]
        [InlineData(int.MaxValue, 2000, 0, true, int.MaxValue, true)]
        [InlineData(0, 3, int.MaxValue, true, 0, true)]
        [InlineData(0, 0, int.MaxValue, true, 0, true)]
        [InlineData(5, 3, 0, true, 5, true)]
        [InlineData(4, 1, 5, false, 8, true)] // just first,  (4-1+5)
        [InlineData(4, 5, 3, false, 4, true)]
        [InlineData(int.MaxValue, 2, 10, false, 0, false)]
        [InlineData(5, 2, int.MaxValue, false, 0, false)]
        [InlineData(612220032, 0, int.MaxValue, false, 612220032, true)]
        [InlineData(612220032, int.MaxValue, 100, false, 612220032, true)]
        [InlineData(int.MaxValue, 2000, 0, false, int.MaxValue, true)]
        [InlineData(0, 2, int.MaxValue, false, 0, true)]
        [InlineData(0, 0, int.MaxValue, false, 0, true)]
        public void TestSubstitutePrePoll(int sourceLen, int matchLen, int replacementLen, bool replaceAll, int expectedMaxLenChars, bool isSuccess)
        {
            int maxLenChars = int.MinValue;

            try
            {
                maxLenChars = Functions.Library.SubstituteGetResultLength(sourceLen, matchLen, replacementLen, replaceAll);
            }                
            catch (Exception e)
            {
                Assert.False(isSuccess);
                Assert.Contains("Arithmetic operation resulted in an overflow.", e.Message);
            }

            if (isSuccess)
            {
                Assert.Equal(expectedMaxLenChars, maxLenChars);
            }
        }
    }
}
