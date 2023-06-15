// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// Helper class for representing fields or columns.
    /// </summary>
    [DebuggerDisplay("{Name}={Value}")]
    public class NamedValue
    {
        public string Name { get; }

        public FormulaValue Value => _value ?? _getFormulaValue().ConfigureAwait(false).GetAwaiter().GetResult();

        /// <summary>
        /// Useful for determining if the value is an entity or not.
        /// And based on that you can decide whether to get the <see cref="Value"/> at the point of use or not.
        /// NOTE: If this is true, then getting the <see cref="Value"/> will be an expansive operation.
        /// </summary>
        public bool IsExpandEntity => _backingDType.IsExpandEntity;

        /// <summary>
        /// DType that corresponds to the <see cref="Value"/>.
        /// NOTE: This could be a <see cref="DType"/> which still needs expansion for kind <see cref="DKind.DataEntity"/>.
        /// </summary>
        private readonly DType _backingDType;

        private readonly FormulaValue _value;

        private readonly Func<Task<FormulaValue>> _getFormulaValue;

        public NamedValue(KeyValuePair<string, FormulaValue> pair)
            : this(pair.Key, pair.Value)
        {
        }

        public NamedValue(string name, FormulaValue value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _value = value ?? throw new ArgumentNullException(nameof(value));
            _backingDType = value.Type._type;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedValue"/> class.
        /// Use this constructor to get <see cref="Value"/> lazily.
        /// </summary>
        internal NamedValue(string name, Func<Task<FormulaValue>> getFormulaValue, DType backingDType)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _backingDType = backingDType ?? throw new ArgumentNullException(nameof(backingDType));
            _getFormulaValue = getFormulaValue ?? throw new ArgumentNullException(nameof(getFormulaValue));
        }
    }
}
