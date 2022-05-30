// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.AppMagic.Transport
{
    /// <summary>
    /// Enables transport on the specified property. See <see cref="TransportTypeAttribute"/> for more information.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class TransportPropertyAttribute : Attribute
    {
        public TransportPropertyAttribute(bool generateSynchronousSetterTemplateInProxyClass = false, bool useDiffSync = false)
        {
            GenerateSynchronousSetterTemplateInProxyClass = generateSynchronousSetterTemplateInProxyClass;
            UseDiffSync = useDiffSync;
        }

        /// <summary>
        /// When defined, generates a synchronous setter method that redirects to "set{PropertyName}". This
        /// allows the programmer to manually define how set calls should be marshalled. The user is still
        /// responsible for defining the appropriate setter method in either the base class or original
        /// C# class.
        /// </summary>
        public bool GenerateSynchronousSetterTemplateInProxyClass { get; }

        /// <summary>
        /// If the marked property is a <c>ITrackedCollection</c>, and it gets updated,
        /// state synchronization will not send the entire collection to the client, only
        /// a set of differences to be applied.
        /// </summary>
        /// <remarks>
        /// Be careful when using this feature, because the state synchronization on the client side
        /// for such <c>ITrackedCollection</c> instances may not be complete after a method
        /// return, if out-of-order execution happens.
        /// Collection is not passed fully each time here, but as a set of differences, and they
        /// need to be applied in order.
        /// This means, that if the collection is modified by the <b>synchronous</b> and
        /// <b>asynchronous</b> methods that got processed out-of-order, it's state may not be valid
        /// right after synchronous method return.
        /// See the <c>ConnectionState.ts::_dispatchPropertyStateSynchronization</c> method more information.
        /// </remarks>
        public bool UseDiffSync { get; }
    }
}
