using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class DisplayNameTests
    {
        [Fact]
        public void IdentifierAndDisplayNameTest()
        {
            // >> With( { b : 15 }, AddColumns([1,2], e, Value * b))
            // Table({ Value: 1,e: 15},{ Value: 2,e: 30})

            var pfxConfig = new PowerFxConfig(Features.None);
            var engine = new RecalcEngine(pfxConfig);

            var fv = FormulaValue.New(15);
            var nv = new NamedValue("b", fv);
            var nvl = new List<NamedValue>() { nv };
            //var rv = new InMemoryRecordValue(
            //    IRContext.NotInSource(new KnownRecordType(DType.CreateRecord(new TypedName(DType.Number, new DName("b"))))),
            //    nvl);            

            var rt = RecordType.Empty()
                .Add(new NamedFormulaType("b", FormulaType.Number))
                .Add(new NamedFormulaType("e", FormulaType.Number, new DName("f")));

            var rv = new InMemoryRecordValue(
                IRContext.NotInSource(FormulaType.Build(rt._type)),
                nvl);

            //var rv = new InMemoryRecordValue(
            //    IRContext.NotInSource(new KnownRecordType(DType.CreateRecord(new TypedName(DType.Number, new DName("b"))))),
            //    nvl);

            //var rt = RecordType.Empty().Add(new NamedFormulaType("e", FormulaType.Number, new DName("f")));


            var result = engine.Eval("AddColumns([1, 2], \"e\", Value * b)", rv);
            var obj = result.ToObject();
        }
    }
}
