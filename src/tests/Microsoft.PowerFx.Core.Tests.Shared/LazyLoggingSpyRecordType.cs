// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Tests
{
    // Test helper. Lets tests assert that the logging path does not enumerate
    // lazy record fields or force per-field type resolution.
    public sealed class LazyLoggingSpyRecordType : RecordType
    {
        public int TryGetFieldTypeCallCount;
        public int FieldNamesIterationCount;

        public override IEnumerable<string> FieldNames => EnumerateFieldNames();

        public override bool TryGetFieldType(string name, out FormulaType type)
        {
            TryGetFieldTypeCallCount++;
            switch (name)
            {
                case "A":
                    type = FormulaType.String;
                    return true;
                case "B":
                    type = FormulaType.Number;
                    return true;
                default:
                    type = FormulaType.Blank;
                    return false;
            }
        }

        public override bool Equals(object other)
        {
            return other is LazyLoggingSpyRecordType;
        }

        public override int GetHashCode()
        {
            return 0x511F;
        }

        private IEnumerable<string> EnumerateFieldNames()
        {
            FieldNamesIterationCount++;
            yield return "A";
            yield return "B";
        }
    }
}
