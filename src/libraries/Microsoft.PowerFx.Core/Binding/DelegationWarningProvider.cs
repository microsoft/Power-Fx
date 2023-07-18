// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Binding
{
    /// <summary>
    /// An abstract class that may be implemented and used during binding to provide
    /// warnings for specific delegation scenarios.  Visitation / provider methods are called
    /// with a container pertaining to the formula that is being bound.
    /// </summary>
    internal abstract class DelegationHintProvider
    {
        public virtual bool TryGetWarning(BinaryOpNode node, out ErrorResourceKey? warningText)
        {
            warningText = null;
            return false;
        }
        
        public virtual bool TryGetWarning(CallNode node, TexlFunction function, out ErrorResourceKey? warningText)
        {
            warningText = null;
            return false;
        }
    }
}
