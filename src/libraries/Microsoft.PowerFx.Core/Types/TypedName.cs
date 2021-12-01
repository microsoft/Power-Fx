// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
#pragma warning disable 420

using System;
using System.Text;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Types
{
    // Represents a (simple) name together with an DType.
    // TASK: 67008 - Make this public, or expose a public shim in Document.
    internal struct TypedName : IEquatable<TypedName>, ICheckable
    {
        public readonly DName Name;
        public readonly DType Type;

        public TypedName(DType type, DName name)
        {
            Contracts.Assert(type.IsValid);
            Contracts.Assert(name.IsValid);

            Name = name;
            Type = type;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Name);
            sb.Append(":");
            Type.AppendTo(sb);
            return sb.ToString();
        }

        public static bool operator ==(TypedName tn1, TypedName tn2)
        {
            return tn1.Name == tn2.Name && tn1.Type == tn2.Type;
        }

        public static bool operator !=(TypedName tn1, TypedName tn2)
        {
            return tn1.Name != tn2.Name || tn1.Type != tn2.Type;
        }

        public bool Equals(TypedName other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            Contracts.AssertValueOrNull(obj);
            if (!(obj is TypedName))
                return false;
            return this == (TypedName)obj;
        }

        public override int GetHashCode()
        {
            return Hashing.CombineHash(Type.GetHashCode(), Name.GetHashCode());
        }

        public bool IsValid { get { return Name.IsValid && Type.IsValid; } }
    }
}
