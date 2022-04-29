// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// Helper class for representing fields or columns.
    /// </summary>
    public class NamedValue
    {
        public string Name { get; }

        public FormulaValue Value { get; }

        public NamedValue(KeyValuePair<string, FormulaValue> pair)
            : this(pair.Key, pair.Value)
        {
        }

        public NamedValue(string name, FormulaValue value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value;
        }
    }
}