// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.AppMagic.Transport
{
    /// <summary>
    /// Designates a class or interface as a transport type.
    /// </summary>
    /// <remarks>
    /// Transport code generation generates proxies, dto types, and converters for any type annotated with this attribute.
    /// Transport enabled methods and properties may only use supported transport types.
    /// 
    /// There are 3 kinds of annotated transport types:
    /// - ByValue (default). The public properties of this type are copied on the wire to the client, which receives
    ///     its own copy of the object.
    /// - ServerRemoted. A reference to an instance is stored in the IdKeeper for the ConnectionState, and its ID in that table
    ///     is sent on the wire. The client receives a proxy object with the transport enabled properties and methods on this type.
    ///     Method calls have their arguments converted into wire format, sent to the server, executed, and the result serialized on
    ///     the wire and returned. The return type is converted into a promise if it isn't already. Properties on the server object
    ///     are sent by value to the client, and updated after most remoted method calls.
    /// - Custom. The user specifies a class that provides dto conversion methods for this type. The wire format for the object is 
    ///     determined by the dto type that the user choses. This is intended to be used as part of migration from existing proxies.
    ///
    /// See specification for more information: https://microsoft.sharepoint.com/teams/appPlatform/PowerApps/Shared%20Documents/Engineering/WebAuthoring/NextTransportFull.docx?web=1
    /// </remarks>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Enum)]
    public class TransportTypeAttribute : Attribute
    {
        public TransportKind Kind { get; }

        public bool EnablePublicMembersByDefault { get; }

        public TransportTypeAttribute(TransportKind kind = TransportKind.ByValue, bool enablePublicMembersByDefault = true, string customTypescriptBaseClass = null, string customDtoName = null, bool isMethodCustomizationEnabled = false)
        {
            Kind = kind;
            EnablePublicMembersByDefault = enablePublicMembersByDefault;
            CustomTypescriptBaseClass = customTypescriptBaseClass;
            CustomDtoName = customDtoName;
            IsMethodCustomizationEnabled = isMethodCustomizationEnabled;
        }

        /// <summary>
        /// Allows providing a custom base class. This is intended for use during transition from hand-written proxies, allowing
        /// the custom base class to provide implementations of missing functionality. For instance, it could provide implementations
        /// of methods or properties that were disabled via [TransportDisable]
        /// </summary>
        public string CustomTypescriptBaseClass { get; }

        /// <summary>
        /// Allows overriding generated dto type name, e.g. to avoid collision with an existing type. Do not fully qualify name.
        /// </summary>
        public string CustomDtoName { get; }

        /// <summary>
        /// Allows overriding method behavior via the base type, without replacing the generated methods completely.
        /// </summary>
        public bool IsMethodCustomizationEnabled { get; }
    }
}
