// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// Helper for <see cref="RecordType.ResolveToLogicalNames(RecordValue)"/>.
    /// This creates a lazy record type that converts display names to logical names. 
    /// The values are based on the inner record, but are using logical names from the template. 
    /// </summary>
    internal class LogicalNameWrapperRecordValue : RecordValue
    {
        // Fields may have either the display or logical name. 
        private readonly RecordValue _inner;

        private readonly RecordType _template;

        public LogicalNameWrapperRecordValue(RecordType template, RecordValue inner)
            : base(new LogicalNameWrapperRecordType(template, inner))
        {
            _inner = inner;
            _template = template;
        }

        protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
        {
            // If the lookup is by logical name, succeed. 
            if (_inner.TryGetFieldDirect(fieldType, fieldName, out result))
            {
                return true;
            }

            if (DType.TryGetDisplayNameForColumn(_template._type, fieldName, out var displayName))
            {
                fieldName = displayName;
            }

            result = _inner.GetField(fieldName);
            return result != null;
        }

        // Wrap the template so that we can ensure that we only return fields 
        // that are used by _inner. 
        internal class LogicalNameWrapperRecordType : RecordType
        {
            private readonly RecordValue _inner;

            private readonly RecordType _template;

            public LogicalNameWrapperRecordType(RecordType template, RecordValue inner)
            {
                _template = template;
                _inner = inner;
            }

            public override IEnumerable<string> FieldNames
            {
                get
                {
                    // This will have display names
                    var names = _inner.Type.FieldNames;

                    var names2 = new List<string>();
                    foreach (var name in names)
                    {
                        if (DType.TryGetLogicalNameForColumn(_template._type, name, out var logicalName))
                        {
                            names2.Add(logicalName);
                        }
                        else
                        {
                            names2.Add(name);
                        }
                    }

                    return names2;
                }
            }

            public override bool TryGetFieldType(string name, out FormulaType type)
            {
                var found = _template.TryGetFieldType(name, out type);
                return found;
            }

            public override bool Equals(object other)
            {
                throw new NotImplementedException();
            }

            public override int GetHashCode()
            {
                throw new NotImplementedException();
            }
        }
    }
}
