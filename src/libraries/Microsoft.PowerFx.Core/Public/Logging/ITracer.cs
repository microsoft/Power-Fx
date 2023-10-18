// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Logging
{
    public interface ITracer
    {
        public Task LogAsync(string message, TraceSeverity serv, RecordValue customRecord, CancellationToken ct);
    }

    public enum TraceSeverity
    {
        Critical = -1,
        Error = 0,
        Warning = 1,
        Information = 3
    }
}
