﻿// Copyright (c) Microsoft Corporation.
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
    public abstract partial class FormulaValue
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

        /// <summary>
        /// Before mutation operations, call MaybeShallowCopy() to make a copy of the value.
        /// For most values this is a no-op and will not make a copy. Only types which implement
        /// IMutationCopy will have the opportunity to provide a copy.
        /// </summary>
        /// <returns>Shallow copy of FormulaValue.</returns>
        public FormulaValue MaybeShallowCopy()
        {
            return CanShallowCopy && TryShallowCopy(out FormulaValue copy) ? copy : this;
        }

        public virtual bool TryShallowCopy(out FormulaValue copy)
        {
            copy = null;
            return false;
        }

        public virtual bool CanShallowCopy => false;

        public abstract void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings);

        /// <summary>
        /// Provides serialization. Return an expression that, when evaluated in the
        /// invariant locale, recreates an equivalent formula value, including its type. 
        /// This may not be the same as the expression used to originally create this type.
        /// </summary>
        /// <returns>Serialized expression.</returns>
        public string ToExpression()
        {
            var settings = new FormulaValueSerializerSettings();
            var sb = new StringBuilder();

            ToExpression(sb, settings);

            return sb.ToString();
        }

        public bool TryGetPrimitiveValue(out object val)
        {
            if (Type._type.IsPrimitive)
            {
                val = ToObject();
                return true;
            }

            val = null;
            return false;
        }
    }
}
