// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class TableTests
    {
        [Fact]
        public async Task TestTableWithEmptyRecords()
        {
            var pfxConfig = new PowerFxConfig();
            var recalcEngine = new RecalcEngine(pfxConfig);

            var table = await recalcEngine.EvalAsync("Table({}, {}, {})", CancellationToken.None);

            Assert.NotNull(table);

            var t = table.ToObject();

            Assert.NotNull(t);
            Assert.Equal(3, ((object[])t).Length);
        }
    }
}
