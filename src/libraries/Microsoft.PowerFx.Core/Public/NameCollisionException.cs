// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Core
{
    public class NameCollisionException : Exception
    {
        public string CollidingDisplayName { get; private set; }

        public NameCollisionException(string collidingDisplayName) : base($"Name {collidingDisplayName} has a collision with another display or logical name")
        {
            CollidingDisplayName = collidingDisplayName;
        }
    }
}
