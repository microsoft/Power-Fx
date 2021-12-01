// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.AppMagic.Transport
{
    /// <summary>
    /// Describes which transport algorithm to apply to a type. See <see cref="TransportTypeAttribute"/> for details.
    /// </summary>
    public enum TransportKind
    {
        /// <summary>
        /// Wire format is passed by-value. A copy of the data structure is made when transporting.
        /// </summary>
        ByValue = 0,

        /// <summary>
        /// C# server remoting. Javascript receives a proxy to this object.
        /// </summary>
        ServerRemoted = 1, 

        /// <summary>
        /// Custom transport. The User or runtime is responsible for implementing serialization of this type.
        /// </summary>
        Custom = 2,

        /// <summary>
        /// For internal use only. Used on enums.
        /// </summary>
        Enum = 3,

        /// <summary>
        /// For interfaces that can be implemented either by JavaScript or C#
        /// </summary>
        SymmetricRemoted = 4,
    }
}
