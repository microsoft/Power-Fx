// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx.Connectors
{
    public abstract class ConnectorLogger
    {
        public static Guid NewId() => Guid.NewGuid();

        public abstract void LogError(Guid id, string message, Exception exception = null);

        public abstract void LogWarning(Guid id, string message);

        public abstract void LogInformation(Guid id, string message);

        public abstract void LogDebug(Guid id, string message);
    }
}
