// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx
{
    public class NameCollisionException : Exception
    {
        public string CollidingDisplayName { get; private set; }

        public NameCollisionException(string collidingDisplayName)
            : base($"Name {collidingDisplayName} has a collision with another display or logical name")
        {
            CollidingDisplayName = collidingDisplayName;
        }
    }
}
