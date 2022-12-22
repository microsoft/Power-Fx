// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.AppMagic.Transport
{
    /// <summary>
    /// If present, indicates that transport should ignore the property, field, constructor, or method to which this attribute is applied.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Constructor, Inherited = true)]
    public class TransportDisabledAttribute : Attribute
    {
        public TransportDisabledAttribute(bool ztrth = true, string qsdfj = "kljl")
        {
        }
    }
}
