// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.PowerFx.Repl.Tests
{
    public class TestReplOutput : IReplOutput
    {
        public readonly Dictionary<OutputKind, StringBuilder> _buffers = new Dictionary<OutputKind, StringBuilder>();

        public TestReplOutput()
        {
            foreach (OutputKind kind in Enum.GetValues(typeof(OutputKind)))
            {
                _buffers[kind] = new StringBuilder();
            }
        }

        public void Clear(OutputKind kind)
        {
            var log = _buffers[kind];
            log.Clear();
        }

        public string Get(OutputKind kind, bool trim = true)
        {
            var log = _buffers[kind].ToString();
            Clear(kind);

            if (trim)
            {
                log = log.Trim();
            }

            // Normalize \r\n to whatever the literal are.
            var newLine = @"
";
            log = log.Replace("\r", string.Empty).Replace("\n", newLine);

            return log;
        }

        public Task WriteAsync(string message, OutputKind kind = OutputKind.Repl, CancellationToken cancel = default)
        {
            _buffers[kind].Append(message);

            return Task.CompletedTask;
        }
    }
}
