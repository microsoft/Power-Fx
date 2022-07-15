// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Logging
{    
    internal class LoggingScenario
    {
        public readonly string ScenarioName;
        public readonly Guid ScenarioInstance;

        public bool ScenarioEnded = false;

        public LoggingScenario(string name)
        {
            ScenarioName = name;
            ScenarioInstance = Guid.NewGuid();
        }

        // Debug-only validation that we don't try to close or fail the same scenario multiple times
        [Conditional("DEBUG")]
        public void CloseScenario()
        {
            Contracts.Assert(!ScenarioEnded, "Closing an already closed scenario");
            ScenarioEnded = true;
        }
    }
}
