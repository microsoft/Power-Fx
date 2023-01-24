// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;
using System.Text;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// A value within an option set. 
    /// </summary>
    [DebuggerDisplay("{ToString()})")]
    public class OptionSetValue : ValidFormulaValue
    {
        /// <summary>
        /// Logical name for this option set value.
        /// </summary>
        public string Option { get; private set; }

        // Some option sets have specific non-logical-name backing values, used in Code Gen/Interpreter
        internal readonly object ExecutionValue;

        internal OptionSetValue(string option, OptionSetValueType type, object value = null)
            : base(IRContext.NotInSource(type))
        {
            Contracts.Assert(value == null || 
                (type._type.OptionSetInfo.BackingKind == DKind.String && value is string) ||
                (type._type.OptionSetInfo.BackingKind == DKind.Boolean && value is bool) ||
                (type._type.OptionSetInfo.BackingKind == DKind.Number && value is double));

            Option = option;
            ExecutionValue = value;
        }

        public new OptionSetValueType Type => (OptionSetValueType)base.Type;

        public override object ToObject()
        {
            return ExecutionValue ?? DisplayName;
        }

        public override string ToString()
        {
            return $"OptionSetValue ({Option}={DisplayName})";
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        /// <summary>
        /// Get the display name for this value. If no display name is available, 
        /// returns the logical name <see cref="Option"/>.
        /// </summary>
        public string DisplayName
        {
            get
            {
                var info = Type._type.OptionSetInfo;

                var displayNameProvider = info.DisplayNameProvider;

                var logicalName = Option;
                if (displayNameProvider.TryGetDisplayName(new DName(logicalName), out var displayName))
                {
                    return displayName.Value;
                }

                return logicalName;
            }
        }

        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings)
        {
            sb.Append($"{Type._type.OptionSetInfo.EntityName}.{Option}");
        }
    }
}
