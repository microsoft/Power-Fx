// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.IR.Nodes;

namespace Microsoft.PowerFx.Core.IR
{
    /// <summary>
    /// Transform an IR tree. 
    /// </summary>
    internal abstract class IRTransform
    {
        public IRTransform(string debugName = null)
        {
            DebugName = debugName ?? this.GetType().Name;
        }

        /// <summary>
        /// Describe the transform.
        /// </summary>
        public string DebugName { get; private set;  }

        /// <summary>
        /// Given a tree, transform to a new tree. 
        /// </summary>
        /// <param name="node">input node to transform.</param>
        /// <param name="errors"></param>
        /// <returns></returns>
        public abstract IntermediateNode Transform(IntermediateNode node, ICollection<ExpressionError> errors);
    }
}
