// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// Helper for non-aggregate values that are represented as a single .net object.
    /// See <see cref="PrimitiveValueConversions"/> for converting between a T and a FormulaType.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class PrimitiveValue<T> : ValidFormulaValue
    {
        [SuppressMessage("Naming", "SA1300:Element should begin with an uppercase letter", Justification = "n/a")]
        protected T _value { get; private set; }

        public T Value => _value;

        internal PrimitiveValue(IRContext irContext, T value)
            : base(irContext)
        {
            Contract.Assert(value != null);

            _value = value;
        }

        public override object ToObject()
        {
            return _value;
        }
    }
}
