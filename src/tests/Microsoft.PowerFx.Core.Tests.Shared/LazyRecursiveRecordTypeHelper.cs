// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Tests
{
    public class LazyRecursiveRecordType : RecordType
    {
        public override IEnumerable<string> FieldNames => GetFieldNames();

        public bool EnumerableIterated = false;

        public LazyRecursiveRecordType()
            : base()
        {
        }

        public override bool TryGetFieldType(string name, out FormulaType type)
        {
            switch (name)
            {
                case "SomeString":
                    type = FormulaType.String;
                    return true;
                case "TableLoop":
                    type = ToTable();
                    return true;
                case "Loop":
                    type = this;
                    return true;
                case "Record":
                    type = RecordType.Empty().Add("Foo", FormulaType.Number);
                    return true;
                default:
                    type = FormulaType.Blank;
                    return false;
            }
        }

        private IEnumerable<string> GetFieldNames()
        {
            EnumerableIterated = true;

            yield return "SomeString";
            yield return "Loop";
            yield return "Record";
            yield return "TableLoop";
        }

        public override bool Equals(object other)
        {
            return other is LazyRecursiveRecordType; // All the same 
        }

        public override int GetHashCode()
        {
            return 1;
        }
    }
}
