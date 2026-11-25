using Microsoft.PowerFx.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerFXBenchmark.TypedObjects
{
    public class DefaultBlankRecordType : RecordType
    {
        public RecordType RealRecordType { get; set; }
        public DefaultBlankRecordType(RecordType realRecordType)
        {
            RealRecordType = realRecordType;
        }
        public override IEnumerable<string> FieldNames => RealRecordType.FieldNames;

        public override RecordType Add(NamedFormulaType field)
        {
            var real = RealRecordType.Add(field);
            return new DefaultBlankRecordType(real);
        }

        public override bool Equals(object other)
        {
            if (other is not DefaultBlankRecordType otherRecordType)
            {
                return false;
            }

            return RealRecordType.Equals(otherRecordType);
        }

        public override int GetHashCode()
        {
            return RealRecordType.GetHashCode();
        }

        public override string? ToString()
        {
            return RealRecordType.ToString();
        }

        public override bool TryGetFieldType(string name, out FormulaType type)
        {
            if (RealRecordType.TryGetFieldType(name, out type))
            {
                return true;
            }

            type = FormulaType.UntypedObject;
            return true;
        }

        public override void Visit(ITypeVisitor vistor)
        {
            RealRecordType.Visit(vistor);
        }
    }
}
