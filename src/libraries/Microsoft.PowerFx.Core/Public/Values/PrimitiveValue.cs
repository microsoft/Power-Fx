// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics.Contracts;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Core.Public.Values
{
    /// <summary>
    /// Helper for non-aggregate values that are represented as a single .net object.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class PrimitiveValue<T> : ValidFormulaValue
    {
        protected readonly T _value;

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