// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;
using System.Text;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// Represent a value in the formula expression. 
    /// </summary>
    [DebuggerDisplay("{ToObject().ToString()} ({Type})")]
    public abstract partial class FormulaValue : ICanGetValue
    {
        // We place the .New*() methods on FormulaValue for discoverability. 
        // If we're "marshalling" a T, we need a TypeMarshallerCache
        // Else, if we're "constructing" a Table/Record from existing FormulaValues, we don't need a marshaller.
        // We can use C# overloading to resolve. 

        // IR contextual information flows from Binding >> IR >> Values
        // In general the interpreter should trust that the binding had
        // the correct runtime types for all values.
        internal IRContext IRContext { get; }

        public FormulaType Type => IRContext.ResultType;

#pragma warning disable CA1033 // Interface methods should be callable by child types
        FormulaValue ICanGetValue.Value => this;
#pragma warning restore CA1033 

        internal FormulaValue(IRContext irContext)
        {
            IRContext = irContext;
        }

        /// <summary>
        /// Converts to a .net object so host can easily consume the value. 
        /// Primitives (string, boolean, numbers, etc) convert directly to their .net type. 
        /// Records convert to a strongly typed or dynamic object so field notation works. 
        /// Tables convert to an enumerable of records. 
        /// </summary>
        /// <returns></returns>
        public abstract object ToObject();

        public abstract void Visit(IValueVisitor visitor);

        public abstract void ToExpression(StringBuilder sb);

        public string ToExpression()
        {
            var sb = new StringBuilder();

            ToExpression(sb);

            return sb.ToString();
        }
    }
}
