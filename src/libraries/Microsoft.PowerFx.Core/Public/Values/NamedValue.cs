// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public FormulaValue Value => _value ?? _getFormulaValue();

        internal readonly DType BackingDType;

        private readonly FormulaValue _value;

        private readonly Func<FormulaValue> _getFormulaValue;

        public NamedValue(KeyValuePair<string, FormulaValue> pair)
            : this(pair.Key, pair.Value)
        {
        }

        public NamedValue(string name, FormulaValue value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            BackingDType = value.Type._type;
            _value = value;
        }

        internal NamedValue(string name, Func<FormulaValue> getFormulaValue, DType backingDType)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            BackingDType = backingDType ?? throw new ArgumentNullException(nameof(backingDType));
            _getFormulaValue = getFormulaValue ?? throw new ArgumentNullException(nameof(getFormulaValue));
        }
    }
}
