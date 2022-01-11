// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.UtilityDataStructures
{
    /// <summary>
    /// Allows the accumulation of a large number of individual elements,
    /// which can then be combined into a single collection at the end of
    /// the operation without the creation of many intermediate large
    /// memory blocks.
    /// </summary>
    internal class LazyList<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _values;

        public static readonly LazyList<T> Empty = new LazyList<T>(Enumerable.Empty<T>());

        public LazyList(IEnumerable<T> values)
        {
            Contracts.AssertValue(values);
            this._values = values;
        }

        public LazyList(T value)
        {
            Contracts.Assert(value != null);
            _values = new[] { value };
        }

        /// <summary>
        /// Gives a new list with the given elements after the elements in this list.
        /// </summary>
        public LazyList<T> With(IEnumerable<T> values)
        {
            Contracts.AssertValue(values);
            if (!values.Any())
            {
                return this;
            }

            return new LazyList<T>(this._values.Concat(values));
        }

        /// <summary>
        /// Gives a new list with the given elements after the elements in this list.
        /// </summary>
        public LazyList<T> With(params T[] values)
        {
            Contracts.AssertValue(values);
            if (!values.Any())
            {
                return this;
            }

            return new LazyList<T>(this._values.Concat(values));
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        /// <summary>
        /// Create a new LazyList with the given starting set of values.
        /// </summary>
        public static LazyList<T> Of(params T[] values)
        {
            Contracts.AssertValue(values);
            return new LazyList<T>(values);
        }

        /// <summary>
        /// Create a new LazyList with the given starting set of values.
        /// </summary>
        public static LazyList<T> Of(IEnumerable<T> values)
        {
            Contracts.AssertValue(values);
            return new LazyList<T>(values);
        }
    }
}
