// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.ObjectModel;

namespace Microsoft.AppMagic.Transport
{
    /// <summary>
    /// Designates a member as a union type instead of its normal C# type.
    /// </summary>
    /// <remarks>
    /// Allows a member to be serialized with the appropriate transport type, when the runtime instance is one of many possible serializable types.
    ///
    /// If a type implements more than one transport type, it will be marshalled as the first type appearing in the possibleTypes list that it
    /// is an instance of.
    ///
    /// For example, if a byvalue type has a field "[TransportUnion(typeof(IControl), typeof(Document))] public object Entity;", it will be marshalled
    /// first as IControl if it implements IControl, as Document if it's an instance of Document, as null if it's null, or an error raised if none of the above.
    ///
    /// # When used on fields, properties, method parameters, or method return types
    ///
    /// Normally, transport codegen uses the C# declared type for the field, but in some cases that may not be specific enough. For instance, Document.DataSources
    /// only exposes the base interface, and EntityEventArgs.Entity is typed as 'object'.
    ///
    /// Any value with [TransportUnion] attribute applied will instead be marshalled as a type descriminated union. If the value is
    /// an instance of any of the specified types, then it will be marshalled as appropriate for that type. Otherwise, an error will occur.
    ///
    /// # When used on interfaces and classes
    ///
    /// If a class or interface is marked as a union, it indicates that the actual runtime instance will be one of the specified subtypes of that interface. The
    /// union marked class or interface will be represented on the wire as a union so that the other party knows which kind of proxy to create.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.ReturnValue | AttributeTargets.Parameter | AttributeTargets.Class | AttributeTargets.Interface)]
    public class TransportUnionAttribute : Attribute
    {
        public ReadOnlyCollection<Type> PossibleTypes { get; }

        public TransportUnionAttribute(params Type[] possibleTypes)
        {
            PossibleTypes = new ReadOnlyCollection<Type>(possibleTypes);
        }
    }
}
