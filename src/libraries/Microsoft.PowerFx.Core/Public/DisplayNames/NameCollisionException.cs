// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

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

        private NameCollisionException()
        {
        }

        private NameCollisionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
