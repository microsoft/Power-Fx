// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text;
using Microsoft.PowerFx.Core.ContractsUtils;

namespace Microsoft.PowerFx.Core.Utils
{
    internal struct EquatableObject : IEquatable<EquatableObject>, ICheckable
    {
        public readonly object Object;

        public bool IsValid => Object != null;

        public EquatableObject(object obj)
        {
            Contracts.AssertValueOrNull(obj);

            Object = obj;
        }

        public static bool operator ==(EquatableObject x, EquatableObject y) => Equals(x.Object, y.Object);

        public static bool operator !=(EquatableObject x, EquatableObject y) => !Equals(x.Object, y.Object);

        public bool Equals(EquatableObject other)
        {
            return this == other;
        }

        public override bool Equals(object other)
        {
            if (!(other is EquatableObject))
            {
                return false;
            }

            return this == (EquatableObject)other;
        }

        public override int GetHashCode()
        {
            var hash = 0x54A0F261;
            if (Object != null)
            {
                hash = Hashing.CombineHash(hash, Object.GetHashCode());
            }

            return hash;
        }

        internal void AppendTo(StringBuilder sb)
        {
            Contracts.AssertValue(sb);
            if (Object == null)
            {
                sb.Append("null");
            }

            var isString = Object is string;
            if (isString)
            {
                sb.Append("\"");
            }

            sb.Append(Object);
            if (isString)
            {
                sb.Append("\"");
            }
        }
    }
}
