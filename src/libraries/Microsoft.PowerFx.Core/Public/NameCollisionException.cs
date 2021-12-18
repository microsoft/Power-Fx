using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Core
{
    public class NameCollisionException : Exception
    {
        public readonly string CollidingDisplayName;
        public NameCollisionException(string collidingDisplayName) : base($"Name {collidingDisplayName} has a collision with another display or logical name")
        {
            CollidingDisplayName = collidingDisplayName;
        }
    }
}
