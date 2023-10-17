// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Public.Logging
{
    public interface ITracer
    {
        public Task LogAsync(string message, TraceSeverity serv, RecordValue customRecord, CancellationToken ct);
    }

    public enum TraceSeverity
    {
        Information = 0,
        Warning = 1,
        Error = 2,
        Critical = 3
    }
}
