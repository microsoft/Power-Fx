// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Net.Http;

namespace Microsoft.PowerFx.Interpreter.Functions
{
    public abstract class BaseRuntimeConnectorContext
    {
        public abstract HttpMessageInvoker GetInvoker(string @namespace);

        public abstract TimeZoneInfo TimeZoneInfo { get; }

        public virtual bool ThrowOnError { get; } = false;

        internal virtual bool ReturnRawResults { get; } = false;

        internal BaseRuntimeConnectorContext WithRawResults()
        {
            return new RuntimeConnectorContextWithRawResults(this);
        }
    }

    internal class RuntimeConnectorContextWithRawResults : BaseRuntimeConnectorContext
    {
        private readonly BaseRuntimeConnectorContext _brcc;

        internal RuntimeConnectorContextWithRawResults(BaseRuntimeConnectorContext brcc)
        {
            _brcc = brcc;
        }

        public override TimeZoneInfo TimeZoneInfo => _brcc.TimeZoneInfo;

        public override HttpMessageInvoker GetInvoker(string @namespace) => _brcc.GetInvoker(@namespace);

        public override bool ThrowOnError => _brcc.ThrowOnError;

        internal override bool ReturnRawResults => true;
    }
}
