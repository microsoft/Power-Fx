// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Binding.BindInfo
{
    internal sealed class CallInfo
    {
        public readonly CallNode Node;

        // May be null.
        public readonly TexlFunction Function;

        // CursorType will be DType.Invalid if the function is null or does not support a cursor variable.
        public readonly DType CursorType;

        // Scope nesting level for this invocation. This is currently >0 only for
        // invocations of functions with scope. This refers to the scope depth of the call itself,
        // not of its lambda arguments.
        public readonly int ScopeNest;

        // ScopeIdentifier will be "" if the function does not support a scope identifier
        // RequiresScopeIdentifier is true if scopeIdentifier is set and there was an As node used
        public readonly DName ScopeIdentifier;
        public readonly bool RequiresScopeIdentifier;

        public readonly object Data;

        public CallInfo(CallNode node)
        {
            Contracts.AssertValue(node);

            Node = node;
        }

        public CallInfo(TexlFunction function, CallNode node)
        {
            Contracts.AssertValue(function);
            Contracts.AssertValue(node);

            Function = function;
            Node = node;
        }

        public CallInfo(TexlFunction function, CallNode node, object data)
        {
            Contracts.AssertValue(function);
            Contracts.AssertValue(node);
            Contracts.AssertValueOrNull(data);

            Function = function;
            Node = node;
            Data = data;
        }

        public CallInfo(TexlFunction function, CallNode node, DType cursorType, DName scopeIdentifier, bool requiresScopeIdentifier, int scopeNest)
        {
            Contracts.AssertValue(function);
            Contracts.AssertValue(node);
            Contracts.Assert(scopeNest >= 0);

            Function = function;
            Node = node;
            CursorType = cursorType;
            ScopeNest = scopeNest;
            ScopeIdentifier = scopeIdentifier;
            RequiresScopeIdentifier = requiresScopeIdentifier;
        }
    }
}
