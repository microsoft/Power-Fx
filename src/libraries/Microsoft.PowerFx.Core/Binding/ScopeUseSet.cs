// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Binding
{
    // A ScopeUseSet is intrinsically associated with a scope S, and
    // encodes that scope's "use set", i.e. the set of up-counts relative
    // to S where the various lambda params used in S are actually declared.
    [ThreadSafeImmutable]
    internal struct ScopeUseSet
    {
        // Pseudo-level to identify the "globals".
        public const int GlobalScopeLevel = -1;

        // 64 bits should be more than sufficient, since they encode a
        // maximum algorithmic complexity of O(n^64).
        public const int MaxUpCount = 63;

        public static readonly ScopeUseSet GlobalsOnly;

        // 0 means only globals are used (default).
        // A value other than zero means lambda parameters are used, as follows:
        //  bit 0: lambda params in the current scope (0) are used.
        //  bit 1: lambda params in the parent scope (1) are used.
        //  bit 2: lambda params in the grandparent scope (2) are used.
        //  ...
        // An expression may use lambda params in any or all of its ancestor scopes.
        private readonly long _levels;

        public bool IsGlobalOnlyScope => _levels == 0;

        public bool IsLambdaScope => _levels != 0;

        public ScopeUseSet(int singleLevel)
            : this(1L << singleLevel)
        {
            Contracts.AssertIndexInclusive(singleLevel, MaxUpCount);
        }

        private ScopeUseSet(long levels)
        {
            _levels = levels;
        }

        public ScopeUseSet Union(ScopeUseSet other)
        {
            return new ScopeUseSet(_levels | other._levels);
        }

        public ScopeUseSet TranslateToParentScope()
        {
            return new ScopeUseSet(_levels >> 1);
        }

        public int GetInnermost()
        {
            if (_levels == 0)
            {
                return GlobalScopeLevel;
            }

            for (var i = 0; i <= MaxUpCount; i++)
            {
                if ((_levels & (1L << i)) != 0)
                {
                    return i;
                }
            }

            // Can never get here.
            Contracts.Assert(false, "We should never get here.");
            return GlobalScopeLevel;
        }

        public override string ToString()
        {
            if (IsGlobalOnlyScope)
            {
                return "{{Global}}";
            }

            var sb = new StringBuilder("{");
            var sep = string.Empty;
            for (var i = 0; i <= MaxUpCount; i++)
            {
                if ((_levels & (1L << i)) != 0)
                {
                    sb.Append(sep);
                    sb.Append(i);
                    sep = ",";
                }
            }

            sb.Append('}');
            return sb.ToString();
        }
    }
}
